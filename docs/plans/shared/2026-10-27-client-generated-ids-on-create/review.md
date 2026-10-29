# Review: Client-generated IDs on create endpoints

**Verdict:** Ready to merge
**Build:** pass (0 errors, 0 warnings)   **Tests:** full suite runs in CI — merge only when it is green

Second review, after the fix round for the first review's Fix first 1 (user chose option A). Commits `578fd86` (role) and `60607dc` (location) now answer a retried create (same ID, same name or code) with 409 `id.already_used`. Each fix looks up the ID in the token organization only, before the name or code check. Another organization's ID still reaches the primary key check, so the cross-tenant answer is unchanged. In `RoleService.CreateAsync`, the role rules now run before the ID and name checks, so a caller without the right gets 403 before any 409 or 400. Matrix rows T158 and OR59 are ✅, and their tests are in new files (user-approved after Gate 2). `check-security-matrices.py` passes. No existing test changed in this round. The existing role unit tests still reach the name error: they send an empty ID, and no seeded role has that ID.

## Watch out
| # | File:line | Problem | Suggestion |
|---|---|---|---|
| 1 | `.claude/skills/security/SKILL.md:31-32` | Still open from the first review. Section 3 says "Never accept `Id` … from the client", but six create requests now take one on purpose. The next reviewer or implementer will read this as a violation | Add an exception line: create requests may take the record's `id`. A clash answers the same 409 `id.already_used` whichever organization owns the ID, and only the token organization is ever queried for it |
| 2 | `src/RestaurantManagement.Modules.Identity/Services/Invitations/InvitationService.cs:94` | Still open from the first review. The retry guard runs after `DescribeAsync`, token generation and `ChooseLanguageAsync`, so a refused retry still does that work | Move the guard directly after the `replaced` query (line 90) |

## Check before merging
- `PrimaryKeyViolation.Matches` (`src/RestaurantManagement.Shared/Utils/Exceptions/PrimaryKeyViolation.cs`) matches any constraint whose name starts with `pk_`. This holds because EFCore.NamingConventions writes lower-case names. A hand-named primary key would turn the 409 into a 500.
- The new pre-checks rely on tenant scoping. Location (`LocationService.cs:70`) relies on the `Locations` query filter. Role (`RoleService.cs:173`) filters `OrganizationId` explicitly, because the Identity context has no tenant filter (see the untracked backlog item). If either scope were lost, a 409 from that pre-check would confirm that another organization uses the ID. The answer would still be the same 409 body, so nothing else would leak.
- `id` is now required on six create requests. Every client must send it from now on, and a request without it gets 400.

## Decisions for you
None

## Pull request description
**Accept client-generated IDs on create endpoints**

The app can now choose a new record's ID itself. Offline and retried creates can then refer to the record before the server answers. The source is `backlog/offline/2026-10-26-client-generated-ids-on-create.md`; that file's deletion is left for the user to commit.

What changed:
- **Shared:** new error code `id.already_used` (409, not a retry code), with English, Portuguese and French text that names no record or organization. New `PrimaryKeyViolation.Matches` recognises a primary key clash from the PostgreSQL error, never from a cross-organization "ID exists" query.
- **Menu:** `POST api/v1/menus/create` and `POST api/v1/menu-categories` take a required `id`.
- **Organization:** `POST api/v1/locations` and `POST api/v1/organizations/create` take a required `id`. A location primary key clash is no longer reported as a taken location code. A retried location create with the same ID and code answers 409, not "code already used".
- **Identity:** `POST api/v1/roles` and `POST api/v1/invitations` take a required `id`. A retried role create with the same ID and name answers 409, not "name already used". Role rules are checked first. A retried invitation with the same ID is refused before the open invitation is replaced, and no second email is sent.

Outcomes delivered:
- The record is stored under the client's `id`, and the response returns it.
- An empty, missing or malformed `id` answers 400.
- A reused `id` answers 409 `id.already_used`, including a retry with the same body. The body is the same whichever organization owns the ID and holds nothing about that organization.
- The organization still comes only from the token's `org_id`.
- The OpenAPI document shows `id` (uuid, required) on all six create requests.
- Access and rule checks run before the ID clash, so a caller without the right gets 403, never 409.

Risk: High (plan flag). Clients now choose primary keys. A tenant test in each module's matrix proves the cross-tenant 409 (M51, M56, OR51, OR56, T150, T155). Matrices updated: Shared SH71–SH72, Menu M49–M58, Organization OR49–OR59, Identity T148–T158, Web WD11. No migration and no new package.

Client note: `id` is now required on these six creates, so every client must send a UUID (v4 or v7).

Test changes after Gate 2, all approved by the user:
- `OrganizationClientIdEndpointTests` line 228 now selects the organization by ID.
- `LocationFieldsTests` and `LocationMessagesEndpointTests` now send an `id`.
- `InvitationWebSupport.InvitationsAsync` now filters by organization.
- New `RoleClientIdRetryEndpointTests` (T158) and `LocationClientIdRetryEndpointTests` (OR59) cover the retry fix.

T2 and T3 share commit `16ba282`.

🤖 Generated with [Claude Code](https://claude.com/claude-code)
