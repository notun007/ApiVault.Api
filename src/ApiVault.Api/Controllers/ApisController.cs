using ApiVault.Application.Common;
using ApiVault.Application.DTOs;
using ApiVault.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ApiVault.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/apis")]
public sealed class ApisController(ApiCatalogService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResult<ApiSummaryResponse>>> Search(
        [FromQuery] ApiSearchQuery query,
        CancellationToken cancellationToken) =>
        Ok(await service.SearchAsync(query, cancellationToken));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiDetailResponse>> Get(Guid id, CancellationToken cancellationToken) =>
        Ok(await service.GetAsync(id, cancellationToken));

    [Authorize(Roles = "Admin,ApiOwner")]
    [HttpPost]
    public async Task<ActionResult<ApiDetailResponse>> Create(CreateApiRequest request, CancellationToken cancellationToken)
    {
        var created = await service.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
    }

    [Authorize(Roles = "Admin,ApiOwner")]
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ApiDetailResponse>> Update(
        Guid id,
        CreateApiRequest request,
        CancellationToken cancellationToken) =>
        Ok(await service.UpdateAsync(id, request, cancellationToken));

    [Authorize(Roles = "Admin,ApiOwner")]
    [HttpPost("{apiId:guid}/versions")]
    public async Task<ActionResult<ApiVersionResponse>> AddVersion(
        Guid apiId,
        CreateApiVersionRequest request,
        CancellationToken cancellationToken) =>
        Ok(await service.AddVersionAsync(apiId, request, cancellationToken));

    [HttpGet("versions/{versionId:guid}")]
    public async Task<ActionResult<ApiVersionResponse>> GetVersion(Guid versionId, CancellationToken cancellationToken) =>
        Ok(await service.GetVersionAsync(versionId, cancellationToken));

    [Authorize(Roles = "Admin,ApiOwner")]
    [HttpPut("{apiId:guid}/versions/{versionId:guid}")]
    public async Task<ActionResult<ApiVersionResponse>> UpdateVersion(
        Guid apiId,
        Guid versionId,
        UpdateApiVersionRequest request,
        CancellationToken cancellationToken) =>
        Ok(await service.UpdateVersionAsync(apiId, versionId, request, cancellationToken));

    [Authorize(Roles = "Admin,ApiOwner")]
    [HttpPatch("{apiId:guid}/versions/{versionId:guid}/lifecycle")]
    public async Task<ActionResult<ApiVersionResponse>> ChangeLifecycle(
        Guid apiId,
        Guid versionId,
        ChangeLifecycleRequest request,
        CancellationToken cancellationToken) =>
        Ok(await service.ChangeLifecycleAsync(apiId, versionId, request, cancellationToken));

    [Authorize(Roles = "Admin,ApiOwner")]
    [HttpPost("{apiId:guid}/versions/{versionId:guid}/endpoints")]
    public async Task<ActionResult<EndpointResponse>> AddEndpoint(
        Guid apiId,
        Guid versionId,
        CreateEndpointRequest request,
        CancellationToken cancellationToken) =>
        Ok(await service.AddEndpointAsync(apiId, versionId, request, cancellationToken));

    [Authorize(Roles = "Admin,ApiOwner")]
    [HttpPut("{apiId:guid}/versions/{versionId:guid}/endpoints/{endpointId:guid}")]
    public async Task<ActionResult<EndpointResponse>> UpdateEndpoint(
        Guid apiId,
        Guid versionId,
        Guid endpointId,
        CreateEndpointRequest request,
        CancellationToken cancellationToken) =>
        Ok(await service.UpdateEndpointAsync(apiId, versionId, endpointId, request, cancellationToken));

    [Authorize(Roles = "Admin,ApiOwner")]
    [HttpPost("{apiId:guid}/versions/{versionId:guid}/environments")]
    public async Task<ActionResult<EnvironmentResponse>> AddEnvironment(
        Guid apiId,
        Guid versionId,
        CreateEnvironmentRequest request,
        CancellationToken cancellationToken) =>
        Ok(await service.AddEnvironmentAsync(apiId, versionId, request, cancellationToken));

    [Authorize(Roles = "Admin,ApiOwner")]
    [HttpPut("{apiId:guid}/versions/{versionId:guid}/environments/{environmentId:guid}")]
    public async Task<ActionResult<EnvironmentResponse>> UpdateEnvironment(
        Guid apiId,
        Guid versionId,
        Guid environmentId,
        CreateEnvironmentRequest request,
        CancellationToken cancellationToken) =>
        Ok(await service.UpdateEnvironmentAsync(apiId, versionId, environmentId, request, cancellationToken));

    [Authorize(Roles = "Admin,ApiOwner")]
    [HttpPut("{apiId:guid}/versions/{versionId:guid}/environments/{environmentId:guid}/secret")]
    public async Task<ActionResult<EnvironmentResponse>> SetEnvironmentSecret(
        Guid apiId,
        Guid versionId,
        Guid environmentId,
        SetEnvironmentSecretRequest request,
        CancellationToken cancellationToken) =>
        Ok(await service.SetEnvironmentSecretAsync(apiId, versionId, environmentId, request, cancellationToken));
}
