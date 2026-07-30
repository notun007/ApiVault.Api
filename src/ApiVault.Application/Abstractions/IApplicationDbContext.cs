using ApiVault.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ApiVault.Application.Abstractions;

public interface IApplicationDbContext
{
    DbSet<BusinessArea> BusinessAreas { get; }
    DbSet<DevelopmentTeam> DevelopmentTeams { get; }
    DbSet<ApiProject> ApiProjects { get; }
    DbSet<ApiAsset> ApiAssets { get; }
    DbSet<ApiVersion> ApiVersions { get; }
    DbSet<ApiEndpoint> ApiEndpoints { get; }
    DbSet<ApiEnvironment> ApiEnvironments { get; }
    DbSet<EnvironmentSecret> EnvironmentSecrets { get; }
    DbSet<Project> Projects { get; }
    DbSet<ProjectApiVersion> ProjectApiVersions { get; }
    DbSet<AppUser> AppUsers { get; }
    DbSet<SecurityRole> SecurityRoles { get; }
    DbSet<SecurityPermission> SecurityPermissions { get; }
    DbSet<SecurityScreen> SecurityScreens { get; }
    DbSet<AppUserRole> AppUserRoles { get; }
    DbSet<RolePermission> RolePermissions { get; }
    DbSet<TestExecution> TestExecutions { get; }
    DbSet<AuditLog> AuditLogs { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
