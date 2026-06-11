using Microsoft.OpenApi;

namespace DocVault.Api.Extensions
{
    public static class SwaggerExtensions
    {
        public static IServiceCollection AddSwaggerWithJwt(this IServiceCollection services)
        {
            services.AddEndpointsApiExplorer();

            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = "DocVault Management API",
                    Version = "v1",
                    Description = "REST API for managing documents and users with role-based access control.",
                    Contact = new OpenApiContact { Name = "DocVault Team", Email = "support@docvault.com" }
                });

                c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Name = "Authorization",
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer",
                    BearerFormat = "JWT",
                    In = ParameterLocation.Header,
                    Description = "Enter: Bearer {your JWT token}"
                });

                // v2.x API: factory receives the document so the reference can be resolved
                // JWT Bearer has no OAuth2 scopes — value must be an empty array
                c.AddSecurityRequirement(doc => new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecuritySchemeReference("Bearer", doc),
                        new List<string>()
                    }
                });
            });

            return services;
        }
    }
}
