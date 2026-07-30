using ApiVault.Application.Abstractions;
using ApiVault.Application.Common;
using ApiVault.Application.DTOs;
using ApiVault.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ApiVault.Application.Services;

public sealed class ApiProjectService(IApplicationDbContext dbContext)
{
    public async Task<IReadOnlyList<ApiProjectResponse>> GetAllAsync(bool activeOnly, CancellationToken cancellationToken)
    {
        var query = dbContext.ApiProjects.AsNoTracking().AsQueryable();
        if (activeOnly) query = query.Where(x => x.IsActive);
        return await query.OrderBy(x => x.Name).Select(MapProjection).ToListAsync(cancellationToken);
    }

    public async Task<ApiProjectResponse> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        var entity = await dbContext.ApiProjects.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new NotFoundException("API project was not found.");
        return Map(entity);
    }

    public async Task<ApiProjectResponse> CreateAsync(CreateApiProjectRequest request, CancellationToken cancellationToken)
    {
        var code = request.Code.Trim().ToUpperInvariant();
        var name = request.Name.Trim();
        if (string.IsNullOrWhiteSpace(code))
            throw new RequestValidationException("API project code is required.");
        if (string.IsNullOrWhiteSpace(name))
            throw new RequestValidationException("API project name is required.");

        if (await dbContext.ApiProjects.AnyAsync(x => x.Code.ToUpper() == code || x.Name.ToUpper() == name.ToUpper(), cancellationToken))
            throw new ConflictException("An API project with the same code or name already exists.");

        var entity = new ApiProject { Code = code, Name = name, Description = request.Description?.Trim(), IsActive = request.IsActive };
        dbContext.ApiProjects.Add(entity);

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (IsUniqueConstraintViolation(exception))
        {
            throw new ConflictException("An API project with the same code or name already exists.");
        }

        return Map(entity);
    }

    public async Task<ApiProjectResponse> UpdateAsync(Guid id, CreateApiProjectRequest request, CancellationToken cancellationToken)
    {
        var entity = await dbContext.ApiProjects.SingleOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new NotFoundException("API project was not found.");
        var code = request.Code.Trim().ToUpperInvariant();
        var name = request.Name.Trim();
        if (await dbContext.ApiProjects.AnyAsync(x => x.Id != id && (x.Code.ToUpper() == code || x.Name.ToUpper() == name.ToUpper()), cancellationToken))
            throw new ConflictException("An API project with the same code or name already exists.");
        entity.Code = code; entity.Name = name; entity.Description = request.Description?.Trim(); entity.IsActive = request.IsActive;
        await dbContext.SaveChangesAsync(cancellationToken);
        return Map(entity);
    }

    public async Task DeactivateAsync(Guid id, CancellationToken cancellationToken)
    {
        var entity = await dbContext.ApiProjects.SingleOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new NotFoundException("API project was not found.");
        entity.IsActive = false;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static ApiProjectResponse Map(ApiProject x) => new() { Id=x.Id, Code=x.Code, Name=x.Name, Description=x.Description, IsActive=x.IsActive, ApiCount=x.Apis?.Count ?? 0 };
    private static readonly System.Linq.Expressions.Expression<Func<ApiProject, ApiProjectResponse>> MapProjection = x => new ApiProjectResponse { Id=x.Id, Code=x.Code, Name=x.Name, Description=x.Description, IsActive=x.IsActive, ApiCount=x.Apis.Count };

    private static bool IsUniqueConstraintViolation(DbUpdateException exception) =>
        exception.InnerException?.Message.Contains("duplicate", StringComparison.OrdinalIgnoreCase) == true ||
        exception.InnerException?.Message.Contains("unique", StringComparison.OrdinalIgnoreCase) == true ||
        exception.InnerException?.Message.Contains("2601", StringComparison.OrdinalIgnoreCase) == true ||
        exception.InnerException?.Message.Contains("2627", StringComparison.OrdinalIgnoreCase) == true;
}
