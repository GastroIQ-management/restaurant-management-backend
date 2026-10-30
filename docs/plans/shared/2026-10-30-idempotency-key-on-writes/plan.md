# Plan: Idempotency key on writes

**Source:** docs/specs/shared/2026-10-30-idempotency-key-on-writes.md   **Date:** 2026-10-30   **Status:** In progress
**Risk:** High — new Shared contracts (key store, a transaction a service can join, one filter on every signed-in write), a new table and migration in three modules, and a replay answered before the modules' access checks.

## Summary
7 tasks in 5 batches. Shared adds the key table mapping and store, a transaction a service can join, and one MVC (Model-View-Controller: the framework that runs the controllers) filter. The filter checks the `Idempotency-Key` header, replays a finished answer, and saves a new answer in the same transaction as the write. Web registers the filter and shows the header in the OpenAPI document (the API (Application Programming Interface) description that generated clients use). Then each module maps its own key table, so a write and its key always commit together in that module's database, and no transaction spans two modules.

## Tasks
| Order | ID | Task | Batch | Priority | Depends on | Outcome |
|---|---|---|---|---|---|---|
| 1 | T1 | Shared key table mapping, store, settings, purge job and error codes | B1 | P1 | — | #2, #3, #6, #9 |
| 2 | T2 | Shared transaction a service can join, with an undo step when it does not commit | B1 | P1 | — | #8 |
| 3 | T3 | Shared write filter: check, reserve, replay, save with the write | B1 | P1 | T1, T2 | #1–#9 |
| 4 | T4 | Web wiring, OpenAPI header and coverage check | B2 | P1 | T3 | #5, #10 |
| 5 | T5 | Menu writes accept the key | B3 | P1 | T2, T4 | #1, #4, #10 |
| 6 | T6 | Organization writes accept the key | B4 | P1 | T2, T4 | #4, #8, #10 |
| 7 | T7 | Identity writes accept the key | B5 | P2 | T4 | #4, #10 |

## Flags
| Task | New integration | New library | Security |
|---|---|---|---|
| T1 | — | — | New Shared contract: key table and store for every module. A key is looked up only with the token's user and organization. Only a hash of the request is stored, never its body. No raw SQL |
| T2 | — | — | New Shared contract: a service joins the request's transaction on its own context; no transaction ever spans two contexts |
| T3 | — | — | A replay is answered before the module's access checks, only to the same user in the same organization. A write with a key runs in one transaction |
| T4 | — | — | — |
| T5 | — | — | Migration: new table `menu_idempotency_keys` (no data, not launched) |
| T6 | — | — | Migration: new table `organization_idempotency_keys`. Create organization (no organization in the token) scopes the key to the user. The Owner undo still runs when the write does not commit |
| T7 | — | — | Migration: new table `identity_idempotency_keys`. The three session endpoints ignore the key, so no access token is ever stored |

## Overall Definition of Done
- [ ] All tasks done, tests passing
- [ ] All Outcomes met (spec Outcomes, or the backlog item's Acceptance criteria)
- [ ] Affected tests green after the last batch; full suite green in CI (Continuous Integration)
- [ ] No out-of-scope changes

## Checklist
- [x] T1 — Shared key table mapping, store, settings, purge job and error codes
- [x] T2 — Shared transaction a service can join, with an undo step when it does not commit
- [x] T3 — Shared write filter: check, reserve, replay, save with the write
- [ ] T4 — Web wiring, OpenAPI header and coverage check
- [ ] T5 — Menu writes accept the key
- [ ] T6 — Organization writes accept the key
- [ ] T7 — Identity writes accept the key

## Assumptions
- Each module keeps its own key table and saves the key in its own transaction, so it stays correct when modules get separate databases. A key reused on another module's endpoint counts as new there: no 422 across modules — keys are random, so only a client bug reuses one.
- `select-organization`, `logout-all` and `change-password` ignore the key: their answers carry an access token or change the refresh cookie, which must never be stored or replayed.
- "Same request" = same method, path with query, `X-Location-Id` and body. Only its SHA-256 (Secure Hash Algorithm, 256-bit) hash is stored, never the body (passwords, invitation tokens).
- A key whose first request never finished (process crash) is released after 120 seconds (setting `Idempotency:InProgressTimeoutSeconds`). Until then it answers 409 `request.in_progress` (existing retry code).
- A replay is answered before the module's access checks. It holds only that same user's earlier answer in the same organization.
