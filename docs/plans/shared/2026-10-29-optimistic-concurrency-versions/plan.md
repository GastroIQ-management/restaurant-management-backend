# Plan: Record versions to prevent lost updates (optimistic concurrency)

**Source:** backlog/offline/2026-10-26-optimistic-concurrency-versions.md   **Date:** 2026-10-29   **Status:** In progress
**Risk:** High — new Shared contract (versioned entity, version conflict error carrying a record) and a model change plus migration on six tables in three modules; a 409 (HTTP Conflict) answered before the access check would hand the current record to a caller who may not change it.

## Summary
6 tasks in 4 batches. Shared first adds the version mapping (PostgreSQL `xmin`: the hidden per-row number that changes on every update), one `version.conflict` 409 code whose body carries the current record, and the check that compares the sent version; then each module gives its editable records a `version` in every response and requires it back on update and delete.

## Tasks
| Order | ID | Task | Batch | Priority | Depends on | Outcome |
|---|---|---|---|---|---|---|
| 1 | T1 | Shared version mapping, version check and `version.conflict` 409 | B1 | P1 | — | #1, #4, #5 |
| 2 | T2 | Menu edit and delete check the version | B2 | P1 | T1 | #1–#6 |
| 3 | T3 | Menu category update and delete check the version | B2 | P1 | T1, T2 | #1–#6 |
| 4 | T4 | Location edit and delete check the version | B3 | P1 | T1 | #1–#6 |
| 5 | T5 | Organization edit checks the version | B3 | P2 | T1, T4 | #1–#6 |
| 6 | T6 | Role rename and levels update check the version | B4 | P2 | T1 | #1–#6 |

Outcomes (the backlog item's Acceptance criteria, in order): #1 `xmin` is the concurrency token ·
#2 GET responses include `version` · #3 update and delete send back the version they read ·
#4 a stale version answers 409 with the current record · #5 the current version succeeds and
returns the new version · #6 the OpenAPI (API description) document shows the `version` fields.

## Flags
| Task | New integration | New library | Security |
|---|---|---|---|
| T1 | — | — | New Shared contract (versioned entity, version check, 409 with a record in the body); the body is written only after the caller passed the 404 and 403 checks |
| T2 | — | — | 409 with the current menu: Menus Manage checked first; another organization's menu answers 404, never 409 |
| T3 | — | — | Same as T2, for categories (Menus Manage at the category's location) |
| T4 | — | — | Same as T2, Locations Manage (Owner for closing or deleting) checked first |
| T5 | — | — | Owner checked first; the organization comes from the token only |
| T6 | — | — | Role rules checked first; a levels change must change the role's version (child rows alone would not) |

## Overall Definition of Done
- [ ] All tasks done, tests passing
- [ ] All Outcomes met (the backlog item's Acceptance criteria)
- [ ] Affected tests green after the last batch; full suite green in CI (Continuous Integration)
- [ ] No out-of-scope changes
- [ ] Backlog item only: deleted if fully solved (deletion left uncommitted for the user), or updated with what is left

## Checklist
- [x] T1 — Shared version mapping, version check and `version.conflict` 409
- [x] T2 — Menu edit and delete check the version
- [x] T3 — Menu category update and delete check the version
- [x] T4 — Location edit and delete check the version
- [x] T5 — Organization edit checks the version
- [x] T6 — Role rename and levels update check the version

## Assumptions
- Updates send `version` in the JSON body; deletes send it as a required `?version=` query parameter (no body on DELETE, no `If-Match` header) — both appear in the OpenAPI document; a missing version answers 400, translated (not launched, so no fallback); existing tests that edit or delete get a version in step 3.
- Order of answers: 404, then 403, then 409 `version.conflict`, then other rules (taken code or name); a stale version on a record deleted meanwhile answers 404.
- "Editable entities" = menus, menu categories, locations, organizations (with their settings, one version on the organization row) and roles; single-value or create/revoke-only endpoints (member access cell, user language, invitation revoke, menu-location links) stay last-write-wins.
- The 409 body is the Shared problem answer (code `version.conflict`) with the record's normal response, version included, under `current`.
- Kept as one plan (about 30 source, migration and matrix files) like the client-generated IDs plan: each module is its own batch.
