using ApiVault.Application.Abstractions;
using ApiVault.Application.Common;
using ApiVault.Application.DTOs;
using ApiVault.Domain.Entities;
using Microsoft.EntityFrameworkCore;

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

    private static RoleResponse Map(SecurityRole role, int userCount) => new()
    {
        Id = role.Id, Code = role.Code, Name = role.Name, Description = role.Description,
        IsSystemRole = role.IsSystemRole, IsActive = role.IsActive, UserCount = userCount
    };
}
