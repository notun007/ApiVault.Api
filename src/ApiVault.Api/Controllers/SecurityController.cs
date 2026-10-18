using ApiVault.Application.DTOs;
using ApiVault.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ApiVault.Api.Controllers;

[ApiController, Authorize(Roles = "Admin,SuperAdmin")]
[Route("api/security")]
public sealed class SecurityController(SecurityService service) : ControllerBase
{
    [HttpGet("roles")] public async Task<ActionResult<IReadOnlyList<RoleResponse>>> Roles(CancellationToken ct) => Ok(await service.GetRolesAsync(ct));
    [HttpPost("roles")] public async Task<ActionResult<RoleResponse>> CreateRole(CreateRoleRequest request, CancellationToken ct)
    { var result = await service.CreateRoleAsync(request, ct); return Created($"api/security/roles/{result.Id}", result); }
    [HttpPut("roles/{id:guid}")] public async Task<ActionResult<RoleResponse>> UpdateRole(Guid id, CreateRoleRequest request, CancellationToken ct) => Ok(await service.UpdateRoleAsync(id, request, ct));
    [HttpGet("permissions")] public async Task<ActionResult<IReadOnlyList<PermissionResponse>>> Permissions(CancellationToken ct) => Ok(await service.GetPermissionsAsync(ct));
    [HttpGet("screens")] public async Task<ActionResult<IReadOnlyList<SecurityScreenResponse>>> Screens(CancellationToken ct) => Ok(await service.GetScreensAsync(ct));
    [HttpGet("roles/{roleId:guid}/permissions")] public async Task<ActionResult<IReadOnlyList<RolePermissionResponse>>> RolePermissions(Guid roleId, CancellationToken ct) => Ok(await service.GetRolePermissionsAsync(roleId, ct));
    [HttpPut("roles/{roleId:guid}/permissions")] public async Task<IActionResult> UpdateRolePermissions(Guid roleId, UpdateRolePermissionsRequest request, CancellationToken ct)
    { await service.UpdateRolePermissionsAsync(roleId, request, ct); return NoContent(); }
    [HttpGet("api-access/catalog")] public async Task<ActionResult<IReadOnlyList<ApiAccessCatalogApplicationResponse>>> ApiAccessCatalog(CancellationToken ct) => Ok(await service.GetApiAccessCatalogAsync(ct));
    [HttpGet("users/{userId:guid}/api-access")] public async Task<ActionResult<IReadOnlyList<UserApiAccessGrantResponse>>> UserApiAccess(Guid userId, CancellationToken ct) => Ok(await service.GetUserApiAccessAsync(userId, ct));
    [HttpPut("users/{userId:guid}/api-access")] public async Task<IActionResult> UpdateUserApiAccess(Guid userId, SaveUserApiAccessRequest request, CancellationToken ct)
    { await service.UpdateUserApiAccessAsync(userId, request, ct); return NoContent(); }
}
