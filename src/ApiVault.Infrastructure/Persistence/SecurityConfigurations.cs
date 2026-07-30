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
