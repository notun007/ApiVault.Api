# Dynamic navigation

The sidebar now loads its layout from `GET /api/navigation`. Administrators manage it at `/admin/navigation` under **Security & Users → Menu Management**.

## First use

1. Restart the API with the updated application and start/reload ApiVault.Web.
2. Sign in as Admin or SuperAdmin and open `/admin/navigation`.
3. If the layout has not been initialized, select **Initialize default menu** once. This is necessary when database reference-data initialization is disabled. If reference-data seeding is enabled, initialization happens during startup.

The initial layout keeps Dashboard at the top level and creates API Portfolio, Testing, Reference Data, API Access, Security & Users, and Monitoring. API Access contains the existing Access Assignments screen.

## Editing

- **Add group** creates a top-level group in the draft.
- Select a top-level group and use **Add subgroup** for its second group level.
- Select any entry to change its name, icon, enabled state, or parent. Page destinations are registered application routes and cannot be replaced with arbitrary URLs.
- Use the up/down arrow buttons to reorder entries among siblings. Change **Parent group** to move an entry to another group.
- **Apply to draft** updates the local preview; **Save menu** persists the workspace configuration. Save also applies the currently selected entry's edits.
- **Reload saved menu** explicitly discards the draft and reloads the current database configuration. Failed saves retain the draft. Conflicting revisions return HTTP 409.
- Disable unused groups rather than deleting them. Disabled groups hide their descendants; empty groups are omitted from the sidebar.

Two group levels are supported: group → subgroup → page. Page links can also sit directly under a group. Group selection expands/collapses the sidebar; navigating to a page opens its ancestor groups automatically.

## Storage and security

Configuration uses the existing `SEC_SCREEN` table (`ParentId`, `DisplayOrder`, `Icon`, `Name`, `IsActive`). Group rows have a unique `menu-group:` route marker; this marker is never a navigable route and group rows are excluded from the permissions matrix. Existing screen identifiers and permission assignments are preserved. Initial setup normalizes legacy screen route strings to the actual Angular routes. No schema migration is required.

Only Admin and SuperAdmin can read the management layout, initialize it, or save it. Normal navigation returns only active page links allowed by the application's existing role rules, plus their active ancestors. This does not grant API permissions or replace route guards. SuperAdmin now inherits Admin access in the frontend as it already does in API JWT claims.

Layout saves are audited by the existing DbContext audit mechanism. A revision hash detects stale drafts; the existing update timestamp is also used as an EF concurrency token. Dashboard remains enabled at the top level, and Menu Management and its ancestors must remain enabled.

## Verification

- `dotnet run --project scripts/NavigationSmoke/NavigationSmoke.csproj` exercises hierarchy, immutable routes, required links, icon validation, role visibility, and disabled-parent behavior.
- Browser checks exercise initialization, group/subgroup creation, icon selection, reparenting, sibling ordering, save/reload, conflict handling, expanded/collapsed navigation, and mobile behavior with intercepted API responses. They do not modify live workspace data.
- Live authorization smoke checks cover anonymous HTTP 401 and Viewer HTTP 403 on management endpoints. Database persistence needs a connected Oracle instance and was not exercised by those checks.
