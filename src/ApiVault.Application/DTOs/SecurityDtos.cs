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

public sealed class ApiAccessCatalogApplicationResponse
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public List<ApiAccessCatalogApiResponse> Apis { get; set; } = [];
}

public sealed class ApiAccessCatalogApiResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public List<ApiAccessCatalogReleaseResponse> Releases { get; set; } = [];
}

public sealed class ApiAccessCatalogReleaseResponse
{
    public Guid Id { get; set; }
    public string Version { get; set; } = string.Empty;
    public string? ReleaseName { get; set; }
    public string LifecycleStatus { get; set; } = string.Empty;
    public List<ApiAccessCatalogEndpointResponse> Endpoints { get; set; } = [];
    public List<ApiAccessCatalogEnvironmentResponse> Environments { get; set; } = [];
}

public sealed class ApiAccessCatalogEndpointResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string HttpMethod { get; set; } = string.Empty;
    public string RelativePath { get; set; } = string.Empty;
}

public sealed class ApiAccessCatalogEnvironmentResponse
{
    public Guid Id { get; set; }
    public string EnvironmentType { get; set; } = string.Empty;
    public bool IsEnabled { get; set; }
}

public sealed class UserApiAccessGrantResponse
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid ApplicationId { get; set; }
    public string ApplicationCode { get; set; } = string.Empty;
    public string ApplicationName { get; set; } = string.Empty;
    public Guid ApiAssetId { get; set; }
    public string ApiName { get; set; } = string.Empty;
    public Guid ApiVersionId { get; set; }
    public string ApiVersion { get; set; } = string.Empty;
    public Guid ApiEndpointId { get; set; }
    public string EndpointName { get; set; } = string.Empty;
    public string HttpMethod { get; set; } = string.Empty;
    public string RelativePath { get; set; } = string.Empty;
    public Guid ApiEnvironmentId { get; set; }
    public string EnvironmentType { get; set; } = string.Empty;
    public bool CanRead { get; set; }
    public bool CanCreate { get; set; }
    public bool CanEdit { get; set; }
}

public sealed class SaveUserApiAccessRequest
{
    public List<SaveUserApiAccessGrantRequest> Grants { get; set; } = [];
}

public sealed class SaveUserApiAccessGrantRequest
{
    public Guid ApplicationId { get; set; }
    public Guid ApiAssetId { get; set; }
    public Guid ApiVersionId { get; set; }
    public Guid ApiEndpointId { get; set; }
    public Guid ApiEnvironmentId { get; set; }
    public bool CanRead { get; set; } = true;
    public bool CanCreate { get; set; }
    public bool CanEdit { get; set; }
}
