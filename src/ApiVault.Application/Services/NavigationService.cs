using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using ApiVault.Application.Abstractions;
using ApiVault.Application.Common;
using ApiVault.Application.DTOs;
using ApiVault.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ApiVault.Application.Services;

// Group markers keep the existing non-null, unique SEC_SCREEN.Route intact.
// They are never exposed as browser routes or permission-bearing screens.
public sealed class NavigationService(IApplicationDbContext db, IUserContext user)
{
    public const string GroupPrefix = "menu-group:";
    public static readonly string[] Icons = ["dashboard", "api", "applications", "test", "history", "reference", "vendor", "users", "password", "access", "roles", "permissions", "audit", "folder", "settings"];
    public sealed record Page(string Code, string Name, string Route, string Icon, string? Group, string[] Roles);
    public static readonly Page[] Pages = [
        new("DASHBOARD", "Dashboard", "/dashboard", "dashboard", null, []),
        new("API_CATALOG", "API Catalog", "/apis", "api", "PORTFOLIO", []),
        new("PROJECTS", "Applications & Systems", "/projects", "applications", "PORTFOLIO", []),
        new("TEST_CONSOLE", "Test Console", "/testing", "test", "TESTING", ["Admin", "ApiOwner", "Tester"]),
        new("TEST_HISTORY", "Test History", "/test-history", "history", "TESTING", []),
        new("REFERENCE_DATA", "Business Areas", "/admin/business-areas", "reference", "REFERENCE", ["Admin"]),
        new("VENDORS", "Vendor Companies", "/admin/vendors", "vendor", "REFERENCE", ["Admin", "SuperAdmin"]),
        new("API_ACCESS", "Access Assignments", "/admin/security/api-access", "access", "API_ACCESS_GROUP", ["Admin"]),
        new("USERS", "User Accounts", "/admin/users", "users", "SECURITY", ["Admin"]),
        new("USER_ROLES", "User Roles", "/admin/security/user-access", "roles", "SECURITY", ["Admin"]),
        new("RESET_PASSWORD", "Reset Password", "/admin/reset-password", "password", "SECURITY", ["Admin", "SuperAdmin"]),
        new("SECURITY_ROLES", "Roles", "/admin/security/roles", "roles", "SECURITY", ["Admin"]),
        new("SECURITY_PERMISSIONS", "Permissions", "/admin/security/permissions", "permissions", "SECURITY", ["Admin"]),
        new("MENU_MANAGEMENT", "Menu Management", "/admin/navigation", "settings", "SECURITY", ["Admin", "SuperAdmin"]),
        new("AUDIT_LOGS", "Audit Logs", "/admin/audit", "audit", "MONITORING", ["Admin"])
    ];

    public async Task InitializeAsync(CancellationToken ct)
    {
        // This marker is also the one-time initialization flag. Never reset an edited layout.
        if (await db.SecurityScreens.AnyAsync(x => x.Code == "MENU_MANAGEMENT", ct)) return;
        var groups = new (string Code, string Name, string Icon)[] {
            ("PORTFOLIO", "API Portfolio", "api"), ("TESTING", "Testing", "test"),
            ("REFERENCE", "Reference Data", "reference"), ("API_ACCESS_GROUP", "API Access", "access"),
            ("SECURITY", "Security & Users", "users"), ("MONITORING", "Monitoring", "audit") };
        var parents = groups.Select((g, i) => new SecurityScreen { Code = "NAV_" + g.Code,
            Name = g.Name, Icon = g.Icon, Route = GroupPrefix + g.Code, DisplayOrder = (i + 1) * 10 }).ToArray();
        db.SecurityScreens.AddRange(parents);
        var existing = await db.SecurityScreens.ToListAsync(ct);
        foreach (var (page, index) in Pages.Select((p, i) => (p, i)))
        {
            var screen = existing.SingleOrDefault(x => x.Code == page.Code);
            if (screen is null) { screen = new SecurityScreen { Code = page.Code }; db.SecurityScreens.Add(screen); }
            screen.Name = page.Name; screen.Route = page.Route; screen.Icon = page.Icon;
            screen.ParentId = page.Group is null ? null : parents.Single(x => x.Code == "NAV_" + page.Group).Id;
            screen.DisplayOrder = page.Group is null ? 0 : index * 10;
        }
        await db.SaveChangesAsync(ct);
    }

    public async Task<NavigationLayout> GetLayoutAsync(bool management, CancellationToken ct)
    {
        var screens = await db.SecurityScreens.AsNoTracking().ToListAsync(ct);
        if (!screens.Any(x => x.Code == "MENU_MANAGEMENT")) return new(Revision([]), []);
        var entries = screens.Where(x => x.Route.StartsWith(GroupPrefix, StringComparison.Ordinal) || Pages.Any(p => p.Code == x.Code))
            .Select(Map).OrderBy(x => x.DisplayOrder).ThenBy(x => x.Name).ToArray();
        if (management) return new(Revision(entries), entries);
        return new(Revision(entries), VisibleEntries(entries, user.IsInRole));
    }

    public static IReadOnlyList<NavigationEntry> VisibleEntries(IReadOnlyList<NavigationEntry> entries, Func<string, bool> isInRole)
    {
        var byId = entries.ToDictionary(x => x.Id);
        bool Active(NavigationEntry item) => item.IsActive && (item.ParentId is null || byId.TryGetValue(item.ParentId.Value, out var parent) && Active(parent));
        var visible = new HashSet<Guid>();
        foreach (var page in entries.Where(x => !x.IsGroup && Active(x)))
        {
            var definition = Pages.Single(p => p.Route == page.Route);
            if (definition.Roles.Length > 0 && !definition.Roles.Any(isInRole)) continue;
            visible.Add(page.Id);
            var parentId = page.ParentId;
            while (parentId is Guid id && byId.TryGetValue(id, out var parent)) { visible.Add(id); parentId = parent.ParentId; }
        }
        return entries.Where(x => visible.Contains(x.Id)).ToArray();
    }

    public async Task<NavigationLayout> SaveAsync(SaveNavigationRequest request, CancellationToken ct)
    {
        var current = await GetLayoutAsync(true, ct);
        if (current.Revision != request.Revision) throw new ConflictException("The menu changed since you opened it. Reload before saving.");
        Validate(request.Items, current.Items);
        var rows = await db.SecurityScreens.ToListAsync(ct);
        foreach (var item in request.Items)
        {
            var row = rows.SingleOrDefault(x => x.Id == item.Id);
            if (row is null) {
                row = new SecurityScreen { Id = item.Id, Code = "NAV_" + item.Id.ToString("N"), Route = GroupPrefix + item.Id.ToString("N") };
                db.SecurityScreens.Add(row);
            }
            row.Name = item.Name.Trim(); row.Icon = item.Icon; row.ParentId = item.ParentId;
            row.DisplayOrder = item.DisplayOrder; row.IsActive = item.IsActive;
        }
        // Groups are deactivated rather than deleted so historical permission records remain intact.
        foreach (var row in rows.Where(x => x.Route.StartsWith(GroupPrefix, StringComparison.Ordinal) && request.Items.All(i => i.Id != x.Id)))
        { row.IsActive = false; row.ParentId = null; }
        try { await db.SaveChangesAsync(ct); }
        catch (DbUpdateConcurrencyException) { throw new ConflictException("Another administrator changed the menu. Reload before saving."); }
        return await GetLayoutAsync(true, ct);
    }

    public static void Validate(IReadOnlyList<NavigationEntry> items, IReadOnlyList<NavigationEntry> current)
    {
        if (items.Count is 0 or > 200 || items.Any(x => x.Id == Guid.Empty) || items.Select(x => x.Id).Distinct().Count() != items.Count)
            throw new RequestValidationException("The menu must contain 1–200 entries with unique identifiers.");
        var byId = items.ToDictionary(x => x.Id);
        foreach (var page in current.Where(x => !x.IsGroup))
            if (!byId.TryGetValue(page.Id, out var updated) || updated.Route != page.Route || updated.IsGroup)
                throw new RequestValidationException("Existing page links cannot be deleted or changed to a different route.");
        foreach (var item in items)
        {
            if (string.IsNullOrWhiteSpace(item.Name) || item.Name.Trim().Length > 150 || !Icons.Contains(item.Icon) || item.DisplayOrder < 0)
                throw new RequestValidationException("Each entry needs a name, a supported icon, and a non-negative order.");
            if (!item.IsGroup && !current.Any(x => x.Id == item.Id && !x.IsGroup) || item.IsGroup && item.Route is not null)
                throw new RequestValidationException("New entries must be groups. Choose an existing page to move a page link.");
            var visited = new HashSet<Guid> { item.Id }; var groupCount = item.IsGroup ? 1 : 0; var parentId = item.ParentId;
            while (parentId is Guid id)
            {
                if (!visited.Add(id) || !byId.TryGetValue(id, out var parent) || !parent.IsGroup)
                    throw new RequestValidationException("A parent must be an existing group; circular nesting is not allowed.");
                if (++groupCount > 2) throw new RequestValidationException("Use at most two group levels: group, subgroup, then page.");
                if (item.Route == "/admin/navigation" && !parent.IsActive)
                    throw new RequestValidationException("The group containing Menu Management must stay enabled.");
                parentId = parent.ParentId;
            }
            if (item.Route is "/dashboard" or "/admin/navigation" && !item.IsActive)
                throw new RequestValidationException("Dashboard and Menu Management must stay enabled.");
            if (item.Route == "/dashboard" && item.ParentId is not null)
                throw new RequestValidationException("Dashboard must stay at the top level.");
        }
    }

    private static NavigationEntry Map(SecurityScreen x) => new(x.Id, x.Name,
        x.Route.StartsWith(GroupPrefix, StringComparison.Ordinal) ? null : x.Route, x.Icon ?? "folder", x.ParentId, x.DisplayOrder, x.IsActive, x.Route.StartsWith(GroupPrefix, StringComparison.Ordinal));
    private static string Revision(IEnumerable<NavigationEntry> items) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(items.OrderBy(x => x.Id)))));
}
