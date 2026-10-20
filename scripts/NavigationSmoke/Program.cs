using ApiVault.Application.Common;
using ApiVault.Application.DTOs;
using ApiVault.Application.Services;

NavigationEntry Group(string name, Guid? parent = null) => new(Guid.NewGuid(), name, null, "folder", parent, 10, true, true);
var root = Group("API Access");
var subgroup = Group("Assignments", root.Id);
var dashboard = new NavigationEntry(Guid.NewGuid(), "Dashboard", "/dashboard", "dashboard", null, 0, true, false);
var manager = new NavigationEntry(Guid.NewGuid(), "Menu Management", "/admin/navigation", "settings", root.Id, 20, true, false);
var page = new NavigationEntry(Guid.NewGuid(), "Access Assignments", "/admin/security/api-access", "access", subgroup.Id, 10, true, false);
NavigationEntry[] current = [root, subgroup, dashboard, manager, page];
var checks = 0;
void Valid(string name, NavigationEntry[] entries) { NavigationService.Validate(entries, current); Console.WriteLine("PASS " + name); checks++; }
void Invalid(string name, NavigationEntry[] entries) {
    try { NavigationService.Validate(entries, current); throw new Exception("Expected rejection: " + name); }
    catch (RequestValidationException) { Console.WriteLine("PASS rejected " + name); checks++; }
}
Valid("group, subgroup, page", current);
var added = Group("New group");
Valid("new group and moved page", [root, subgroup, dashboard, manager, page with { ParentId = added.Id, DisplayOrder = 100 }, added]);
Valid("disabled nonessential group", [root, subgroup with { IsActive = false }, dashboard, manager, page]);
Invalid("cycle", [root with { ParentId = subgroup.Id }, subgroup, dashboard, manager, page]);
var third = Group("Too deep", subgroup.Id);
Invalid("third group level", [..current, third]);
Invalid("unknown page", [..current, new(Guid.NewGuid(), "Other", "/not-registered", "api", null, 0, true, false)]);
Invalid("missing page", [root, subgroup, dashboard, manager]);
Invalid("changed route", [root, subgroup, dashboard, manager, page with { Route = "/other" }]);
Invalid("page used as parent", [root, subgroup, dashboard, manager, page with { ParentId = dashboard.Id }]);
Invalid("disabled management ancestor", [root with { IsActive = false }, subgroup, dashboard, manager, page]);
Invalid("disabled dashboard", [root, subgroup, dashboard with { IsActive = false }, manager, page]);
Invalid("nested dashboard", [root, subgroup, dashboard with { ParentId = root.Id }, manager, page]);
Invalid("duplicate identifier", [..current, root]);
Invalid("unsupported icon", [root with { Icon = "<script>" }, subgroup, dashboard, manager, page]);
Invalid("empty label", [root with { Name = " " }, subgroup, dashboard, manager, page]);
var viewerMenu = NavigationService.VisibleEntries(current, _ => false);
if (viewerMenu.Count != 1 || viewerMenu[0].Id != dashboard.Id) throw new Exception("Viewer must not see restricted links or empty groups.");
Console.WriteLine("PASS viewer filtering and empty-group pruning"); checks++;
var adminMenu = NavigationService.VisibleEntries(current, role => role == "Admin");
if (adminMenu.Count != current.Length) throw new Exception("Admin must see nested permitted links and their ancestors.");
Console.WriteLine("PASS authorized ancestor inclusion"); checks++;
var hiddenMenu = NavigationService.VisibleEntries([root, subgroup with { IsActive = false }, dashboard, manager, page], role => role == "Admin");
if (hiddenMenu.Any(x => x.Id == page.Id || x.Id == subgroup.Id)) throw new Exception("A disabled parent must hide its descendants.");
Console.WriteLine("PASS disabled-parent propagation"); checks++;
Console.WriteLine($"{checks} navigation checks passed.");
