# Plan: Client-generated IDs on create endpoints

**Source:** backlog/offline/2026-10-26-client-generated-ids-on-create.md   **Date:** 2026-10-27   **Status:** In progress
**Risk:** High — clients choose the primary key on six create endpoints; a careless 409 (HTTP Conflict) could reveal another organization's records.

## Summary
7 tasks in 4 batches. Shared first adds one 409 error code and a primary key clash check; then each module's create requests take a required `id`, stored as the record's ID, with a primary key clash answered by the same plain 409 whichever organization owns the ID.

## Tasks
| Order | ID | Task | Batch | Priority | Depends on | Outcome |
|---|---|---|---|---|---|---|
| 1 | T1 | Shared "ID already used" code and primary key clash check | B1 | P1 | — | #3, #4 |
| 2 | T2 | Menu create takes the client's ID | B2 | P1 | T1 | #1–#6 |
| 3 | T3 | Menu category create takes the client's ID | B2 | P1 | T1 | #1–#6 |
| 4 | T4 | Location create takes the client's ID | B3 | P1 | T1 | #1–#6 |
| 5 | T5 | Organization create takes the client's ID | B3 | P1 | T1 | #1–#4, #6 |
| 6 | T6 | Role create takes the client's ID | B4 | P1 | T1 | #1–#6 |
| 7 | T7 | Invitation create takes the client's ID | B4 | P1 | T1 | #1–#6 |

## Flags
| Task | New integration | New library | Security |
|---|---|---|---|
| T1 | — | — | New Shared error code used by three modules; its text names no record or organization |
| T2 | — | — | Client-chosen primary key; cross-organization 409 equals the same-organization 409; Menus Manage checked before a clash can be seen |
| T3 | — | — | Same as T2, for menu categories |
| T4 | — | — | Same as T2, Owner checked first; a primary key clash must no longer be reported as a taken location code |
| T5 | — | — | Client chooses the organization's own ID; a clash with another user's organization answers the same 409 and creates no Owner access |
| T6 | — | — | Same as T2, role rules (Members Manage) checked first |
| T7 | — | — | Same as T2, delegation rules checked first; the accept token stays separate from the ID; a 409 sends no email |

## Overall Definition of Done
- [ ] All tasks done, tests passing
- [ ] All Outcomes met (the backlog item's Acceptance criteria)
- [ ] Affected tests green after the last batch; full suite green in CI (Continuous Integration)
- [ ] No out-of-scope changes
- [ ] Backlog item only: deleted if fully solved (deletion left uncommitted for the user), or updated with what is left

## Checklist
- [x] T1 — Shared "ID already used" code and primary key clash check
- [x] T2 — Menu create takes the client's ID
- [x] T3 — Menu category create takes the client's ID
- [ ] T4 — Location create takes the client's ID
- [ ] T5 — Organization create takes the client's ID
- [ ] T6 — Role create takes the client's ID
- [ ] T7 — Invitation create takes the client's ID

## Assumptions
- `id` is required: missing, null, malformed or empty all answer 400, like other required IDs (Shared row SH20) — not launched yet, so one way to create; existing tests that post these creates get an `id` in step 3.
- One Shared code `id.already_used` (409, not a retry code) serves all six creates, not one code per module — the client handles one case.
- Any non-empty UUID (Universally Unique Identifier) is accepted; no check for version 4 or 7 — the item only says what the app sends.
- Only the created record takes the client's ID; rows made with it (location links, organization settings, Owner access, role levels, invitation grids) keep server-made IDs.
- Kept as one plan (about 20 source and matrix files) instead of one plan per module — each module is its own batch.
