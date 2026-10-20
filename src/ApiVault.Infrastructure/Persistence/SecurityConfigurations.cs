using ApiVault.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ApiVault.Infrastructure.Persistence;

public sealed class SecurityRoleConfiguration : IEntityTypeConfiguration<SecurityRole>
{
    public void Configure(EntityTypeBuilder<SecurityRole> b)
    {
        b.ToTable("SEC_ROLE");
        ConfigurationHelpers.ConfigureAudit(b);
        b.Property(x => x.Code).HasMaxLength(50).IsRequired();
        b.Property(x => x.Name).HasMaxLength(150).IsRequired();
        b.Property(x => x.Description).HasMaxLength(500);
        b.HasIndex(x => x.Code).IsUnique();
    }
}

public sealed class SecurityPermissionConfiguration : IEntityTypeConfiguration<SecurityPermission>
{
    public void Configure(EntityTypeBuilder<SecurityPermission> b)
    {
        b.ToTable("SEC_PERMISSION");
        ConfigurationHelpers.ConfigureAudit(b);
        b.Property(x => x.Code).HasMaxLength(50).IsRequired();
        b.Property(x => x.Name).HasMaxLength(150).IsRequired();
        b.Property(x => x.Description).HasMaxLength(500);
        b.HasIndex(x => x.Code).IsUnique();
    }
}

public sealed class SecurityScreenConfiguration : IEntityTypeConfiguration<SecurityScreen>
{
    public void Configure(EntityTypeBuilder<SecurityScreen> b)
    {
        b.ToTable("SEC_SCREEN");
        ConfigurationHelpers.ConfigureAudit(b);
        b.Property(x => x.UpdatedAtUtc).IsConcurrencyToken();
        b.Property(x => x.Code).HasMaxLength(80).IsRequired();
        b.Property(x => x.Name).HasMaxLength(150).IsRequired();
        b.Property(x => x.Route).HasMaxLength(300).IsRequired();
        b.Property(x => x.Icon).HasMaxLength(50);
        b.HasIndex(x => x.Code).IsUnique();
        b.HasIndex(x => x.Route).IsUnique();
        b.HasOne(x => x.Parent).WithMany(x => x.Children)
            .HasForeignKey(x => x.ParentId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class AppUserRoleConfiguration : IEntityTypeConfiguration<AppUserRole>
{
    public void Configure(EntityTypeBuilder<AppUserRole> b)
    {
        b.ToTable("APP_USER_ROLE");
        ConfigurationHelpers.ConfigureAudit(b);
        b.HasIndex(x => new { x.UserId, x.RoleId }).IsUnique();
        b.HasOne(x => x.User).WithMany(x => x.RoleAssignments)
            .HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Role).WithMany(x => x.UserAssignments)
            .HasForeignKey(x => x.RoleId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class RolePermissionConfiguration : IEntityTypeConfiguration<RolePermission>
{
    public void Configure(EntityTypeBuilder<RolePermission> b)
    {
        b.ToTable("ROLE_PERMISSION");
        ConfigurationHelpers.ConfigureAudit(b);
        b.HasIndex(x => new { x.RoleId, x.ScreenId, x.PermissionId }).IsUnique();
        b.HasOne(x => x.Role).WithMany(x => x.Permissions)
            .HasForeignKey(x => x.RoleId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Screen).WithMany(x => x.RolePermissions)
            .HasForeignKey(x => x.ScreenId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Permission).WithMany(x => x.RolePermissions)
            .HasForeignKey(x => x.PermissionId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class UserApiAccessGrantConfiguration : IEntityTypeConfiguration<UserApiAccessGrant>
{
    public void Configure(EntityTypeBuilder<UserApiAccessGrant> b)
    {
        b.ToTable("USER_API_ACCESS");
        ConfigurationHelpers.ConfigureAudit(b);
        b.Property(x => x.UserId).HasColumnType("RAW(16)");
        b.Property(x => x.ApplicationId).HasColumnType("RAW(16)");
        b.Property(x => x.ApiEndpointId).HasColumnType("RAW(16)");
        b.Property(x => x.ApiEnvironmentId).HasColumnType("RAW(16)");
        b.Property(x => x.IsActive).HasColumnType("NUMBER(1)");

        b.HasIndex(x => new { x.UserId, x.ApplicationId, x.ApiEndpointId, x.ApiEnvironmentId }).IsUnique();
        b.HasIndex(x => new { x.UserId, x.ApplicationId });

        b.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Application).WithMany().HasForeignKey(x => x.ApplicationId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.ApiEndpoint).WithMany().HasForeignKey(x => x.ApiEndpointId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.ApiEnvironment).WithMany().HasForeignKey(x => x.ApiEnvironmentId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class UserApiAccessActionConfiguration : IEntityTypeConfiguration<UserApiAccessAction>
{
    public void Configure(EntityTypeBuilder<UserApiAccessAction> b)
    {
        b.ToTable("USER_API_ACCESS_ACTION");
        b.HasKey(x => new { x.GrantId, x.ActionCode });
        b.Property(x => x.GrantId).HasColumnType("RAW(16)");
        b.Property(x => x.ActionCode).HasMaxLength(30).IsRequired();
        b.HasOne(x => x.Grant).WithMany(x => x.Actions)
            .HasForeignKey(x => x.GrantId).OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class UserApiAccessAuditConfiguration : IEntityTypeConfiguration<UserApiAccessAudit>
{
    public void Configure(EntityTypeBuilder<UserApiAccessAudit> b)
    {
        b.ToTable("USER_API_ACCESS_AUDIT");
        ConfigurationHelpers.ConfigureAudit(b);
        b.Property(x => x.GrantId).HasColumnType("RAW(16)");
        b.Property(x => x.UserId).HasColumnType("RAW(16)");
        b.Property(x => x.ActionType).HasMaxLength(30).IsRequired();
        b.Property(x => x.OldValueJson).HasColumnType("NCLOB");
        b.Property(x => x.NewValueJson).HasColumnType("NCLOB");
        b.HasIndex(x => new { x.UserId, x.CreatedAtUtc });
        b.HasIndex(x => x.GrantId);
    }
}
