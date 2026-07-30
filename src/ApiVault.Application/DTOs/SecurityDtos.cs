using System.ComponentModel.DataAnnotations;

namespace ApiVault.Application.DTOs;

public sealed class CreateRoleRequest
{
    [Required, MaxLength(50)] public string Code { get; set; } = string.Empty;
    [Required, MaxLength(150)] public string Name { get; set; } = string.Empty;
    [MaxLength(500)] public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class RoleResponse
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsSystemRole { get; set; }
    public bool IsActive { get; set; }
    public int UserCount { get; set; }
}

public sealed class PermissionResponse
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}

public sealed class SecurityScreenResponse
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Route { get; set; } = string.Empty;
    public string? Icon { get; set; }
    public Guid? ParentId { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; }
}

public sealed class RolePermissionResponse
{
    public Guid ScreenId { get; set; }
    public List<Guid> PermissionIds { get; set; } = [];
}

public sealed class UpdateRolePermissionsRequest
{
    public List<RolePermissionResponse> Permissions { get; set; } = [];
}

public sealed class UserAccessResponse
{
    public Guid UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public List<UserRoleAssignmentResponse> Roles { get; set; } = [];
}

public sealed class UserRoleAssignmentResponse
{
    public Guid RoleId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

public sealed class UpdateUserRolesRequest
{
    public List<Guid> RoleIds { get; set; } = [];
}
