using ApiVault.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace ApiVault.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<ApiCatalogService>();
        services.AddScoped<VendorService>();
        services.AddScoped<SecurityService>();
        services.AddScoped<ProjectService>();
        services.AddScoped<ReferenceDataService>();
        services.AddScoped<AuthService>();
        services.AddScoped<UserAdministrationService>();
        services.AddScoped<ApiTestService>();
        services.AddScoped<AuditLogService>();
        return services;
    }
}
