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
            .Select(x => new LookupResponse { Id = x.Id, Code = x.Code, Name = x.Name })
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<LookupResponse>> GetDevelopmentTeamsAsync(CancellationToken cancellationToken) =>
        await dbContext.DevelopmentTeams.AsNoTracking().OrderBy(x => x.Name)
            .Select(x => new LookupResponse { Id = x.Id, Code = x.Code, Name = x.Name })
            .ToListAsync(cancellationToken);

    public async Task<LookupResponse> CreateBusinessAreaAsync(CreateLookupRequest request, CancellationToken cancellationToken)
    {
        var code = request.Code.Trim().ToUpperInvariant();
        if (await dbContext.BusinessAreas.AnyAsync(x => x.Code.ToUpper() == code, cancellationToken))
            throw new ConflictException("Business area code already exists.");
        var entity = new BusinessArea { Code = code, Name = request.Name.Trim(), Description = request.Description?.Trim() };
        dbContext.BusinessAreas.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
        return new LookupResponse { Id = entity.Id, Code = entity.Code, Name = entity.Name };
    }

    public async Task<LookupResponse> CreateDevelopmentTeamAsync(CreateLookupRequest request, CancellationToken cancellationToken)
    {
        var code = request.Code.Trim().ToUpperInvariant();
        if (await dbContext.DevelopmentTeams.AnyAsync(x => x.Code.ToUpper() == code, cancellationToken))
            throw new ConflictException("Development team code already exists.");
        var entity = new DevelopmentTeam
        {
            Code = code,
            Name = request.Name.Trim(),
            Description = request.Description?.Trim(),
            ContactEmail = request.ContactEmail?.Trim()
        };
        dbContext.DevelopmentTeams.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
        return new LookupResponse { Id = entity.Id, Code = entity.Code, Name = entity.Name };
    }
}
