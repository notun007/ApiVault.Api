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
        if (options.Value.SeedBusinessReferenceData && !await dbContext.BusinessAreas.AnyAsync(cancellationToken))
        {
            dbContext.BusinessAreas.AddRange(
                new BusinessArea { Code = "CORE", Name = "Core Banking" },
                new BusinessArea { Code = "DIGITAL", Name = "Digital Banking" },
                new BusinessArea { Code = "HR", Name = "Human Resources" });
        }

        if (options.Value.SeedBusinessReferenceData && !await dbContext.DevelopmentTeams.AnyAsync(cancellationToken))
            dbContext.DevelopmentTeams.Add(new DevelopmentTeam { Code = "PLATFORM", Name = "Platform Engineering" });

        await dbContext.SaveChangesAsync(cancellationToken);
        await SeedSecurityReferenceDataAsync(cancellationToken);
    }

    private async Task SeedSecurityReferenceDataAsync(CancellationToken cancellationToken)
    {
        var permissions = new (string Code, string Name)[]
        {
            ("VIEW", "View"), ("CREATE", "Create"), ("UPDATE", "Update"),
            ("DELETE", "Delete"), ("EXECUTE", "Execute"), ("APPROVE", "Approve")
        };
        foreach (var (code, name) in permissions)
            if (!await dbContext.SecurityPermissions.AnyAsync(x => x.Code == code, cancellationToken))
                dbContext.SecurityPermissions.Add(new SecurityPermission { Code = code, Name = name });

        var roles = new (string Code, string Name, UserRole? LegacyRole)[]
        {
            ("SUPER_ADMIN", "Super Administrator", UserRole.SuperAdmin),
            ("ADMIN", "Administrator", UserRole.Admin), ("API_OWNER", "API Owner", UserRole.ApiOwner),
            ("TESTER", "Tester", UserRole.Tester), ("VIEWER", "Viewer", UserRole.Viewer),
            ("SECURITY_ADMIN", "Security Administrator", null)
        };
        foreach (var (code, name, _) in roles)
            if (!await dbContext.SecurityRoles.AnyAsync(x => x.Code == code, cancellationToken))
                dbContext.SecurityRoles.Add(new SecurityRole { Code = code, Name = name, IsSystemRole = true, IsActive = code != "SECURITY_ADMIN" });

        var screens = new (string Code, string Name, string Route, string Icon, int Order)[]
        {
            ("DASHBOARD", "Dashboard", "/dashboard", "DB", 10), ("API_CATALOG", "API Catalog", "/admin/apis", "AP", 20),
            ("PROJECTS", "Applications & Systems", "/projects", "AS", 30), ("TEST_CONSOLE", "Test Console", "/admin/test-console", "TX", 40),
            ("TEST_HISTORY", "Test History", "/admin/test-history", "HS", 50), ("REFERENCE_DATA", "Reference Data", "/admin/reference-data", "RF", 60),
            ("VENDORS", "Vendor Companies", "/admin/vendors", "VN", 75), ("USERS", "Users", "/admin/users", "US", 80),
            ("RESET_PASSWORD", "Reset Password", "/admin/reset-password", "PW", 85),
            ("AUDIT_LOGS", "Audit Logs", "/admin/audit-logs", "AU", 90), ("SECURITY_ROLES", "Roles", "/admin/security/roles", "RL", 100),
            ("SECURITY_PERMISSIONS", "Permissions", "/admin/security/permissions", "PM", 110)
        };
        foreach (var (code, name, route, icon, order) in screens)
            if (!await dbContext.SecurityScreens.AnyAsync(x => x.Code == code, cancellationToken))
                dbContext.SecurityScreens.Add(new SecurityScreen { Code = code, Name = name, Route = route, Icon = icon, DisplayOrder = order });

        await dbContext.SaveChangesAsync(cancellationToken);

        var seededRoles = await dbContext.SecurityRoles.Where(x => x.IsSystemRole).ToListAsync(cancellationToken);
        var allScreens = await dbContext.SecurityScreens.ToListAsync(cancellationToken);
        var allPermissions = await dbContext.SecurityPermissions.ToListAsync(cancellationToken);
        foreach (var role in seededRoles)
            foreach (var screen in allScreens)
                foreach (var permission in allPermissions)
                    if (ShouldGrant(role.Code, screen.Code, permission.Code) &&
                        !await dbContext.RolePermissions.AnyAsync(x => x.RoleId == role.Id && x.ScreenId == screen.Id && x.PermissionId == permission.Id, cancellationToken))
                        dbContext.RolePermissions.Add(new RolePermission { RoleId = role.Id, ScreenId = screen.Id, PermissionId = permission.Id });

        var users = await dbContext.AppUsers.ToListAsync(cancellationToken);
        foreach (var user in users)
        {
            var roleCode = user.Role switch { UserRole.SuperAdmin => "SUPER_ADMIN", UserRole.Admin => "ADMIN", UserRole.ApiOwner => "API_OWNER", UserRole.Tester => "TESTER", _ => "VIEWER" };
            var role = await dbContext.SecurityRoles.SingleAsync(x => x.Code == roleCode, cancellationToken);
            if (!await dbContext.AppUserRoles.AnyAsync(x => x.UserId == user.Id && x.RoleId == role.Id, cancellationToken))
                dbContext.AppUserRoles.Add(new AppUserRole { UserId = user.Id, RoleId = role.Id });
        }
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static bool ShouldGrant(string role, string screen, string permission) => role switch
    {
        "SUPER_ADMIN" or "ADMIN" => true,
        "API_OWNER" => screen is "DASHBOARD" or "API_CATALOG" or "PROJECTS" or "TEST_CONSOLE" or "TEST_HISTORY"
            && permission is "VIEW" or "CREATE" or "UPDATE" or "EXECUTE",
        "TESTER" => screen is "DASHBOARD" or "API_CATALOG" or "TEST_CONSOLE" or "TEST_HISTORY"
            && permission is "VIEW" or "EXECUTE",
        "VIEWER" => screen is "DASHBOARD" or "API_CATALOG" or "PROJECTS" && permission == "VIEW",
        _ => false
    };

    private async Task SeedAdminAsync(DatabaseInitializationOptions settings, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(settings.AdminPassword))
        {
            logger.LogWarning("Admin seeding was requested but DatabaseInitialization:AdminPassword is empty.");
            return;
        }

        var username = settings.AdminUsername.Trim().ToLowerInvariant();
        if (await dbContext.AppUsers.AnyAsync(x => x.Username == username, cancellationToken)) return;

        var admin = new AppUser
        {
            Username = username,
            DisplayName = settings.AdminDisplayName,
            Email = settings.AdminEmail,
            PasswordHash = passwordHasher.Hash(settings.AdminPassword),
            Role = UserRole.SuperAdmin,
            IsActive = true
        };
        var role = await dbContext.SecurityRoles.SingleAsync(x => x.Code == "SUPER_ADMIN", cancellationToken);
        admin.RoleAssignments.Add(new AppUserRole { RoleId = role.Id });
        dbContext.AppUsers.Add(admin);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
