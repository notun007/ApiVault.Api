using ApiVault.Application.DTOs;
using ApiVault.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ApiVault.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/api-tests")]
public sealed class ApiTestsController(ApiTestService service) : ControllerBase
{
    [Authorize(Roles = "Admin,ApiOwner,Tester")]
    [HttpPost("execute")]
    public async Task<ActionResult<TestExecutionResponse>> Execute(
        ExecuteApiTestRequest request,
        CancellationToken cancellationToken) =>
        Ok(await service.ExecuteAsync(request, cancellationToken));

    [HttpGet("history")]
    public async Task<ActionResult<IReadOnlyList<TestExecutionResponse>>> History(
        [FromQuery] Guid? endpointId,
        [FromQuery] Guid? environmentId,
        [FromQuery] int take = 50,
        CancellationToken cancellationToken = default) =>
        Ok(await service.GetHistoryAsync(endpointId, environmentId, take, cancellationToken));
}
