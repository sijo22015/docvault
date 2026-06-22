using DocVault.Infrastructure.Services;
using PdfSharpCore.Fonts;
using DocVault.Api.Extensions;
using DocVault.Api.Middleware;
using DocVault.Application.Validators;
using DocVault.Domain.Entities;
using DocVault.Domain.Enums;
using DocVault.Infrastructure;
using DocVault.Infrastructure.Data;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Serilog;
using System.Security.Claims;
using System.Text;
using System.Threading.RateLimiting;

AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
GlobalFontSettings.FontResolver = new LinuxFontResolver();

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.ConfigureKestrel(opts =>
{
    var mb = int.Parse(builder.Configuration["Storage:MaxFileSizeMB"] ?? "25");
    opts.Limits.MaxRequestBodySize = (mb + 5) * 1024L * 1024L; // headroom for form fields
});

// Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("logs/docvault-.log", rollingInterval: RollingInterval.Day)
    .CreateLogger();
builder.Host.UseSerilog();

// Infrastructure (DbContext + services)
builder.Services.AddInfrastructure(builder.Configuration);

// Identity
builder.Services.AddIdentity<AppUser, IdentityRole<Guid>>(opts =>
{
    opts.Password.RequiredLength = 10;
    opts.Password.RequireDigit = true;
    opts.Password.RequireUppercase = true;
    opts.Password.RequireLowercase = true;
    opts.Password.RequireNonAlphanumeric = true;
    opts.User.RequireUniqueEmail = true;
})
.AddEntityFrameworkStores<AppDbContext>()
.AddDefaultTokenProviders();

// JWT
var jwtKey = builder.Configuration["Jwt:SigningKey"]!;
builder.Services.AddAuthentication(opts =>
{
    opts.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    opts.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    opts.DefaultForbidScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(opts =>
{
    opts.MapInboundClaims = false;
    opts.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
        RoleClaimType = "role",
        NameClaimType = "sub"
    };
    opts.Events = new Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerEvents
    {
        OnMessageReceived = ctx =>
        {
            if (ctx.Request.Cookies.TryGetValue("access_token", out var token) && !string.IsNullOrEmpty(token))
                ctx.Token = token;
            return Task.CompletedTask;
        }
    };
});

// CORS
var corsOrigins = builder.Configuration.GetSection("Cors:Origins").Get<string[]>() ?? ["http://localhost:5173"];
builder.Services.AddCors(opts =>
    opts.AddDefaultPolicy(p => p.WithOrigins(corsOrigins).AllowAnyHeader().AllowAnyMethod().AllowCredentials()));

// Rate limiting
builder.Services.AddRateLimiter(opts =>
{
    opts.AddFixedWindowLimiter("login", p =>
    {
        p.PermitLimit = 5;
        p.Window = TimeSpan.FromMinutes(1);
        p.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        p.QueueLimit = 0;
    });
    opts.AddFixedWindowLimiter("api", p =>
    {
        p.PermitLimit = 60;
        p.Window = TimeSpan.FromMinutes(1);
        p.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        p.QueueLimit = 5;
    });
    opts.RejectionStatusCode = 429;
});

//Policy-based authorization 
//builder.Services.AddAuthorization(options =>
//{
//    options.AddPolicy("Admin", p => p.RequireRole(UserRole.Admin));
//    options.AddPolicy("User", p => p.RequireRole(UserRole.User));
//    options.AddPolicy("AdminOrUser", p => p.RequireRole(UserRole.Admin, UserRole.User));
//});

// Validation
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<RegisterRequestValidator>();

// HttpClient (used by ChatController to proxy Groq)
builder.Services.AddHttpClient();

// Controllers + OpenAPI
builder.Services.AddControllers()
    .AddJsonOptions(opts =>
        opts.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase);
builder.Services.AddSwaggerWithJwt();

// Health checks
builder.Services.AddHealthChecks();

var app = builder.Build();

app.UseMiddleware<ExceptionMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "DocVault API v1");
        c.RoutePrefix = "swagger";
    });
}

if (app.Environment.IsDevelopment()) app.UseHttpsRedirection();
app.UseSerilogRequestLogging();
app.UseCors();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health");

// Add new columns that don't exist yet (idempotent — safe on every startup)
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.ExecuteSqlRawAsync(
        "ALTER TABLE \"AspNetUsers\" ADD COLUMN IF NOT EXISTS \"CommunicationAddress\" character varying(500)");
}

// Seed admin user and reference data on first run
using (var scope = app.Services.CreateScope())
{
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();

    foreach (var role in new[] { "Admin", "User" })
        if (!await roleManager.RoleExistsAsync(role))
            await roleManager.CreateAsync(new IdentityRole<Guid>(role));

    var adminEmail = app.Configuration["Seed:AdminEmail"] ?? "admin@docvault.local";
    var adminPassword = app.Configuration["Seed:AdminPassword"] ?? "Admin@12345";
    var existingAdmin = await userManager.FindByEmailAsync(adminEmail);
    if (existingAdmin == null)
    {
        var admin = new AppUser
        {
            UserName = adminEmail,
            Email = adminEmail,
            FullName = "System Admin",
            Department = "Administration",
            UserStatus = "APPROVED",
            EmailConfirmed = true
        };
        var result = await userManager.CreateAsync(admin, adminPassword);
        if (result.Succeeded)
            await userManager.AddToRoleAsync(admin, "Admin");
    }
    else
    {
        existingAdmin.UserStatus = "APPROVED";
        await userManager.UpdateAsync(existingAdmin);
        if (!await userManager.IsInRoleAsync(existingAdmin, "Admin"))
            await userManager.AddToRoleAsync(existingAdmin, "Admin");
    }

    // Seed reference data (runs only when tables are empty)
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    if (!await db.Departments.AnyAsync())
    {
        db.Departments.AddRange(
            new DocVault.Domain.Entities.Department { Name = "Physics",          Code = "PHY" },
            new DocVault.Domain.Entities.Department { Name = "Chemistry",        Code = "CHE" },
            new DocVault.Domain.Entities.Department { Name = "Mathematics",      Code = "MAT" },
            new DocVault.Domain.Entities.Department { Name = "English",          Code = "ENG" },
            new DocVault.Domain.Entities.Department { Name = "Malayalam",        Code = "MAL" },
            new DocVault.Domain.Entities.Department { Name = "Computer Science", Code = "CS"  },
            new DocVault.Domain.Entities.Department { Name = "Biology",          Code = "BIO" },
            new DocVault.Domain.Entities.Department { Name = "History",          Code = "HIS" },
            new DocVault.Domain.Entities.Department { Name = "Economics",        Code = "ECO" },
            new DocVault.Domain.Entities.Department { Name = "Commerce",         Code = "COM" }
        );
        await db.SaveChangesAsync();
    }

    {
        var existingTypes = await db.DocumentTypes.Select(t => t.Name).ToHashSetAsync();
        var allTypes = new[]
        {
            "Syllabus", "LessonPlan", "AttendanceRegister", "MarksList",
            "MinutesOfMeeting", "EventReport", "ResearchPaper", "AuditReport",
            "CircularReceived", "GeotaggedPhoto", "Other"
        };
        foreach (var name in allTypes.Where(n => !existingTypes.Contains(n)))
            db.DocumentTypes.Add(new DocVault.Domain.Entities.DocumentType { Name = name });
        if (db.ChangeTracker.HasChanges()) await db.SaveChangesAsync();
    }

    if (!await db.FinancialYears.AnyAsync())
    {
        db.FinancialYears.AddRange(
            new DocVault.Domain.Entities.FinancialYear { Label = "2023-2024", FyStart = new DateOnly(2023, 4, 1), FyEnd = new DateOnly(2024, 3, 31) },
            new DocVault.Domain.Entities.FinancialYear { Label = "2024-2025", FyStart = new DateOnly(2024, 4, 1), FyEnd = new DateOnly(2025, 3, 31) },
            new DocVault.Domain.Entities.FinancialYear { Label = "2025-2026", FyStart = new DateOnly(2025, 4, 1), FyEnd = new DateOnly(2026, 3, 31) },
            new DocVault.Domain.Entities.FinancialYear { Label = "2026-2027", FyStart = new DateOnly(2026, 4, 1), FyEnd = new DateOnly(2027, 3, 31) },
            new DocVault.Domain.Entities.FinancialYear { Label = "2027-2028", FyStart = new DateOnly(2027, 4, 1), FyEnd = new DateOnly(2028, 3, 31) }
        );
        await db.SaveChangesAsync();
    }
}

    app.Run();
