using ApiVault.Application.Abstractions;
using ApiVault.Domain.Entities;
using ApiVault.Domain.Enums;
using ApiVault.Infrastructure.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ApiVault.Infrastructure.Persistence;

public sealed class DatabaseSeeder(
    ApiVaultDbContext dbContext,
    IPasswordHasher passwordHasher,
    IOptions<DatabaseInitializationOptions> options,
    ILogger<DatabaseSeeder> logger)
{
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        var settings = options.Value;
        if (!settings.Enabled) return;

        if (settings.ApplyMigrations)
            await dbContext.Database.MigrateAsync(cancellationToken);

        if (settings.SeedReferenceData)
            await SeedReferenceDataAsync(cancellationToken);

        if (settings.SeedAdmin)
            await SeedAdminAsync(settings, cancellationToken);
    }

    private async Task SeedReferenceDataAsync(CancellationToken cancellationToken)
    {
        if (!await dbContext.BusinessAreas.AnyAsync(cancellationToken))
        {
            dbContext.BusinessAreas.AddRange(
                new BusinessArea { Code = "CORE", Name = "Core Banking" },
                new BusinessArea { Code = "DIGITAL", Name = "Digital Banking" },
                new BusinessArea { Code = "HR", Name = "Human Resources" });
        }

        if (!await dbContext.DevelopmentTeams.AnyAsync(cancellationToken))
            dbContext.DevelopmentTeams.Add(new DevelopmentTeam { Code = "PLATFORM", Name = "Platform Engineering" });

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task SeedAdminAsync(DatabaseInitializationOptions settings, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(settings.AdminPassword))
        {
            logger.LogWarning("Admin seeding was requested but DatabaseInitialization:AdminPassword is empty.");
            return;
        }

        var username = settings.AdminUsername.Trim().ToLowerInvariant();
        if (await dbContext.AppUsers.AnyAsync(x => x.Username == username, cancellationToken)) return;

        dbContext.AppUsers.Add(new AppUser
        {
            Username = username,
            DisplayName = settings.AdminDisplayName,
            Email = settings.AdminEmail,
            PasswordHash = passwordHasher.Hash(settings.AdminPassword),
            Role = UserRole.Admin,
            IsActive = true
        });
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
