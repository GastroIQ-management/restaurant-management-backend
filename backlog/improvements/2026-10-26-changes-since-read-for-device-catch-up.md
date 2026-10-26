# Add a "changes since" read so offline devices can catch up

**Status:** proposed
**Severity:** high
**Origin:** Frontend session of 2026-10-26 on the offline approach (restaurant-management-frontend/.claude/skills/offline-sync/SKILL.md, section 3) · requested by the user
**Date:** 2026-10-26

## Problem
A device that was offline must catch up on what changed while it was away. Reloading every list
in full is slow on a poor connection. A full reload also never tells the device that a record was
deleted, so the device keeps showing removed records. This blocks the frontend offline engine
(offline from day one, decided 2026-10-26).

## Evidence
- No read in `src/` takes a cursor or returns changes since a point; list reads return everything
- `src/RestaurantManagement.Modules.Menu/Services/MenuService.cs:116` — `DeleteMenuAsync` hard-deletes,
  so no trace of the delete is left to report
- `src/RestaurantManagement.Shared/Data/Entities/BaseEntity.cs:7` — `UpdatedAt` exists but is not
  safe as a cursor on its own (see design notes)
- `src/RestaurantManagement.Shared/Middleware/LocationAccessMiddleware.cs:17` — the `X-Location-Id`
  header the read must be scoped by

Design notes:
- Do not use `xmin` as the cursor: it is a 32-bit transaction ID that wraps around and is not
  ordered by commit. Options: a `bigint` change sequence column set on every write, or
  `UpdatedAt` plus ID
- `UpdatedAt` can miss rows when transactions commit out of order. If chosen, the design must
  handle it, for example with a short overlap window and the client deduplicating by ID
- Deletes need tombstones (soft delete or a deleted-records table). Keep them longer than the
  longest a device may stay offline. That limit is frontend offline-sync open decision 4, still
  undecided

## Acceptance criteria
- [ ] Each offline-capable resource has a read that takes an opaque cursor and returns the records
      changed after it, the IDs of records deleted after it (tombstones) and the next cursor
- [ ] The read is paged, scoped by the organization from the token and the `X-Location-Id`
      header, and filtered by the member's current access
- [ ] A change made after the cursor is returned once
- [ ] A delete made after the cursor is returned as a tombstone
- [ ] A cursor older than tombstone retention returns 410 Gone, so the client does a full reload
- [ ] No record from another organization, or from a location the member cannot read, is ever
      returned (tenant leak test in the matrix)
- [ ] A record the member has lost access to stops appearing

## Related
- `backlog/improvements/2026-10-26-client-generated-ids-on-create.md`
- `backlog/improvements/2026-10-26-idempotency-key-on-writes.md`
- `backlog/improvements/2026-10-26-optimistic-concurrency-versions.md`
- `backlog/improvements/2026-10-15-menu-deactivate-instead-of-delete.md` (deactivate vs delete
  decides how menu tombstones work)
- `backlog/features/2026-10-19-filter-lists-by-location-access.md` (same per-location access filter)
- `backlog/improvements/2026-10-15-paginate-organization-and-location-lists.md` (paging)
