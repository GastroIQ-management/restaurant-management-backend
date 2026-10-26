# Return every error as Problem Details with a stable code

**Status:** proposed
**Severity:** high
**Origin:** User decisions of 2026-10-26 on offline-first (backend conversation) · error codes
**Date:** 2026-10-26

## Problem
Errors carry only a status code and a localized message. When a queued write fails on replay,
the app cannot tell a retry from a permanent failure, or one failure from another, without
parsing translated text. Error bodies also come in two shapes. This blocks the frontend offline
engine (offline from day one, decided 2026-10-26).

## Evidence
- `src/RestaurantManagement.Shared/Middleware/ExceptionHandlingMiddleware.cs:37` — business errors
  return `application/json` with `{ message }` only, no code
- `src/RestaurantManagement.Shared/Middleware/ExceptionHandlingMiddleware.cs:49` — unexpected
  errors return `{ message, detail }`
- `src/RestaurantManagement.Web/Hosting/LocalizedValidationTitleExtensions.cs:21` — validation
  errors already use `ProblemDetails`, so the formats differ
- 409 has two meanings: "still in progress" (Idempotency-Key, retry) and a lasting conflict
  (ID already used, stale version, no retry). Only the code can tell them apart

## Acceptance criteria
- [ ] Every error response is RFC 9457 Problem Details (`application/problem+json`) with a stable
      `code` field, such as `order.closed`, `item.not_found` or `validation.failed`
- [ ] Codes are fixed strings, never translated; `title` and `detail` stay localized
- [ ] Each 409 carries a code that says whether it is "in progress" (retry) or a lasting conflict
- [ ] The app retries on network errors, 5xx, 409 "in progress" and 429; every other error is
      permanent or needs the user's attention
- [ ] The codes appear in the OpenAPI document

## Related
- `docs/specs/shared/2026-10-15-localize-client-messages.md`
- `backlog/offline/2026-10-26-idempotency-key-on-writes.md`
- `backlog/offline/2026-10-26-client-generated-ids-on-create.md`
- `backlog/offline/2026-10-26-optimistic-concurrency-versions.md`
