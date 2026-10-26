# Accept an Idempotency-Key header on all writes

**Status:** proposed
**Severity:** high
**Origin:** Frontend session of 2026-10-26 on the offline approach (restaurant-management-frontend/.claude/skills/offline-sync/SKILL.md, section 3) · requested by the user
**Date:** 2026-10-26

## Problem
Sometimes the server applies a write but the response never reaches the client (flaky Wi-Fi, a
timeout). The client then retries and the write runs twice: a double tap or a retry can
duplicate a record today, and later a duplicate order or a double payment. Client-generated IDs
protect only creates. Writes such as close bill, record payment and change quantity also need
this protection. This blocks the frontend offline engine (offline from day one, decided
2026-10-26): every replayed write from the outbox carries a key.

## Evidence
- No write endpoint reads an `Idempotency-Key` header; nothing in `src/` stores request keys
- `src/RestaurantManagement.Shared/Middleware/` — holds only `ExceptionHandlingMiddleware` and
  `LocationAccessMiddleware`
- Each module writes through its own `DbContext` (`IdentityDbContext`, `OrganizationDbContext`,
  `MenuDbContext`). Module rules forbid sharing a transaction across modules, so a single Shared
  key table cannot sit in "the same transaction as the write". The design must decide whether each
  module stores its own keys or the key row is saved separately with an in-progress marker

## Acceptance criteria
- [ ] POST, PUT, PATCH and DELETE accept `Idempotency-Key: <uuid>` through one shared endpoint
      filter or middleware
- [ ] The stored key row holds organization, user, key (unique together), endpoint, request hash,
      status code, response body and created-at
- [ ] A repeated key with the same request returns the saved response without running the write again
- [ ] The same key with a different request body returns 422
- [ ] The same key while the first request is still running returns 409
- [ ] A key sent by another user or another organization never returns the first user's response
      (tenant leak test in the matrix)
- [ ] A crash between the write and the key row never leaves a write applied with no key, and
      never leaves a key with no write
- [ ] Keys expire after a configurable time, longer than the 7-day offline limit (sessions end
      after 7 days unused, decided 2026-10-26). Assumption: default 8 days. Expired rows are purged

## Related
- `backlog/offline/2026-10-26-client-generated-ids-on-create.md`
- `backlog/offline/2026-10-26-optimistic-concurrency-versions.md`
- `backlog/offline/2026-10-26-changes-since-read-for-device-catch-up.md`
- `backlog/improvements/2026-10-16-audit-entries-through-outbox.md` (same cross-module transaction question)
