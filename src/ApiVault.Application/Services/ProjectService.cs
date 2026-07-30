using ApiVault.Application.Abstractions;
using ApiVault.Application.Common;
using ApiVault.Application.DTOs;
using ApiVault.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ApiVault.Application.Services;

public sealed class ProjectService(IApplicationDbContext dbContext)
{

    public async Task<IReadOnlyList<ProjectSummaryResponse>> GetAllAsync(CancellationToken cancellationToken) =>
        await dbContext.Projects.AsNoTracking().OrderBy(x => x.Name)
            .Select(x => new ProjectSummaryResponse
            {
                Id = x.Id,
                Code = x.Code,
                Name = x.Name,
                Criticality = x.Criticality,
                Status = x.Status,
                BusinessArea = x.BusinessArea.Name,
                OwnerTeam = x.OwnerTeam.Name,
                LinkedApiVersionCount = x.ApiLinks.Count
            }).ToListAsync(cancellationToken);

    public async Task<ProjectDetailResponse> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        var entity = await dbContext.Projects.AsNoTracking()
            .Include(x => x.BusinessArea)
            .Include(x => x.OwnerTeam)
            .Include(x => x.ApiLinks)
                .ThenInclude(x => x.ApiVersion)
                    .ThenInclude(x => x.ApiAsset)
            .SingleOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new NotFoundException("Project was not found.");
        return Map(entity);
    }

    public async Task<ProjectDetailResponse> CreateAsync(CreateProjectRequest request, CancellationToken cancellationToken)
    {
        if (!await dbContext.BusinessAreas.AnyAsync(x => x.Id == request.BusinessAreaId, cancellationToken))
            throw new RequestValidationException("Business area does not exist.");
        if (!await dbContext.DevelopmentTeams.AnyAsync(x => x.Id == request.OwnerTeamId, cancellationToken))
            throw new RequestValidationException("Owner team does not exist.");

        var code = request.Code.Trim().ToUpperInvariant();
        if (await dbContext.Projects.AnyAsync(x => x.Code.ToUpper() == code, cancellationToken))
            throw new ConflictException("A project with the same code already exists.");

        var entity = new Project
        {
            Code = code,
            Name = request.Name.Trim(),
            Description = request.Description?.Trim(),
            Criticality = request.Criticality,
            Status = request.Status,
            BusinessAreaId = request.BusinessAreaId,
            OwnerTeamId = request.OwnerTeamId
        };
        dbContext.Projects.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
        return await GetAsync(entity.Id, cancellationToken);
    }

    public async Task<ProjectDetailResponse> LinkApiVersionAsync(
        Guid projectId,
        LinkProjectApiVersionRequest request,
        CancellationToken cancellationToken)
    {
        if (!await dbContext.Projects.AnyAsync(x => x.Id == projectId, cancellationToken))
            throw new NotFoundException("Project was not found.");
        if (!await dbContext.ApiVersions.AnyAsync(x => x.Id == request.ApiVersionId, cancellationToken))
            throw new RequestValidationException("API version does not exist.");
        if (await dbContext.ProjectApiVersions.AnyAsync(
                x => x.ProjectId == projectId && x.ApiVersionId == request.ApiVersionId, cancellationToken))
            throw new ConflictException("The project is already linked to this exact API version.");

        dbContext.ProjectApiVersions.Add(new ProjectApiVersion
        {
            ProjectId = projectId,
            ApiVersionId = request.ApiVersionId,
            Purpose = request.Purpose?.Trim(),
            IsRequired = request.IsRequired
        });
        await dbContext.SaveChangesAsync(cancellationToken);
        return await GetAsync(projectId, cancellationToken);
    }

    private static ProjectDetailResponse Map(Project entity) => new()
    {
        Id = entity.Id,
        Code = entity.Code,
        Name = entity.Name,
        Description = entity.Description,
        Criticality = entity.Criticality,
        Status = entity.Status,
        BusinessArea = entity.BusinessArea.Name,
        OwnerTeam = entity.OwnerTeam.Name,
        LinkedApiVersionCount = entity.ApiLinks.Count,
        ApiVersions = entity.ApiLinks.OrderBy(x => x.ApiVersion.ApiAsset.Name).Select(x => new ProjectApiLinkResponse
        {
            LinkId = x.Id,
            ApiId = x.ApiVersion.ApiAssetId,
            ApiName = x.ApiVersion.ApiAsset.Name,
            ApiVersionId = x.ApiVersionId,
            Version = x.ApiVersion.Version,
            LifecycleStatus = x.ApiVersion.LifecycleStatus,
            Purpose = x.Purpose,
            IsRequired = x.IsRequired
        }).ToList()
    };


    //public async Task<IReadOnlyList<ProjectSummaryResponse>> GetAllAsync(CancellationToken cancellationToken) =>
    //    await dbContext.Projects.AsNoTracking().OrderBy(x => x.Name)
    //        .Select(x => new ProjectSummaryResponse
    //        {
    //            Id = x.Id,
    //            Code = x.Code,
    //            Name = x.Name,
    //            Criticality = x.Criticality,
    //            Status = x.Status,
    //            BusinessArea = x.BusinessArea.Name,
    //            OwnerTeam = x.OwnerTeam.Name,
    //            LinkedApiVersionCount = x.ApiLinks.Count
    //        }).ToListAsync(cancellationToken);

    //public async Task<ProjectDetailResponse> GetAsync(Guid id, CancellationToken cancellationToken)
    //{
    //    var entity = await dbContext.Projects.AsNoTracking()
    //        .Include(x => x.BusinessArea)
    //        .Include(x => x.OwnerTeam)
    //        .Include(x => x.ApiLinks)
    //            .ThenInclude(x => x.ApiVersion)
    //                .ThenInclude(x => x.ApiAsset)
    //        .SingleOrDefaultAsync(x => x.Id == id, cancellationToken)
    //        ?? throw new NotFoundException("Project was not found.");
    //    return Map(entity);
    //}

    //public async Task<ProjectDetailResponse> CreateAsync(CreateProjectRequest request, CancellationToken cancellationToken)
    //{
    //    if (!await dbContext.BusinessAreas.AnyAsync(x => x.Id == request.BusinessAreaId, cancellationToken))
    //        throw new RequestValidationException("Business area does not exist.");
    //    if (!await dbContext.DevelopmentTeams.AnyAsync(x => x.Id == request.OwnerTeamId, cancellationToken))
    //        throw new RequestValidationException("Owner team does not exist.");

    //    var code = request.Code.Trim().ToUpperInvariant();
    //    if (await dbContext.Projects.AnyAsync(x => x.Code.ToUpper() == code, cancellationToken))
    //        throw new ConflictException("A project with the same code already exists.");

    //    var entity = new Project
    //    {
    //        Code = code,
    //        Name = request.Name.Trim(),
    //        Description = request.Description?.Trim(),
    //        Criticality = request.Criticality,
    //        Status = request.Status,
    //        BusinessAreaId = request.BusinessAreaId,
    //        OwnerTeamId = request.OwnerTeamId
    //    };
    //    dbContext.Projects.Add(entity);
    //    await dbContext.SaveChangesAsync(cancellationToken);
    //    return await GetAsync(entity.Id, cancellationToken);
    //}

    //public async Task<ProjectDetailResponse> LinkApiVersionAsync(
    //    Guid projectId,
    //    LinkProjectApiVersionRequest request,
    //    CancellationToken cancellationToken)
    //{
    //    if (!await dbContext.Projects.AnyAsync(x => x.Id == projectId, cancellationToken))
    //        throw new NotFoundException("Project was not found.");
    //    if (!await dbContext.ApiVersions.AnyAsync(x => x.Id == request.ApiVersionId, cancellationToken))
    //        throw new RequestValidationException("API version does not exist.");
    //    if (await dbContext.ProjectApiVersions.AnyAsync(
    //            x => x.ProjectId == projectId && x.ApiVersionId == request.ApiVersionId, cancellationToken))
    //        throw new ConflictException("The project is already linked to this exact API version.");

    //    dbContext.ProjectApiVersions.Add(new ProjectApiVersion
    //    {
    //        ProjectId = projectId,
    //        ApiVersionId = request.ApiVersionId,
    //        Purpose = request.Purpose?.Trim(),
    //        IsRequired = request.IsRequired
    //    });
    //    await dbContext.SaveChangesAsync(cancellationToken);
    //    return await GetAsync(projectId, cancellationToken);
    //}

    //private static ProjectDetailResponse Map(Project entity) => new()
    //{
    //    Id = entity.Id,
    //    Code = entity.Code,
    //    Name = entity.Name,
    //    Description = entity.Description,
    //    Criticality = entity.Criticality,
    //    Status = entity.Status,
    //    BusinessArea = entity.BusinessArea.Name,
    //    OwnerTeam = entity.OwnerTeam.Name,
    //    LinkedApiVersionCount = entity.ApiLinks.Count,
    //    ApiVersions = entity.ApiLinks.OrderBy(x => x.ApiVersion.ApiAsset.Name).Select(x => new ProjectApiLinkResponse
    //    {
    //        LinkId = x.Id,
    //        ApiId = x.ApiVersion.ApiAssetId,
    //        ApiName = x.ApiVersion.ApiAsset.Name,
    //        ApiVersionId = x.ApiVersionId,
    //        Version = x.ApiVersion.Version,
    //        LifecycleStatus = x.ApiVersion.LifecycleStatus,
    //        Purpose = x.Purpose,
    //        IsRequired = x.IsRequired
    //    }).ToList()
    //};
}
