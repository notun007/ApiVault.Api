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

    [Authorize(Roles = "Admin")]
    [HttpPost("business-areas")]
    public async Task<ActionResult<LookupResponse>> CreateBusinessArea(
        CreateLookupRequest request,
        CancellationToken cancellationToken) =>
        Ok(await service.CreateBusinessAreaAsync(request, cancellationToken));

    [Authorize(Roles = "Admin")]
    [HttpPost("development-teams")]
    public async Task<ActionResult<LookupResponse>> CreateDevelopmentTeam(
        CreateLookupRequest request,
        CancellationToken cancellationToken) =>
        Ok(await service.CreateDevelopmentTeamAsync(request, cancellationToken));
}
