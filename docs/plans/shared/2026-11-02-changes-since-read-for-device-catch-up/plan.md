# Plan: Changes-Since Read for Device Catch-Up

**Source:** docs/specs/shared/2026-11-02-changes-since-read-for-device-catch-up.md   **Date:** 2026-11-02   **Status:** In progress
**Risk:** High — new Shared contract, schema changes with hand-written database triggers in two migrations, new reads of tenant data

## Summary
8 tasks in 4 batches. Every write stamps the row with the PostgreSQL 64-bit transaction ID (xid8)
of the writing transaction, and every delete leaves a tombstone row, both through database
triggers. A changes read returns rows stamped at or after the cursor's lower bound; the next bound
is the oldest transaction still running (snapshot xmin: the lowest in-flight transaction ID) when
the read began, so a write that commits late is never missed (it may come back twice). A guard
test and an architecture rule keep future modules from shipping tenant data without it.

## Tasks
| Order | ID | Task | Batch | Priority | Depends on | Outcome |
|---|---|---|---|---|---|---|
| 1 | T1 | Change cursor, page contract and error codes | B1 | P1 | — | #6 |
| 2 | T2 | Change stamps, tombstones and change reader | B1 | P1 | T1 | #5, #10 |
| 3 | T3 | Stamp and tombstone the Menu tables | B2 | P1 | T2 | #3 |
| 4 | T4 | Menus and menu-location links changes reads | B2 | P1 | T1, T3 | #1, #2, #4, #5, #6, #7, #8, #9, #10 |
| 5 | T5 | Menu categories changes read | B2 | P2 | T4 | #1, #3, #7, #8 |
| 6 | T6 | Stamp the locations table | B3 | P1 | T2 | #11 |
| 7 | T7 | Locations changes read | B3 | P2 | T1, T6 | #1, #7, #11 |
| 8 | T8 | Guard and rule: offline-capable modules keep change tracking | B4 | P2 | T3, T4, T5, T6, T7 | User request 2026-11-02 (future modules) |

## Flags
| Task | New integration | New library | Security |
|---|---|---|---|
| T1 | — | — | New Shared contract (cursor, page, error codes `changes.cursor_invalid` 400, `changes.cursor_expired` 410, `location.required` 400) |
| T2 | — | — | Raw SQL: trigger SQL text and the snapshot query; new Shared contract (model builder extension) |
| T3 | — | — | Raw SQL in a migration (`migrationBuilder.Sql` triggers: an exception to the ef-migration rule "only backfills"); tombstones carry organization and location |
| T4 | — | — | Two new endpoints reading tenant data per location |
| T5 | — | — | New endpoint reading tenant data per location |
| T6 | — | — | Raw SQL in a migration (stamp trigger on `locations`, same exception) |
| T7 | — | — | New endpoint reading tenant data |
| T8 | — | — | — (changes the ef-migration skill to allow the `ChangeTrackingSql` triggers as a hand edit) |

## Overall Definition of Done
- [ ] All tasks done, tests passing
- [ ] All Outcomes met (spec Outcomes)
- [ ] Affected tests green after the last batch; full suite green in CI (Continuous Integration)
- [ ] No out-of-scope changes
- [ ] `python3 .claude/scripts/check-security-matrices.py` passes with the shared, menu, organization and web matrix rows ✅

## Checklist
- [x] T1 — Change cursor, page contract and error codes
- [x] T2 — Change stamps, tombstones and change reader
- [x] T3 — Stamp and tombstone the Menu tables
- [x] T4 — Menus and menu-location links changes reads
- [x] T5 — Menu categories changes read
- [ ] T6 — Stamp the locations table
- [ ] T7 — Locations changes read
- [ ] T8 — Guard and rule: offline-capable modules keep change tracking

## Assumptions
- Decided by the user (2026-11-02): a member with no Read at the header location is refused with 403 on that location's feeds (Outcome 9: the device drops that location's data); tombstones are sent only for records leaving a location the member can still read (for example a detached menu).
- Stamps and tombstones come from database triggers (hand-written `migrationBuilder.Sql`): EF Core (Entity Framework Core) cannot stamp updates with the transaction ID or see rows removed by database cascades (a menu delete removes its links and categories).
- The cursor is encoded, unsigned data bound to feed, organization and location; one that fails to decode or is bound elsewhere is refused with 400. Tampering gains nothing: every read filters by token organization, header location and access.
- The categories feed returns only categories of the header location, like `GET api/v1/locations/{locationId}/menu-categories`; categories with no location are not synced.
- The guard's exclusion list lives in the test file with a reason per entry (Identity access data, `OrganizationSettings`, `AuditLogEntry`), not as an attribute on entities, so no Identity or Shared entity file changes.
