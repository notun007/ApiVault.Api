using System.Text.Json;
using ApiVault.Application.Abstractions;
using ApiVault.Domain.Common;
using ApiVault.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ApiVault.Infrastructure.Persistence;

public sealed class ApiVaultDbContext(
    DbContextOptions<ApiVaultDbContext> options,
    IUserContext userContext) : DbContext(options), IApplicationDbContext
{
    public DbSet<BusinessArea> BusinessAreas => Set<BusinessArea>();
    public DbSet<DevelopmentTeam> DevelopmentTeams => Set<DevelopmentTeam>();

    public DbSet<ApiAsset> ApiAssets => Set<ApiAsset>();
    public DbSet<ApiVersion> ApiVersions => Set<ApiVersion>();
    public DbSet<ApiEndpoint> ApiEndpoints => Set<ApiEndpoint>();
    public DbSet<ApiEnvironment> ApiEnvironments => Set<ApiEnvironment>();
    public DbSet<EnvironmentSecret> EnvironmentSecrets => Set<EnvironmentSecret>();
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<ProjectApiVersion> ProjectApiVersions => Set<ProjectApiVersion>();
    public DbSet<Vendor> Vendors => Set<Vendor>();
    public DbSet<AppUser> AppUsers => Set<AppUser>();
    public DbSet<SecurityRole> SecurityRoles => Set<SecurityRole>();
    public DbSet<SecurityPermission> SecurityPermissions => Set<SecurityPermission>();
    public DbSet<SecurityScreen> SecurityScreens => Set<SecurityScreen>();
    public DbSet<AppUserRole> AppUserRoles => Set<AppUserRole>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<TestExecution> TestExecutions => Set<TestExecution>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApiVaultDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        ApplyAuditFields();
        var auditLogs = BuildAuditLogs();
        if (auditLogs.Count > 0)
            AuditLogs.AddRange(auditLogs);
        return await base.SaveChangesAsync(cancellationToken);
    }

    private void ApplyAuditFields()
    {
        var now = DateTime.UtcNow;
        foreach (var entry in ChangeTracker.Entries<AuditableEntity>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAtUtc = now;
                entry.Entity.CreatedBy = userContext.UserName;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAtUtc = now;
                entry.Entity.UpdatedBy = userContext.UserName;
            }
        }
    }

    private List<AuditLog> BuildAuditLogs()
    {
        var result = new List<AuditLog>();
        foreach (var entry in ChangeTracker.Entries<AuditableEntity>()
                     .Where(x => x.State is EntityState.Added or EntityState.Modified or EntityState.Deleted))
        {
            var action = entry.State.ToString();
            var changes = new Dictionary<string, object?>();

            foreach (var property in entry.Properties)
            {
                if (entry.State == EntityState.Modified && !property.IsModified)
                    continue;

                var propertyName = property.Metadata.Name;
                if (IsSensitiveProperty(propertyName))
                {
                    changes[propertyName] = "***REDACTED***";
                    continue;
                }

                changes[propertyName] = entry.State switch
                {
                    EntityState.Modified => new
                    {
                        Old = property.OriginalValue,
                        New = property.CurrentValue
                    },
                    EntityState.Deleted => property.OriginalValue,
                    _ => property.CurrentValue
                };
            }

            result.Add(new AuditLog
            {
                OccurredAtUtc = DateTime.UtcNow,
                UserName = userContext.UserName,
                IpAddress = userContext.IpAddress,
                CorrelationId = userContext.CorrelationId,
                Action = action,
                EntityType = entry.Metadata.ClrType.Name,
                EntityId = entry.Entity.Id.ToString(),
                ChangesJson = JsonSerializer.Serialize(changes)
            });
        }
        return result;
    }

    private static bool IsSensitiveProperty(string propertyName) =>
        propertyName.Contains("Password", StringComparison.OrdinalIgnoreCase) ||
        propertyName.Contains("EncryptedValue", StringComparison.OrdinalIgnoreCase) ||
        propertyName.Contains("Secret", StringComparison.OrdinalIgnoreCase) ||
        propertyName.Contains("Token", StringComparison.OrdinalIgnoreCase);
}
