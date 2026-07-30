using ApiVault.Application.DTOs;
using ApiVault.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ApiVault.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/api-projects")]
public sealed class ApiProjectsController(ApiProjectService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ApiProjectResponse>>> GetAll([FromQuery] bool activeOnly = false, CancellationToken cancellationToken = default) =>
        Ok(await service.GetAllAsync(activeOnly, cancellationToken));

    [HttpGet("active")]
    public async Task<ActionResult<IReadOnlyList<ApiProjectResponse>>> GetActive(CancellationToken cancellationToken) =>
        Ok(await service.GetAllAsync(true, cancellationToken));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiProjectResponse>> Get(Guid id, CancellationToken cancellationToken) =>
        Ok(await service.GetAsync(id, cancellationToken));

    [Authorize(Roles = "Admin,ApiOwner")]
    [HttpPost]
    public async Task<ActionResult<ApiProjectResponse>> Create(CreateApiProjectRequest request, CancellationToken cancellationToken)
    {
        var created = await service.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
    }

    [Authorize(Roles = "Admin,ApiOwner")]
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ApiProjectResponse>> Update(Guid id, CreateApiProjectRequest request, CancellationToken cancellationToken) =>
        Ok(await service.UpdateAsync(id, request, cancellationToken));

    [Authorize(Roles = "Admin,ApiOwner")]
    [HttpPatch("{id:guid}/deactivate")]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken cancellationToken)
    {
        await service.DeactivateAsync(id, cancellationToken);
        return NoContent();
    }
}
