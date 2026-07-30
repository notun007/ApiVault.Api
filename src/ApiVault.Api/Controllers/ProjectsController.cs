using ApiVault.Application.DTOs;
using ApiVault.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ApiVault.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/projects")]
public sealed class ProjectsController(ProjectService service) : ControllerBase
{

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ProjectSummaryResponse>>> GetAll(CancellationToken cancellationToken)
    {
        var objProjects = await service.GetAllAsync(cancellationToken);
        return Ok(objProjects);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ProjectDetailResponse>> Get(Guid id, CancellationToken cancellationToken)
    {
        var objProject =  await service.GetAsync(id, cancellationToken);
        return Ok(objProject);
    }

    [Authorize(Roles = "Admin,ApiOwner")]
    [HttpPost]
    public async Task<ActionResult<ProjectDetailResponse>> Create(CreateProjectRequest request, CancellationToken cancellationToken)
    {
        var created = await service.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
    }

    [Authorize(Roles = "Admin,ApiOwner")]
    [HttpPost("{projectId:guid}/api-versions")]
    public async Task<ActionResult<ProjectDetailResponse>> LinkApiVersion(
        Guid projectId,
        LinkProjectApiVersionRequest request,
        CancellationToken cancellationToken) 
    {
        var objProjectDetail = await service.LinkApiVersionAsync(projectId, request, cancellationToken);
        return Ok(objProjectDetail);
    }


    //[HttpGet]
    //public async Task<ActionResult<IReadOnlyList<ProjectSummaryResponse>>> GetAll(CancellationToken cancellationToken) =>
    //    Ok(await service.GetAllAsync(cancellationToken));

    //[HttpGet("{id:guid}")]
    //public async Task<ActionResult<ProjectDetailResponse>> Get(Guid id, CancellationToken cancellationToken) =>
    //    Ok(await service.GetAsync(id, cancellationToken));

    //[Authorize(Roles = "Admin,ApiOwner")]
    //[HttpPost]
    //public async Task<ActionResult<ProjectDetailResponse>> Create(CreateProjectRequest request, CancellationToken cancellationToken)
    //{
    //    var created = await service.CreateAsync(request, cancellationToken);
    //    return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
    //}

    //[Authorize(Roles = "Admin,ApiOwner")]
    //[HttpPost("{projectId:guid}/api-versions")]
    //public async Task<ActionResult<ProjectDetailResponse>> LinkApiVersion(
    //    Guid projectId,
    //    LinkProjectApiVersionRequest request,
    //    CancellationToken cancellationToken) =>
    //    Ok(await service.LinkApiVersionAsync(projectId, request, cancellationToken));
}
