# Review: Record versions to prevent lost updates (optimistic concurrency)

**Verdict:** Fix first — the 409 body writes enums as numbers (an OR61 test will fail), and no test run has proved T3–T6 because the Web.Tests suite hangs.
**Build:** pass (11 projects, 0 errors, 0 warnings)   **Tests:** full suite runs in CI (Continuous Integration) — merge only when it is green

Security matrix check (`python3 .claude/scripts/check-security-matrices.py`): passes. No packages added, so the vulnerable-package check was not needed.

## Fix first
| # | File:line | Problem | Change |
|---|---|---|---|
| 1 | `src/RestaurantManagement.Shared/Utils/Exceptions/ProblemResponse.cs:27` | `WriteAsJsonAsync(problem, options: null, …)` uses the minimal-API (application programming interface) `Microsoft.AspNetCore.Http.Json.JsonOptions`. Only the MVC (Model-View-Controller) options get `JsonStringEnumConverter` (`Program.cs:118`). So `current` writes `status`, `type` (location) and `currency` (organization) as numbers, while every normal response writes them as strings. The frontend gets two shapes for one record. `LocationVersionEndpointTests.cs:109` (OR61) expects `"Active"` and will fail | In `Program.cs`, add `builder.Services.ConfigureHttpJsonOptions(o => o.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));` so problem answers serialize like controller answers. Another option: resolve `IOptions<Microsoft.AspNetCore.Mvc.JsonOptions>` in `ProblemResponse` and pass its `JsonSerializerOptions` |
| 2 | `docs/plans/shared/2026-10-29-optimistic-concurrency-versions/plan.md:3,35-39` and the matrices | Completeness: no final test run took place (the Web.Tests suite hangs at about 360 tests). Rows M67–M72, OR60–OR71, T159–T164 and WD12 are still ❌. The Overall DoD (Definition of Done: the checks that must pass to count as finished) is not ticked. Checklist items have no commit hashes. Status is "In progress". CI runs the same full suite, so it will hang too and can never go green | Find and fix the hang first. Start with `033c373`: it is the only change on this branch to the shared Web.Tests setup (static container, `Lazy` template build, `pg_terminate_backend`, `max_connections=500`). Run `git checkout origin/development -- tests/RestaurantManagement.Web.Tests/PostgresDatabaseFixture.cs tests/RestaurantManagement.Web.Tests/TenantApiFactory.cs` locally and run the suite to see whether it still hangs. Then run the suite, turn the rows ✅, tick the DoD, add each task's hash (T1 `ba540c7`, T2 `0fbce68`, T3 `bcaaa75`, T4 `9da8548`, T5 `ca960a2`, T6 `ceba4ed`) and set Status to Done |

## Watch out
| # | File:line | Problem | Suggestion |
|---|---|---|---|
| 1 | `src/RestaurantManagement.Modules.Menu/Migrations/20261030182028_VersionMenus.cs:13` (and `VersionMenuCategories`, `VersionLocations`, `VersionOrganizations`, `VersionRoles`) | All five migrations were emptied by hand, and a comment was added. The ef-migration skill forbids this: only `migrationBuilder.Sql` backfills may be added by hand. Npgsql 9.0.3 has a system-column check (`IsSystemColumn`) that may already skip `AddColumn xmin` when it generates SQL, so the generated migration may have been safe | Regenerate one migration and run `dotnet ef migrations script` on it. If it emits no `xmin` SQL, keep the generated `Up`/`Down` in all five. If it does, keep them empty and state the exception in the pull request (below) |
| 2 | `src/RestaurantManagement.Modules.Identity/Services/Roles/RoleService.cs:200`, `src/RestaurantManagement.Modules.Organization/Services/LocationService.cs:191` | `SaveRoleAsync(Guid? organizationId = null, Guid? roleId = null)` and `SaveAsync(Guid? locationId = null)` choose between two save behaviours based on whether an optional ID is null (a hidden flag argument) | Split each into two methods: the plain save (for create) and a versioned save that takes the IDs it needs. Both keep the existing duplicate-key catch |
| 3 | `src/RestaurantManagement.Modules.Organization/Services/OrganizationService.cs` (316 lines) | The file was already over the 300-line limit (305). It grew by 11 lines | Move the settings audit helper (`RecordSettingsChangeAsync` and its describe code) into its own class in `Services/`. Or drop this item if the fix pass has no room |

## Check before merging
- **409 body ordering (all six endpoints):** the current record goes into the body only after the 404 and 403 checks. The code follows this everywhere, but only the Menu (M59–M66) and Shared rows have a recorded passing run. The cross-tenant rows (M71, OR64, OR70, T162) and the "refused caller, stale version" rows (M72, OR65, OR69, T163) are the security-critical ones. Confirm they pass in CI.
- **Web.Tests fixture change (`033c373`) and workflow change (`e5edbe1`):** neither is a plan task. The user chose to keep both on this branch. The fixture change replaces how every endpoint test gets its database, and it is the main suspect for the hang.
- **Role rename order (`RoleService.cs`, `RenameAsync`):** the taken-name check now runs after the role rules and the version check. Before, it ran before the rules. A refused caller renaming to a taken name now gets 403 instead of 409 `role.name_taken`. This is stricter and matches the plan, but it changes existing behaviour.

## Decisions for you
None

## Pull request description
**Record versions to prevent lost updates (optimistic concurrency)**

Two people editing the same record no longer silently overwrite each other. Every editable record now carries a `version`. Updates and deletes must send back the version they read. A stale version answers 409 `version.conflict` with the record as stored now under `current`. That body is sent only after the not-found (404) and access (403) checks pass.

What changed per module:
- **Shared:** `IVersionedEntity` and `ApplyVersionTokens` map `Version` to PostgreSQL `xmin` (a plain concurrency token on SQLite). `VersionCheck.Require` / `SaveAsync` also turn a lost race into the same conflict. New code `ErrorCodes.VersionConflict` (409) has its title in en, pt and fr. The middleware writes `current` into the problem answer.
- **Menu:** menus and menu categories carry `version`. Edit requires it in the body; delete requires `?version=`.
- **Organization:** locations (edit, close) and the organization edit carry and require `version`. One version covers the organization and its settings.
- **Identity:** role rename and levels update require `version`. A levels change also bumps the role's version.
- **Tests infrastructure (not a plan task):** Web.Tests share one PostgreSQL container and copy a migrated template database (`033c373`).
- **Workflow (not a plan task):** the implementer runs tests once after the last batch, not per task (`e5edbe1`).

Outcomes delivered (backlog item `backlog/offline/2026-10-26-optimistic-concurrency-versions.md`): #1 `xmin` is the concurrency token · #2 responses include `version` · #3 update and delete send the version back · #4 a stale version answers 409 with the current record · #5 the current version succeeds and returns the new one · #6 the OpenAPI document shows `version` (required on requests and on every delete).

Risk: **High.** It adds a new Shared contract (versioned entity, a 409 that carries a record) and a model change on six tables in three modules. Flags: the 409 body is written only after the access checks; another organization's record answers 404, never 409.

Breaking for clients: every update and delete on these endpoints now requires `version` (400 when missing). This is allowed before launch.

Migrations: `VersionMenus`, `VersionMenuCategories`, `VersionLocations`, `VersionOrganizations`, `VersionRoles`. They add no column (`xmin` is a PostgreSQL system column), so they apply no SQL; only the model snapshots change.

Backlog: when merged, delete `backlog/offline/2026-10-26-optimistic-concurrency-versions.md` (fully solved).

🤖 Generated with [Claude Code](https://claude.com/claude-code)
