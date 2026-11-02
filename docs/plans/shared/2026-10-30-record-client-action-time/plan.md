# Plan: Record Client Action Time

**Source:** docs/specs/shared/2026-10-30-record-client-action-time.md   **Date:** 2026-10-31   **Status:** In progress
**Risk:** High — new Shared contract, new columns on every table of every module, and two new required headers on every signed-in write

## Summary
7 tasks in 5 batches. Shared reads and checks two device-time headers on every signed-in write,
then a save interceptor stamps the times on every record. The Organization module answers the
business day through a new Shared contract. Each module then gets its migration and one
end-to-end write test, and the OpenAPI (the machine-readable API description) document declares
the headers.

## Tasks
| Order | ID | Task | Batch | Priority | Depends on | Outcome |
|---|---|---|---|---|---|---|
| 1 | T1 | Check the device-time headers on signed-in writes | B1 | P1 | — | #2, #4, #5, #6 |
| 2 | T2 | Stamp the times on every saved record | B1 | P1 | T1 | #1, #2, #3, #7 |
| 3 | T3 | Business day of a location through a Shared contract | B2 | P1 | T2 | #3 |
| 4 | T4 | Organization tables store the times | B2 | P2 | T2 | #8 |
| 5 | T5 | Identity tables store the times | B3 | P2 | T2 | #8 |
| 6 | T6 | Menu tables store the times | B4 | P2 | T2 | #8 |
| 7 | T7 | OpenAPI declares the two required headers | B5 | P2 | T1 | #6 |

## Flags
| Task | New integration | New library | Security |
|---|---|---|---|
| T1 | — | — | New client input on every signed-in write: format, length and bounds checks; refusals run nothing |
| T2 | New Shared contract `ILocationBusinessDay` | — | Interceptor runs on every save of every module |
| T3 | Organization implements the contract | — | Reads location data: must stay inside the token organization |
| T4–T6 | — | — | Migration on every table of the module (no data yet, no backfill) |

## Overall Definition of Done
- [ ] All tasks done, tests passing
- [ ] All Outcomes met (spec Outcomes)
- [ ] Affected tests green after the last batch; full suite green in CI (Continuous Integration)
- [ ] No out-of-scope changes
- [ ] `python3 .claude/scripts/check-security-matrices.py` passes

## Checklist
- [x] T1 — Check the device-time headers on signed-in writes
- [ ] T2 — Stamp the times on every saved record
- [ ] T3 — Business day of a location through a Shared contract
- [ ] T4 — Organization tables store the times
- [ ] T5 — Identity tables store the times
- [ ] T6 — Menu tables store the times
- [ ] T7 — OpenAPI declares the two required headers

## Assumptions
- Times travel as headers `X-Client-Action-Time` and `X-Client-Sent-At` (RFC 3339 date and time with a required offset), with no request body change; rows saved with no signed-in write (registration, background jobs) keep the columns empty — one rule for every module.
- Business day follows the existing rule (location time zone plus the organization's cutoff, 04:00 by default, Organization spec Outcome 6) — it matches spec Outcome 3.
- Action times are refreshed on every update, like `UpdatedAt`; the business day is set once, at creation, so an edit never moves a record to another day.
- Bounds are inclusive: exactly 5 minutes ahead and exactly 7 days old are accepted — the spec says "more than" and "older than".
- The time check runs after the idempotency key lookup: a replay gets the first answer untouched, and a refused write frees its key (existing rule SH104).
