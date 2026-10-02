using ApiVault.Application.Abstractions;
using ApiVault.Application.Common;
using ApiVault.Application.DTOs;
using ApiVault.Domain.Entities;
using ApiVault.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace ApiVault.Application.Services;

public sealed class ProjectService(IApplicationDbContext dbContext)
{
    public async Task<IReadOnlyList<ProjectSummaryResponse>> GetAllAsync(bool activeOnly, CancellationToken cancellationToken)
    {
        var query = dbContext.Projects.AsNoTracking().AsQueryable();
        if (activeOnly) query = query.Where(x => x.Status == ProjectStatus.Active);

        return await query.OrderBy(x => x.Name).Select(x => new ProjectSummaryResponse
        {
            Id = x.Id,
            Code = x.Code,
            Name = x.Name,
            Description = x.Description,
            Criticality = x.Criticality,
            Status = x.Status,
            BusinessAreaId = x.BusinessAreaId,
            BusinessArea = x.BusinessArea == null ? "Unassigned" : x.BusinessArea.Name,
            OwnerTeamId = x.OwnerTeamId,
            OwnerTeam = x.OwnerTeam == null ? "Unassigned" : x.OwnerTeam.Name,
            OwnershipType = x.OwnershipType,
            VendorId = x.VendorId,
            VendorName = x.Vendor == null ? null : x.Vendor.Name,
            LinkedApiVersionCount = x.ApiLinks.Count,
            PublishedApiCount = x.PublishedApis.Count
        }).ToListAsync(cancellationToken);
    }

    public async Task<ProjectDetailResponse> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        var entity = await dbContext.Projects.AsNoTracking()
            .Include(x => x.BusinessArea)
            .Include(x => x.OwnerTeam)
            .Include(x => x.Vendor)
            .Include(x => x.ApiLinks).ThenInclude(x => x.ApiVersion).ThenInclude(x => x.ApiAsset)
            .Include(x => x.PublishedApis).ThenInclude(x => x.Versions)
            .SingleOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new NotFoundException("Application or system was not found.");
        return Map(entity);
    }

    public async Task<ProjectDetailResponse> CreateAsync(CreateProjectRequest request, CancellationToken cancellationToken)
    {
        await ValidateAsync(null, request, cancellationToken);
        var entity = new Project();
        Apply(entity, request);
        dbContext.Projects.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
        return await GetAsync(entity.Id, cancellationToken);
    }

    public async Task<ProjectDetailResponse> UpdateAsync(Guid id, CreateProjectRequest request, CancellationToken cancellationToken)
    {
        var entity = await dbContext.Projects.SingleOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new NotFoundException("Application or system was not found.");
        await ValidateAsync(id, request, cancellationToken);
        Apply(entity, request);
        await dbContext.SaveChangesAsync(cancellationToken);
        return await GetAsync(id, cancellationToken);
    }

    public async Task<ProjectDetailResponse> LinkApiVersionAsync(
        Guid projectId,
        LinkProjectApiVersionRequest request,
        CancellationToken cancellationToken)
    {
        if (!await dbContext.Projects.AnyAsync(x => x.Id == projectId, cancellationToken))
            throw new NotFoundException("Application or system was not found.");
        if (!await dbContext.ApiVersions.AnyAsync(x => x.Id == request.ApiVersionId, cancellationToken))
            throw new RequestValidationException("API version does not exist.");
        if (await dbContext.ProjectApiVersions.AnyAsync(
                x => x.ProjectId == projectId && x.ApiVersionId == request.ApiVersionId, cancellationToken))
            throw new ConflictException("The application is already linked to this exact API version.");

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

    private async Task ValidateAsync(Guid? id, CreateProjectRequest request, CancellationToken cancellationToken)
    {
        var code = request.Code.Trim().ToUpperInvariant();
        var name = request.Name.Trim();
        if (string.IsNullOrWhiteSpace(code)) throw new RequestValidationException("Application code is required.");
        if (string.IsNullOrWhiteSpace(name)) throw new RequestValidationException("Application name is required.");
        if (await dbContext.Projects.AnyAsync(
                x => x.Id != id && (x.Code.ToUpper() == code || x.Name.ToUpper() == name.ToUpper()), cancellationToken))
            throw new ConflictException("An application or system with the same code or name already exists.");
        if (!await dbContext.BusinessAreas.AnyAsync(x => x.Id == request.BusinessAreaId, cancellationToken))
            throw new RequestValidationException("Business area does not exist.");
        if (!await dbContext.DevelopmentTeams.AnyAsync(x => x.Id == request.OwnerTeamId, cancellationToken))
            throw new RequestValidationException("Owner team does not exist.");
        if (request.OwnershipType == ApiOwnershipType.ThirdParty)
        {
            if (!request.VendorId.HasValue)
                throw new RequestValidationException("Vendor is required for a third-party application.");
            if (!await dbContext.Vendors.AnyAsync(x => x.Id == request.VendorId && x.IsActive, cancellationToken))
                throw new RequestValidationException("Vendor does not exist or is inactive.");
        }
    }

    private static void Apply(Project entity, CreateProjectRequest request)
    {
        entity.Code = request.Code.Trim().ToUpperInvariant();
        entity.Name = request.Name.Trim();
        entity.Description = request.Description?.Trim();
        entity.Criticality = request.Criticality;
        entity.Status = request.Status;
        entity.BusinessAreaId = request.BusinessAreaId;
        entity.OwnerTeamId = request.OwnerTeamId;
        entity.OwnershipType = request.OwnershipType;
        entity.VendorId = request.OwnershipType == ApiOwnershipType.ThirdParty ? request.VendorId : null;
    }

    private static ProjectDetailResponse Map(Project entity) => new()
    {
        Id = entity.Id,
        Code = entity.Code,
        Name = entity.Name,
        Description = entity.Description,
        Criticality = entity.Criticality,
        Status = entity.Status,
        BusinessAreaId = entity.BusinessAreaId,
        BusinessArea = entity.BusinessArea?.Name ?? "Unassigned",
        OwnerTeamId = entity.OwnerTeamId,
        OwnerTeam = entity.OwnerTeam?.Name ?? "Unassigned",
        OwnershipType = entity.OwnershipType,
        VendorId = entity.VendorId,
        VendorName = entity.Vendor?.Name,
        LinkedApiVersionCount = entity.ApiLinks.Count,
        PublishedApiCount = entity.PublishedApis.Count,
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
        }).ToList(),
        PublishedApis = entity.PublishedApis.OrderBy(x => x.Name).Select(x => new ApplicationPublishedApiResponse
        {
            ApiId = x.Id,
            ApiName = x.Name,
            VersionCount = x.Versions.Count
        }).ToList()
    };
}
