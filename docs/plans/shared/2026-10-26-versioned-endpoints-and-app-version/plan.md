# Plan: Versioned endpoints and app version on writes

**Source:** backlog/offline/2026-10-26-versioned-endpoints-and-app-version.md   **Date:** 2026-10-26   **Status:** In progress
**Risk:** High — every URL and every write's contract change, and the refresh cookie path (auth) changes; no data or schema change, easy to undo before launch

## Summary
7 tasks in 5 batches. Every endpoint moves under `/api/v1/`, written in each controller's route
(no new library); every write (POST, PUT, PATCH, DELETE) must send an `X-App-Version` header, checked
by one Shared middleware; the Web host publishes one OpenAPI document (the machine-readable API
description the frontend generates its client from) per version, and the version rules are written down.
Outcomes, numbered from the item's Acceptance criteria: #1 endpoints are versioned; changes inside a
version are additive only · #2 every write carries the app version · #3 an old version is kept for at
least 30 days after the next one ships · #4 the OpenAPI document is published per version.

## Tasks
| Order | ID | Task | Batch | Priority | Depends on | Outcome |
|---|---|---|---|---|---|---|
| 1 | T1 | Writes require the app version header | B1 | P1 | — | #2 |
| 2 | T2 | Audit log under `/api/v1/` | B1 | P1 | — | #1 |
| 3 | T3 | Organization endpoints under `/api/v1/` | B2 | P1 | — | #1 |
| 4 | T4 | Menu endpoints under `/api/v1/` | B3 | P1 | — | #1 |
| 5 | T5 | Identity endpoints and refresh cookie under `/api/v1/` | B4 | P1 | — | #1 |
| 6 | T6 | One OpenAPI document per version, route guard and version rules | B5 | P1 | T2 T3 T4 T5 | #1 #3 #4 |
| 7 | T7 | OpenAPI declares the app version header on writes | B5 | P2 | T1 T6 | #2 #4 |

## Flags
| Task | New integration | New library | Security |
|---|---|---|---|
| T1 | — | — | New client-supplied header, bounded in length and format, never used for access; public sign-in endpoints (register, login, refresh) also refuse a write without it |
| T5 | — | — | Refresh cookie path changes to `/api/v1/auth` (auth change); every auth route must still start with it exactly (rule T10) |
| T6 | — | — (Swashbuckle already used) | Swagger stays Development only (rule W21) |

## Overall Definition of Done
- [ ] All tasks done, tests passing
- [ ] All Outcomes met (the backlog item's Acceptance criteria)
- [ ] Affected tests green after the last batch; full suite green in CI (Continuous Integration)
- [ ] No out-of-scope changes
- [ ] Backlog item only: deleted if fully solved (deletion left uncommitted for the user), or updated with what is left

## Checklist
- [x] T1 — Writes require the app version header
- [x] T2 — Audit log under `/api/v1/`
- [x] T3 — Organization endpoints under `/api/v1/`
- [x] T4 — Menu endpoints under `/api/v1/`
- [x] T5 — Identity endpoints and refresh cookie under `/api/v1/`
- [x] T6 — One OpenAPI document per version, route guard and version rules
- [ ] T7 — OpenAPI declares the app version header on writes

## Assumptions
- URL path versioning written in each controller's `[Route]`, with no versioning library (no `Asp.Versioning`): a version is a route prefix plus its own OpenAPI document, enough to serve two versions side by side
- `X-App-Version` holds a semantic version (`major.minor.patch`, optional `-`/`+` suffix, 32 characters max); required on POST, PUT, PATCH, DELETE only; checked before sign-in, so anonymous and signed-in callers get the same 400; tagged on Sentry events, not stored
- "Additive only" and "kept 30 days" are written rules (architecture skill and the OpenAPI description) applied from launch; no contract snapshot test, since contracts may still change freely before launch
- The refresh cookie path becomes `/api/v1/auth`; when an auth v2 ships, its cookie path is decided then
- One plan despite more than 10 files: route changes are mechanical, one batch and commit per module
