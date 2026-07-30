using ApiVault.Domain.Common;
using ApiVault.Domain.Enums;

namespace ApiVault.Domain.Entities;

public sealed class AppUser : AuditableEntity
{
    public string Username { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string PasswordHash { get; set; } = string.Empty;
    public UserRole Role { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime? LastLoginAtUtc { get; set; }
    public ICollection<AppUserRole> RoleAssignments { get; set; } = new List<AppUserRole>();
}

public sealed class SecurityRole : AuditableEntity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsSystemRole { get; set; }
    public bool IsActive { get; set; } = true;
    public ICollection<AppUserRole> UserAssignments { get; set; } = new List<AppUserRole>();
    public ICollection<RolePermission> Permissions { get; set; } = new List<RolePermission>();
}

public sealed class SecurityPermission : AuditableEntity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
}

public sealed class SecurityScreen : AuditableEntity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Route { get; set; } = string.Empty;
    public string? Icon { get; set; }
    public Guid? ParentId { get; set; }
    public SecurityScreen? Parent { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; } = true;
    public ICollection<SecurityScreen> Children { get; set; } = new List<SecurityScreen>();
    public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
}

public sealed class AppUserRole : AuditableEntity
{
    public Guid UserId { get; set; }
    public AppUser User { get; set; } = null!;
    public Guid RoleId { get; set; }
    public SecurityRole Role { get; set; } = null!;
}

public sealed class RolePermission : AuditableEntity
{
    public Guid RoleId { get; set; }
    public SecurityRole Role { get; set; } = null!;
    public Guid ScreenId { get; set; }
    public SecurityScreen Screen { get; set; } = null!;
    public Guid PermissionId { get; set; }
    public SecurityPermission Permission { get; set; } = null!;
}

public sealed class TestExecution : AuditableEntity
{
    public Guid ApiEndpointId { get; set; }
    public ApiEndpoint ApiEndpoint { get; set; } = null!;
    public Guid ApiEnvironmentId { get; set; }
    public ApiEnvironment ApiEnvironment { get; set; } = null!;
    public DateTime StartedAtUtc { get; set; }
    public long DurationMilliseconds { get; set; }
    public bool IsSuccess { get; set; }
    public int? ResponseStatusCode { get; set; }
    public string RequestUrl { get; set; } = string.Empty;
    public string? RequestHeadersJson { get; set; }
    public string? RequestBody { get; set; }
    public long RequestSizeBytes { get; set; }
    public string? ResponseHeadersJson { get; set; }
    public string? ResponseBody { get; set; }
    public long ResponseSizeBytes { get; set; }
    public string? ErrorMessage { get; set; }
}

public sealed class AuditLog
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime OccurredAtUtc { get; set; }
    public string UserName { get; set; } = "system";
    public string? IpAddress { get; set; }
    public string? CorrelationId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;
    public string? ChangesJson { get; set; }
}
