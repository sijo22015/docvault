using DocVault.Application.Services;
using DocVault.Domain.Interfaces;
using DocVault.Infrastructure.Data;
using DocVault.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DocVault.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        services.AddDbContext<AppDbContext>(opts =>
            opts.UseNpgsql(config.GetConnectionString("DefaultConnection")));

        services.AddScoped<IFileStorage, LocalDiskFileStorage>();
        services.AddScoped<IActivityLogger, ActivityLogger>();
        services.AddScoped<INotificationService, NotificationService>();
        services.AddMemoryCache();
        services.AddHttpClient<ResendEmailSender>();
        services.AddScoped<IEmailSender, ResendEmailSender>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IDocumentService, DocumentService>();
        services.AddScoped<IAdminService, AdminService>();

        return services;
    }
}
