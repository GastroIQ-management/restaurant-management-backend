# Context map

How the modules relate. One row per module, one row per relation and direction. The module
designer (`/module`) adds a new module's rows; a plan that adds or changes a relation lists this
file under a task's Files, and the implementer updates it in the same commit.

The "Through" column names the Shared contract a relation uses today; it is empty for a relation
not built yet. Build order and status live only in `docs/roadmap/`.

## Modules
| Module | Owns | Brief |
|---|---|---|
| Identity | User accounts, sign-in and sessions, memberships, invitations, role presets, each member's access grid | None (built before briefs) |
| Organization | Organizations, their plan and settings, locations with their status, time zone and business day | None (built before briefs) |
| Menu | Menus, menu categories, which locations offer each menu | None (built before briefs) |

## Relations
| From | Uses | What passes | Why | Through |
|---|---|---|---|---|
| Identity | Organization | Whether a location belongs to the organization, its non-closed locations, organization summaries | Invitations and access grids name real locations of the member's organization; role changes cover every non-closed location; the sign-in organization list shows names and plans | `IOrganizationLookup` |
| Organization | Identity | Add or undo the Owner membership and default roles of a new organization | Creating an organization makes its creator the Owner, without Organization touching accounts | `IOrganizationAccessSetup` |
| Organization | Identity | The organizations a user belongs to | Listing and creating organizations (one trial per user) for the signed-in user | `IMemberAccessLookup` |
| Menu | Organization | Whether the organization exists, whether a location belongs to it, its non-closed locations | A menu or category is offered only at the organization's own locations | `IOrganizationLookup` |
| Menu | Identity | The member's access level per location for the Menus area | Read to see a location's menus, Manage to change them | Shared access guard (`IAccessGuard`, backed by `IMemberAccessLookup`) |

Every module also uses the platform services in Shared: audit log, idempotency keys, client
action time, changes since last read (device catch-up), email and localization. These are not
module relations and are not listed above.

## Access areas
The areas of the access grid. Each member has, per location and area, None, Read or Manage. A new
area changes the grid and every role preset: always a decision for the user.

| Area | Used by |
|---|---|
| Locations | Organization |
| Menus | Menu |
| Members | Identity |
| Cashier | Not built yet |
| Orders | Not built yet |
| Kitchen | Not built yet |
| Reports | Not built yet |
