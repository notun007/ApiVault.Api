using ApiVault.Application.DTOs;
using ApiVault.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ApiVault.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/reference-data")]
public sealed class ReferenceDataController(ReferenceDataService service) : ControllerBase
{
    [HttpGet("business-areas")]
    public async Task<ActionResult<IReadOnlyList<LookupResponse>>> GetBusinessAreas(CancellationToken cancellationToken) =>
        Ok(await service.GetBusinessAreasAsync(cancellationToken));

    [HttpGet("development-teams")]
    public async Task<ActionResult<IReadOnlyList<LookupResponse>>> GetDevelopmentTeams(CancellationToken cancellationToken) =>
        Ok(await service.GetDevelopmentTeamsAsync(cancellationToken));

    [Authorize(Roles = "Admin,SuperAdmin")]
    [HttpPost("business-areas")]
    public async Task<ActionResult<LookupResponse>> CreateBusinessArea(
        CreateLookupRequest request,
        CancellationToken cancellationToken) =>
        Ok(await service.CreateBusinessAreaAsync(request, cancellationToken));

    [Authorize(Roles = "Admin,SuperAdmin")]
    [HttpPut("business-areas/{id:guid}")]
    public async Task<ActionResult<LookupResponse>> UpdateBusinessArea(
        Guid id,
        CreateLookupRequest request,
        CancellationToken cancellationToken) =>
        Ok(await service.UpdateBusinessAreaAsync(id, request, cancellationToken));

    [Authorize(Roles = "Admin,SuperAdmin")]
    [HttpDelete("business-areas/{id:guid}")]
    public async Task<IActionResult> DeleteBusinessArea(Guid id, CancellationToken cancellationToken)
    {
        await service.DeleteBusinessAreaAsync(id, cancellationToken);
        return NoContent();
    }

    [Authorize(Roles = "Admin,SuperAdmin")]
    [HttpPost("development-teams")]
    public async Task<ActionResult<LookupResponse>> CreateDevelopmentTeam(
        CreateLookupRequest request,
        CancellationToken cancellationToken) =>
        Ok(await service.CreateDevelopmentTeamAsync(request, cancellationToken));

    [Authorize(Roles = "Admin,SuperAdmin")]
    [HttpPut("development-teams/{id:guid}")]
    public async Task<ActionResult<LookupResponse>> UpdateDevelopmentTeam(
        Guid id,
        CreateLookupRequest request,
        CancellationToken cancellationToken) =>
        Ok(await service.UpdateDevelopmentTeamAsync(id, request, cancellationToken));

    [Authorize(Roles = "Admin,SuperAdmin")]
    [HttpDelete("development-teams/{id:guid}")]
    public async Task<IActionResult> DeleteDevelopmentTeam(Guid id, CancellationToken cancellationToken)
    {
        await service.DeleteDevelopmentTeamAsync(id, cancellationToken);
        return NoContent();
    }
}
