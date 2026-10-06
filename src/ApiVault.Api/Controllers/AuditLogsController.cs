using ApiVault.Application.DTOs;
using ApiVault.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ApiVault.Api.Controllers;

[ApiController]
[Authorize(Roles = "Admin,SuperAdmin")]
[Route("api/audit-logs")]
public sealed class AuditLogsController(AuditLogService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<AuditLogResponse>>> Get(
        [FromQuery] string? entityType,
        [FromQuery] string? entityId,
        [FromQuery] int take = 100,
        CancellationToken cancellationToken = default) =>
        Ok(await service.GetAsync(entityType, entityId, take, cancellationToken));
}
