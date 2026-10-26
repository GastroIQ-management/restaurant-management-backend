# Prevent lost updates with record versions (optimistic concurrency)

**Status:** proposed
**Severity:** high
**Origin:** Frontend session of 2026-10-26 on the offline approach (restaurant-management-frontend/.claude/skills/offline-sync/SKILL.md, section 3) · requested by the user
**Date:** 2026-10-26

## Problem
When two clients edit the same record, the later write silently overwrites the earlier one. This
happens online today (two tabs, two managers editing a menu or a location) and gets worse when
offline edits are replayed later. Nobody is told that their change was lost. This blocks the
frontend offline engine (offline from day one, decided 2026-10-26): replayed edits need a
version to detect conflicts.

## Evidence
- No entity has a concurrency token: no `IsConcurrencyToken`, `[Timestamp]`, `xmin` or
  `UseXminAsConcurrencyToken` in `src/`, and no `DbUpdateConcurrencyException` handling
- `src/RestaurantManagement.Modules.Menu/Services/MenuService.cs:98` — `EditMenuAsync` loads and
  saves with no version check
- `src/RestaurantManagement.Modules.Organization/Services/LocationService.cs:75` — `EditLocationAsync`
  does the same
- `src/RestaurantManagement.Modules.Menu/Models/MenuResponse.cs:6` — responses carry no version

## Acceptance criteria
- [ ] Editable entities use PostgreSQL `xmin` as the concurrency token
      (`[Timestamp] uint Version` or `UseXminAsConcurrencyToken`)
- [ ] GET responses include `version`
- [ ] Update and delete requests send back the version they read (in the body or `If-Match`)
- [ ] An update or delete with a stale version returns 409 with the current record in the body
- [ ] An update with the current version succeeds and returns the new version
- [ ] The OpenAPI document shows the `version` fields, so the frontend's `pnpm api:generate`
      picks them up

## Related
- `backlog/improvements/2026-10-26-client-generated-ids-on-create.md`
- `backlog/improvements/2026-10-26-idempotency-key-on-writes.md`
- `backlog/improvements/2026-10-26-changes-since-read-for-device-catch-up.md`
