using ApiVault.Application.DTOs;
using ApiVault.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ApiVault.Api.Controllers;

[ApiController, Authorize, Route("api/navigation")]
public sealed class NavigationController(NavigationService service) : ControllerBase
{
    [HttpGet] public async Task<ActionResult<NavigationLayout>> Get(CancellationToken ct) => Ok(await service.GetLayoutAsync(false, ct));
    [HttpGet("management"), Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<ActionResult<NavigationLayout>> Management(CancellationToken ct) => Ok(await service.GetLayoutAsync(true, ct));
    [HttpPost("initialize"), Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<ActionResult<NavigationLayout>> Initialize(CancellationToken ct)
    { await service.InitializeAsync(ct); return Ok(await service.GetLayoutAsync(true, ct)); }
    [HttpPut, Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<ActionResult<NavigationLayout>> Save(SaveNavigationRequest request, CancellationToken ct) => Ok(await service.SaveAsync(request, ct));
}
