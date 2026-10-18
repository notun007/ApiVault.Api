using ApiVault.Application.Abstractions;
using ApiVault.Application.Common;
using ApiVault.Application.DTOs;
using ApiVault.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace ApiVault.Application.Services;

public sealed class SecurityService(IApplicationDbContext db)
{
    public async Task<IReadOnlyList<RoleResponse>> GetRolesAsync(CancellationToken ct) =>
        await db.SecurityRoles.AsNoTracking().OrderBy(x => x.Name)
            .Select(x => new RoleResponse
            {
                Id = x.Id, Code = x.Code, Name = x.Name, Description = x.Description,
                IsSystemRole = x.IsSystemRole, IsActive = x.IsActive, UserCount = x.UserAssignments.Count
            }).ToListAsync(ct);

    public async Task<RoleResponse> CreateRoleAsync(CreateRoleRequest request, CancellationToken ct)
    {
        var code = request.Code.Trim().ToUpperInvariant();
        var name = request.Name.Trim();
        if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(name))
            throw new RequestValidationException("Role code and name are required.");
        if (await db.SecurityRoles.AnyAsync(x => x.Code == code || x.Name == name, ct))
            throw new ConflictException("A role with the same code or name already exists.");
        var role = new SecurityRole { Code = code, Name = name, Description = request.Description?.Trim(), IsActive = request.IsActive };
        db.SecurityRoles.Add(role);
        await db.SaveChangesAsync(ct);
        return Map(role, 0);
    }

    public async Task<RoleResponse> UpdateRoleAsync(Guid id, CreateRoleRequest request, CancellationToken ct)
    {
        var role = await db.SecurityRoles.SingleOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new NotFoundException("Security role was not found.");
        var code = request.Code.Trim().ToUpperInvariant();
        var name = request.Name.Trim();
        if (await db.SecurityRoles.AnyAsync(x => x.Id != id && (x.Code == code || x.Name == name), ct))
            throw new ConflictException("A role with the same code or name already exists.");
        role.Code = code; role.Name = name; role.Description = request.Description?.Trim(); role.IsActive = request.IsActive;
        await db.SaveChangesAsync(ct);
        return Map(role, await db.AppUserRoles.CountAsync(x => x.RoleId == id, ct));
    }

    public async Task<IReadOnlyList<PermissionResponse>> GetPermissionsAsync(CancellationToken ct) =>
        await db.SecurityPermissions.AsNoTracking().OrderBy(x => x.Code)
            .Select(x => new PermissionResponse { Id = x.Id, Code = x.Code, Name = x.Name, Description = x.Description })
            .ToListAsync(ct);

    public async Task<IReadOnlyList<SecurityScreenResponse>> GetScreensAsync(CancellationToken ct) =>
        await db.SecurityScreens.AsNoTracking().Where(x => x.IsActive).OrderBy(x => x.DisplayOrder)
            .Select(x => new SecurityScreenResponse
            {
                Id = x.Id, Code = x.Code, Name = x.Name, Route = x.Route, Icon = x.Icon,
                ParentId = x.ParentId, DisplayOrder = x.DisplayOrder, IsActive = x.IsActive
            }).ToListAsync(ct);

    public async Task<IReadOnlyList<RolePermissionResponse>> GetRolePermissionsAsync(Guid roleId, CancellationToken ct)
    {
        if (!await db.SecurityRoles.AnyAsync(x => x.Id == roleId, ct))
            throw new NotFoundException("Security role was not found.");
        return await db.RolePermissions.AsNoTracking().Where(x => x.RoleId == roleId)
            .GroupBy(x => x.ScreenId)
            .Select(g => new RolePermissionResponse { ScreenId = g.Key, PermissionIds = g.Select(x => x.PermissionId).ToList() })
            .ToListAsync(ct);
    }

    public async Task UpdateRolePermissionsAsync(Guid roleId, UpdateRolePermissionsRequest request, CancellationToken ct)
    {
        if (!await db.SecurityRoles.AnyAsync(x => x.Id == roleId, ct))
            throw new NotFoundException("Security role was not found.");
        var screenIds = request.Permissions.Select(x => x.ScreenId).ToHashSet();
        var permissionIds = request.Permissions.SelectMany(x => x.PermissionIds).ToHashSet();
        if (await db.SecurityScreens.CountAsync(x => screenIds.Contains(x.Id), ct) != screenIds.Count ||
            await db.SecurityPermissions.CountAsync(x => permissionIds.Contains(x.Id), ct) != permissionIds.Count)
            throw new RequestValidationException("One or more selected screens or permissions are invalid.");

        var existing = await db.RolePermissions.Where(x => x.RoleId == roleId).ToListAsync(ct);
        db.RolePermissions.RemoveRange(existing);
        db.RolePermissions.AddRange(request.Permissions.SelectMany(x => x.PermissionIds.Select(permissionId => new RolePermission
        {
            RoleId = roleId, ScreenId = x.ScreenId, PermissionId = permissionId
        })));
        await db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<ApiAccessCatalogApplicationResponse>> GetApiAccessCatalogAsync(CancellationToken ct) =>
        await db.Projects.AsNoTracking()
            .Where(x => x.PublishedApis.Any())
            .OrderBy(x => x.Name)
            .Select(application => new ApiAccessCatalogApplicationResponse
            {
                Id = application.Id,
                Code = application.Code,
                Name = application.Name,
                Apis = application.PublishedApis.OrderBy(api => api.Name).Select(api => new ApiAccessCatalogApiResponse
                {
                    Id = api.Id,
                    Name = api.Name,
                    Releases = api.Versions.OrderByDescending(version => version.IsCurrent).ThenByDescending(version => version.CreatedAtUtc)
                        .Select(version => new ApiAccessCatalogReleaseResponse
                        {
                            Id = version.Id,
                            Version = version.Version,
                            ReleaseName = version.ReleaseName,
                            LifecycleStatus = version.LifecycleStatus.ToString(),
                            Endpoints = version.Endpoints.OrderBy(endpoint => endpoint.RelativePath).ThenBy(endpoint => endpoint.HttpMethod)
                                .Select(endpoint => new ApiAccessCatalogEndpointResponse
                                {
                                    Id = endpoint.Id,
                                    Name = endpoint.Name,
                                    HttpMethod = endpoint.HttpMethod,
                                    RelativePath = endpoint.RelativePath
                                }).ToList(),
                            Environments = version.Environments.OrderBy(environment => environment.EnvironmentType)
                                .Select(environment => new ApiAccessCatalogEnvironmentResponse
                                {
                                    Id = environment.Id,
                                    EnvironmentType = environment.EnvironmentType.ToString(),
                                    IsEnabled = environment.IsEnabled
                                }).ToList()
                        }).ToList()
                }).ToList()
            }).ToListAsync(ct);

    public async Task<IReadOnlyList<UserApiAccessGrantResponse>> GetUserApiAccessAsync(Guid userId, CancellationToken ct)
    {
        if (!await db.AppUsers.AnyAsync(x => x.Id == userId, ct))
            throw new NotFoundException("User was not found.");

        return await db.UserApiAccessGrants.AsNoTracking()
            .Where(x => x.UserId == userId && x.IsActive)
            .OrderBy(x => x.Application.Name).ThenBy(x => x.ApiEndpoint.ApiVersion.ApiAsset.Name)
            .ThenBy(x => x.ApiEndpoint.ApiVersion.Version).ThenBy(x => x.ApiEndpoint.RelativePath)
            .Select(x => new UserApiAccessGrantResponse
            {
                Id = x.Id,
                UserId = x.UserId,
                ApplicationId = x.ApplicationId,
                ApplicationCode = x.Application.Code,
                ApplicationName = x.Application.Name,
                ApiAssetId = x.ApiEndpoint.ApiVersion.ApiAssetId,
                ApiName = x.ApiEndpoint.ApiVersion.ApiAsset.Name,
                ApiVersionId = x.ApiEndpoint.ApiVersionId,
                ApiVersion = x.ApiEndpoint.ApiVersion.Version,
                ApiEndpointId = x.ApiEndpointId,
                EndpointName = x.ApiEndpoint.Name,
                HttpMethod = x.ApiEndpoint.HttpMethod,
                RelativePath = x.ApiEndpoint.RelativePath,
                ApiEnvironmentId = x.ApiEnvironmentId,
                EnvironmentType = x.ApiEnvironment.EnvironmentType.ToString(),
                CanRead = x.Actions.Any(action => action.ActionCode == UserApiAccessAction.Read),
                CanCreate = x.Actions.Any(action => action.ActionCode == UserApiAccessAction.Create),
                CanEdit = x.Actions.Any(action => action.ActionCode == UserApiAccessAction.Edit)
            }).ToListAsync(ct);
    }

    public async Task UpdateUserApiAccessAsync(Guid userId, SaveUserApiAccessRequest request, CancellationToken ct)
    {
        if (!await db.AppUsers.AnyAsync(x => x.Id == userId && x.IsActive, ct))
            throw new NotFoundException("An active user was not found.");

        var grants = request.Grants ?? [];
        if (grants.Any(x => !x.CanRead || (x.CanCreate || x.CanEdit) && !x.CanRead))
            throw new RequestValidationException("Read permission is required for every API access grant.");

        var duplicate = grants.GroupBy(x => new { x.ApplicationId, x.ApiEndpointId, x.ApiEnvironmentId }).FirstOrDefault(x => x.Count() > 1);
        if (duplicate is not null)
            throw new RequestValidationException("The same endpoint and environment can only be granted once.");

        var endpointIds = grants.Select(x => x.ApiEndpointId).Distinct().ToList();
        var environmentIds = grants.Select(x => x.ApiEnvironmentId).Distinct().ToList();
        var endpoints = await db.ApiEndpoints.AsNoTracking()
            .Where(x => endpointIds.Contains(x.Id))
            .Select(x => new { x.Id, x.ApiVersionId, x.ApiVersion.ApiAssetId, x.ApiVersion.ApiAsset.PublishingApplicationId })
            .ToDictionaryAsync(x => x.Id, ct);
        var environments = await db.ApiEnvironments.AsNoTracking()
            .Where(x => environmentIds.Contains(x.Id))
            .Select(x => new { x.Id, x.ApiVersionId })
            .ToDictionaryAsync(x => x.Id, ct);

        if (endpoints.Count != endpointIds.Count || environments.Count != environmentIds.Count)
            throw new RequestValidationException("One or more selected endpoints or environments do not exist.");

        foreach (var grant in grants)
        {
            var endpoint = endpoints[grant.ApiEndpointId];
            var environment = environments[grant.ApiEnvironmentId];
            if (endpoint.PublishingApplicationId != grant.ApplicationId || endpoint.ApiAssetId != grant.ApiAssetId ||
                endpoint.ApiVersionId != grant.ApiVersionId || environment.ApiVersionId != grant.ApiVersionId)
                throw new RequestValidationException("An API access selection does not match its application, API, release, endpoint, and environment hierarchy.");
        }

        var existing = await db.UserApiAccessGrants
            .Include(x => x.Actions)
            .Where(x => x.UserId == userId)
            .ToListAsync(ct);
        var existingByKey = existing.ToDictionary(x => new ApiAccessKey(x.ApplicationId, x.ApiEndpointId, x.ApiEnvironmentId));
        var requestedKeys = grants.Select(x => new ApiAccessKey(x.ApplicationId, x.ApiEndpointId, x.ApiEnvironmentId)).ToHashSet();

        foreach (var grant in existing.Where(x => x.IsActive && !requestedKeys.Contains(new ApiAccessKey(x.ApplicationId, x.ApiEndpointId, x.ApiEnvironmentId))))
        {
            var oldValue = SerializeGrant(grant, grant.Actions.Select(x => x.ActionCode));
            grant.IsActive = false;
            db.UserApiAccessAudits.Add(CreateApiAccessAudit(grant, "REVOKED", oldValue, SerializeGrant(grant, grant.Actions.Select(x => x.ActionCode))));
        }

        foreach (var requestGrant in grants)
        {
            var key = new ApiAccessKey(requestGrant.ApplicationId, requestGrant.ApiEndpointId, requestGrant.ApiEnvironmentId);
            var requestedActions = BuildActionCodes(requestGrant);
            if (!existingByKey.TryGetValue(key, out var grant))
            {
                grant = new UserApiAccessGrant
                {
                    UserId = userId,
                    ApplicationId = requestGrant.ApplicationId,
                    ApiEndpointId = requestGrant.ApiEndpointId,
                    ApiEnvironmentId = requestGrant.ApiEnvironmentId,
                    IsActive = true
                };
                foreach (var actionCode in requestedActions)
                    grant.Actions.Add(new UserApiAccessAction { Grant = grant, ActionCode = actionCode });
                db.UserApiAccessGrants.Add(grant);
                db.UserApiAccessAudits.Add(CreateApiAccessAudit(grant, "CREATED", null, SerializeGrant(grant, requestedActions)));
                continue;
            }

            var currentActions = grant.Actions.Select(x => x.ActionCode).OrderBy(x => x).ToArray();
            if (grant.IsActive && currentActions.SequenceEqual(requestedActions))
                continue;

            var oldValue = SerializeGrant(grant, currentActions);
            var actionsToRemove = grant.Actions.Where(x => !requestedActions.Contains(x.ActionCode)).ToList();
            db.UserApiAccessActions.RemoveRange(actionsToRemove);
            foreach (var actionCode in requestedActions.Where(x => !currentActions.Contains(x)))
                grant.Actions.Add(new UserApiAccessAction { GrantId = grant.Id, ActionCode = actionCode });
            grant.IsActive = true;
            db.UserApiAccessAudits.Add(CreateApiAccessAudit(grant, "UPDATED", oldValue, SerializeGrant(grant, requestedActions)));
        }

        await db.SaveChangesAsync(ct);
    }

    private static string[] BuildActionCodes(SaveUserApiAccessGrantRequest request)
    {
        var actions = new List<string> { UserApiAccessAction.Read };
        if (request.CanCreate) actions.Add(UserApiAccessAction.Create);
        if (request.CanEdit) actions.Add(UserApiAccessAction.Edit);
        return actions.OrderBy(x => x).ToArray();
    }

    private static UserApiAccessAudit CreateApiAccessAudit(UserApiAccessGrant grant, string actionType, string? oldValue, string? newValue) => new()
    {
        GrantId = grant.Id,
        UserId = grant.UserId,
        ActionType = actionType,
        OldValueJson = oldValue,
        NewValueJson = newValue
    };

    private static string SerializeGrant(UserApiAccessGrant grant, IEnumerable<string> actions) => JsonSerializer.Serialize(new
    {
        grant.Id,
        grant.UserId,
        grant.ApplicationId,
        grant.ApiEndpointId,
        grant.ApiEnvironmentId,
        grant.IsActive,
        grant.ValidFromUtc,
        grant.ValidToUtc,
        Actions = actions.OrderBy(x => x).ToArray()
    });

    private readonly record struct ApiAccessKey(Guid ApplicationId, Guid ApiEndpointId, Guid ApiEnvironmentId);

    private static RoleResponse Map(SecurityRole role, int userCount) => new()
    {
        Id = role.Id, Code = role.Code, Name = role.Name, Description = role.Description,
        IsSystemRole = role.IsSystemRole, IsActive = role.IsActive, UserCount = userCount
    };
}
