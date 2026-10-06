using ApiVault.Application.Abstractions;
using ApiVault.Infrastructure.Http;
using ApiVault.Infrastructure.Options;
using ApiVault.Infrastructure.Persistence;
using ApiVault.Infrastructure.Security;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Oracle.EntityFrameworkCore;

namespace ApiVault.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.Configure<ExecutionSecurityOptions>(configuration.GetSection(ExecutionSecurityOptions.SectionName));
        services.Configure<DatabaseInitializationOptions>(configuration.GetSection(DatabaseInitializationOptions.SectionName));

        var connectionString = configuration.GetConnectionString("Oracle")
            ?? throw new InvalidOperationException("ConnectionStrings:Oracle is required.");

        services.AddDbContext<ApiVaultDbContext>(options =>
        {
            options.UseOracle(connectionString, oracleOptions =>
            {
                oracleOptions.UseOracleSQLCompatibility(
                    OracleSQLCompatibility.DatabaseVersion19);
                oracleOptions.MigrationsAssembly(typeof(ApiVaultDbContext).Assembly.FullName);
            });
        });

        services.AddScoped<IApplicationDbContext>(provider => provider.GetRequiredService<ApiVaultDbContext>());
        services.AddHttpContextAccessor();
        services.AddScoped<IUserContext, HttpUserContext>();
        services.AddSingleton<IPasswordHasher, Pbkdf2PasswordHasher>();
        services.AddScoped<ITokenService, JwtTokenService>();
        services.AddSingleton<ISecretProtector, DataProtectionSecretProtector>();

        services.AddDataProtection().SetApplicationName("ApiVault");

        services.AddHttpClient("ApiVaultExecutor")
            .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
            {
                AllowAutoRedirect = false,
                AutomaticDecompression = System.Net.DecompressionMethods.All,
                UseCookies = false,
                PooledConnectionLifetime = TimeSpan.FromMinutes(2),
                ConnectTimeout = TimeSpan.FromSeconds(10)
            });
        services.AddScoped<SsrfGuard>();
        services.AddScoped<SecretRedactor>();
        services.AddScoped<IApiTestExecutor, ApiTestExecutor>();
        services.AddScoped<DatabaseSeeder>();
        return services;
    }
}
