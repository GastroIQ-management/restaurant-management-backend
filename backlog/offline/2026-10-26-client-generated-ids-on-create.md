# Accept client-generated IDs on create endpoints

**Status:** proposed
**Severity:** high
**Origin:** Frontend session of 2026-10-26 on the offline approach (restaurant-management-frontend/.claude/skills/offline-sync/SKILL.md, section 3) · requested by the user
**Date:** 2026-10-26

## Problem
The server makes every new record's ID. A create that is retried, or queued while the device is
offline, cannot refer to its record until the server answers. The client must use temporary IDs
and rewrite every later queued request that points to them, which is error-prone. A retried
create with no ID of its own also makes a second record. This blocks the frontend offline engine
(offline from day one, decided 2026-10-26): offline creates depend on it.

## Evidence
- `src/RestaurantManagement.Modules.Menu/Models/CreateMenuRequest.cs:7` — no `Id` field; the same
  holds for `CreateMenuCategoryRequest`, `CreateLocationRequest`, `CreateOrganizationRequest`,
  `CreateRoleRequest` and `CreateInvitationRequest`
- `src/RestaurantManagement.Modules.Menu/Services/MenuService.cs:61` — `Id = Guid.NewGuid()` on create
- `src/RestaurantManagement.Modules.Organization/Services/LocationService.cs:51` — `Id = Guid.NewGuid()` on create
- Tenant query filters hide other organizations' rows, so a "does this ID exist" query cannot see
  a clash with another organization; the primary key violation must decide the 409

## Acceptance criteria
- [ ] Create requests accept an `id` (UUID); the server stores the record under that ID (the app
      sends UUIDv7 or v4)
- [ ] An empty UUID (`00000000-0000-0000-0000-000000000000`) returns 400
- [ ] A UUID already used in the same organization returns 409
- [ ] A UUID used in another organization returns the same plain 409, with nothing about that
      organization in the body (tenant leak test in the module's matrix)
- [ ] The organization still comes only from the token's `org_id` claim
- [ ] The OpenAPI document shows the `id` field on each create request

## Related
- `backlog/offline/2026-10-26-idempotency-key-on-writes.md`
- `backlog/offline/2026-10-26-optimistic-concurrency-versions.md`
- `backlog/offline/2026-10-26-changes-since-read-for-device-catch-up.md`
