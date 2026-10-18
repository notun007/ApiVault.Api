using ApiVault.Application.Abstractions;
using ApiVault.Application.Common;
using ApiVault.Application.DTOs;
using ApiVault.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ApiVault.Application.Services;

public sealed class ReferenceDataService(IApplicationDbContext dbContext)
{
    public async Task<IReadOnlyList<LookupResponse>> GetBusinessAreasAsync(CancellationToken cancellationToken) =>
        await dbContext.BusinessAreas.AsNoTracking().OrderBy(x => x.Name)
            .Select(x => new LookupResponse
            {
                Id = x.Id,
                Code = x.Code,
                Name = x.Name,
                Description = x.Description,
                ReferenceCount = x.Apis.Count + x.Projects.Count,
                CanDelete = !x.Apis.Any() && !x.Projects.Any()
            })
            .ToListAsync(cancellationToken);

    public async Task<LookupResponse> CreateBusinessAreaAsync(CreateLookupRequest request, CancellationToken cancellationToken)
    {
        var code = request.Code.Trim().ToUpperInvariant();
        if (await dbContext.BusinessAreas.AnyAsync(x => x.Code.ToUpper() == code, cancellationToken))
            throw new ConflictException("Business area code already exists.");
        var entity = new BusinessArea { Code = code, Name = request.Name.Trim(), Description = request.Description?.Trim() };
        dbContext.BusinessAreas.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Map(entity);
    }

    public async Task<LookupResponse> UpdateBusinessAreaAsync(
        Guid id,
        CreateLookupRequest request,
        CancellationToken cancellationToken)
    {
        var entity = await dbContext.BusinessAreas.SingleOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new NotFoundException("Business area was not found.");
        var code = request.Code.Trim().ToUpperInvariant();
        if (await dbContext.BusinessAreas.AnyAsync(x => x.Id != id && x.Code.ToUpper() == code, cancellationToken))
            throw new ConflictException("Business area code already exists.");

        entity.Code = code;
        entity.Name = request.Name.Trim();
        entity.Description = Clean(request.Description);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Map(entity);
    }

    public async Task DeleteBusinessAreaAsync(Guid id, CancellationToken cancellationToken)
    {
        var entity = await dbContext.BusinessAreas.SingleOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new NotFoundException("Business area was not found.");
        if (await dbContext.ApiAssets.AnyAsync(x => x.BusinessAreaId == id, cancellationToken) ||
            await dbContext.Projects.AnyAsync(x => x.BusinessAreaId == id, cancellationToken))
            throw new ConflictException("Business area cannot be deleted because it is referenced by an API or application/system.");

        dbContext.BusinessAreas.Remove(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static LookupResponse Map(BusinessArea entity) => new()
    {
        Id = entity.Id,
        Code = entity.Code,
        Name = entity.Name,
        Description = entity.Description,
        CanDelete = true
    };

}
