using ApiVault.Application.DTOs;
using ApiVault.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ApiVault.Api.Controllers;

[ApiController]
[Authorize(Roles = "Admin,SuperAdmin")]
[Route("api/users")]
public sealed class UsersController(UserAdministrationService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<UserResponse>>> GetAll(CancellationToken cancellationToken) =>
        Ok(await service.GetAllAsync(cancellationToken));

    [HttpGet("access")]
    public async Task<ActionResult<IReadOnlyList<UserAccessResponse>>> GetAccess(CancellationToken cancellationToken) =>
        Ok(await service.GetAccessAsync(cancellationToken));

    [HttpPut("{userId:guid}/roles")]
    public async Task<IActionResult> UpdateRoles(Guid userId, UpdateUserRolesRequest request, CancellationToken cancellationToken)
    {
        await service.UpdateRolesAsync(userId, request, cancellationToken);
        return NoContent();
    }

    [HttpPost]
    public async Task<ActionResult<UserResponse>> Create(CreateUserRequest request, CancellationToken cancellationToken) =>
        Ok(await service.CreateAsync(request, cancellationToken));

    [HttpPut("{userId:guid}/password")]
    public async Task<IActionResult> ResetPassword(Guid userId, ResetPasswordRequest request, CancellationToken cancellationToken)
    {
        await service.ResetPasswordAsync(userId, request, cancellationToken);
        return NoContent();
    }
}
