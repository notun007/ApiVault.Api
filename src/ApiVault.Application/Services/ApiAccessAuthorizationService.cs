using ApiVault.Application.Abstractions;
using ApiVault.Application.Common;
using ApiVault.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ApiVault.Application.Services;

public sealed class ApiAccessAuthorizationService(
    IApplicationDbContext db,
    IUserContext userContext)
{
    public bool HasUnrestrictedAccess =>
        userContext.IsInRole("Admin") ||
        userContext.IsInRole("SuperAdmin");

    public IQueryable<ApiAsset> ApplyReadFilter(IQueryable<ApiAsset> source)
    {
        if (HasUnrestrictedAccess)
            return source;

        var userId = RequireUserId();
        var now = DateTime.UtcNow;
        return source.Where(api => db.UserApiAccessGrants.Any(grant =>
            grant.UserId == userId &&
            grant.IsActive &&
            (!grant.ValidFromUtc.HasValue || grant.ValidFromUtc.Value <= now) &&
            (!grant.ValidToUtc.HasValue || grant.ValidToUtc.Value >= now) &&
            grant.ApiEndpoint.ApiVersion.ApiAssetId == api.Id &&
            grant.Actions.Any(action => action.ActionCode == UserApiAccessAction.Read)));
    }

    public IQueryable<TestExecution> ApplyHistoryReadFilter(IQueryable<TestExecution> source)
    {
        if (HasUnrestrictedAccess)
            return source;

        var userId = RequireUserId();
        var now = DateTime.UtcNow;
        return source.Where(execution => db.UserApiAccessGrants.Any(grant =>
            grant.UserId == userId &&
            grant.IsActive &&
            (!grant.ValidFromUtc.HasValue || grant.ValidFromUtc.Value <= now) &&
            (!grant.ValidToUtc.HasValue || grant.ValidToUtc.Value >= now) &&
            grant.ApiEndpointId == execution.ApiEndpointId &&
            grant.ApiEnvironmentId == execution.ApiEnvironmentId &&
            grant.Actions.Any(action => action.ActionCode == UserApiAccessAction.Read)));
    }

    public async Task EnsureEndpointAccessAsync(
        Guid endpointId,
        Guid environmentId,
        string actionCode,
        CancellationToken cancellationToken)
    {
        if (HasUnrestrictedAccess)
            return;

        var userId = RequireUserId();
        var now = DateTime.UtcNow;
        var allowed = await db.UserApiAccessGrants.AsNoTracking().AnyAsync(grant =>
            grant.UserId == userId &&
            grant.ApiEndpointId == endpointId &&
            grant.ApiEnvironmentId == environmentId &&
            grant.IsActive &&
            (!grant.ValidFromUtc.HasValue || grant.ValidFromUtc.Value <= now) &&
            (!grant.ValidToUtc.HasValue || grant.ValidToUtc.Value >= now) &&
            grant.Actions.Any(action => action.ActionCode == actionCode),
            cancellationToken);

        if (!allowed)
            throw new ForbiddenException("You do not have permission to access this API endpoint and environment.");
    }

    public async Task EnsureVersionEnvironmentReadAccessAsync(
        Guid versionId,
        Guid environmentId,
        CancellationToken cancellationToken)
    {
        if (HasUnrestrictedAccess)
            return;

        var userId = RequireUserId();
        var now = DateTime.UtcNow;
        var allowed = await ActiveReadGrants(userId, now).AnyAsync(grant =>
            grant.ApiEndpoint.ApiVersionId == versionId &&
            grant.ApiEnvironmentId == environmentId,
            cancellationToken);

        if (!allowed)
            throw new ForbiddenException("You do not have permission to access this API release and environment.");
    }

    public async Task<ApiReadScope> GetApiReadScopeAsync(Guid apiId, CancellationToken cancellationToken)
    {
        if (HasUnrestrictedAccess)
            return ApiReadScope.Unrestricted;

        var userId = RequireUserId();
        var now = DateTime.UtcNow;
        var grants = await ActiveReadGrants(userId, now)
            .Where(grant => grant.ApiEndpoint.ApiVersion.ApiAssetId == apiId)
            .Select(grant => new
            {
                VersionId = grant.ApiEndpoint.ApiVersionId,
                EndpointId = grant.ApiEndpointId,
                EnvironmentId = grant.ApiEnvironmentId
            })
            .ToListAsync(cancellationToken);

        return CreateScope(grants.Select(x => (x.VersionId, x.EndpointId, x.EnvironmentId)));
    }

    public async Task<ApiReadScope> GetVersionReadScopeAsync(Guid versionId, CancellationToken cancellationToken)
    {
        if (HasUnrestrictedAccess)
            return ApiReadScope.Unrestricted;

        var userId = RequireUserId();
        var now = DateTime.UtcNow;
        var grants = await ActiveReadGrants(userId, now)
            .Where(grant => grant.ApiEndpoint.ApiVersionId == versionId)
            .Select(grant => new
            {
                VersionId = grant.ApiEndpoint.ApiVersionId,
                EndpointId = grant.ApiEndpointId,
                EnvironmentId = grant.ApiEnvironmentId
            })
            .ToListAsync(cancellationToken);

        return CreateScope(grants.Select(x => (x.VersionId, x.EndpointId, x.EnvironmentId)));
    }

    private IQueryable<UserApiAccessGrant> ActiveReadGrants(Guid userId, DateTime now) =>
        db.UserApiAccessGrants.AsNoTracking().Where(grant =>
            grant.UserId == userId &&
            grant.IsActive &&
            (!grant.ValidFromUtc.HasValue || grant.ValidFromUtc.Value <= now) &&
            (!grant.ValidToUtc.HasValue || grant.ValidToUtc.Value >= now) &&
            grant.Actions.Any(action => action.ActionCode == UserApiAccessAction.Read));

    private Guid RequireUserId() => userContext.UserId
        ?? throw new UnauthorizedAccessException("The authenticated user identifier is missing.");

    private static ApiReadScope CreateScope(IEnumerable<(Guid VersionId, Guid EndpointId, Guid EnvironmentId)> grants)
    {
        var values = grants.ToList();
        if (values.Count == 0)
            throw new ForbiddenException("You do not have permission to access this API.");

        return new ApiReadScope(
            false,
            values.Select(x => x.VersionId).ToHashSet(),
            values.Select(x => x.EndpointId).ToHashSet(),
            values.Select(x => x.EnvironmentId).ToHashSet());
    }
}

public sealed record ApiReadScope(
    bool IsUnrestricted,
    IReadOnlySet<Guid> VersionIds,
    IReadOnlySet<Guid> EndpointIds,
    IReadOnlySet<Guid> EnvironmentIds)
{
    public static ApiReadScope Unrestricted { get; } = new(
        true,
        new HashSet<Guid>(),
        new HashSet<Guid>(),
        new HashSet<Guid>());
}
