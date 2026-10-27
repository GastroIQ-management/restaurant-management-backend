# Plan: Problem Details with stable error codes

**Source:** backlog/offline/2026-10-26-problem-details-with-stable-error-codes.md   **Date:** 2026-10-26   **Status:** In progress
**Risk:** High — new Shared contract: every error body of every endpoint changes shape, sign-in refusals included; no data or schema change

## Summary
9 tasks in 8 batches. Shared gets one error answer, RFC 9457 Problem Details (Request for Comments
9457, the standard error body sent as `application/problem+json`) with a fixed `code` and a code
catalog; the Web host, each module and the OpenAPI document (the machine-readable API description
the frontend generates its client from) then adopt it, and the code-less error constructor goes last.
Outcomes, numbered from the item's Acceptance criteria: #1 every error is Problem Details with a
stable `code` · #2 codes never translated, `title` and `detail` localized · #3 each 409 says "in
progress" (retry) or lasting conflict · #4 the app retries only on network errors, 5xx (server
errors 500–599), 409 "in progress" and 429 · #5 the codes appear in the OpenAPI document.

## Tasks
| Order | ID | Task | Batch | Priority | Depends on | Outcome |
|---|---|---|---|---|---|---|
| 1 | T1 | Shared Problem Details answer and code catalog | B1 | P1 | — | #1 #2 #3 #4 |
| 2 | T2 | Shared refusals name their codes | B1 | P1 | T1 | #1 #2 |
| 3 | T3 | Host answers (token, permission, rate limit, validation, framework) as Problem Details | B2 | P1 | T1 | #1 #2 #4 |
| 4 | T4 | Menu refusals name their codes | B3 | P2 | T1 | #1 #2 |
| 5 | T5 | Organization refusals name their codes | B4 | P2 | T1 | #1 #2 |
| 6 | T6 | Identity sign-in and account refusals name their codes | B5 | P2 | T1 | #1 #2 |
| 7 | T7 | Identity members, roles and invitations refusals name their codes | B6 | P2 | T1 | #1 #2 #3 |
| 8 | T8 | Error codes and retry rule in the OpenAPI document | B7 | P2 | T2 T3 T4 T5 T6 T7 | #4 #5 |
| 9 | T9 | Remove the code-less error constructor | B8 | P2 | T2 T4 T5 T6 T7 | #1 |

## Flags
| Task | New integration | New library | Security |
|---|---|---|---|
| T1 | — | — | Shared error contract changes for every endpoint; 500 keeps hiding the exception text outside Development |
| T2 | — | — | Another organization's location and a location without access keep one answer and one code |
| T3 | — | — | Auth refusals (401, 403) change shape; a forged and an expired token keep one code and say nothing about why |
| T4, T5, T7 | — | — | Another organization's item answers the same 404 and code as a missing one |
| T6 | — | — | Every collapsed login and verify failure keeps one status, one body and one code (no account enumeration) |
| T8 | — | — (Swashbuckle already used) | — |

## Overall Definition of Done
- [ ] All tasks done, tests passing
- [ ] All Outcomes met (the backlog item's Acceptance criteria)
- [ ] Affected tests green after the last batch; full suite green in CI (Continuous Integration)
- [ ] No out-of-scope changes; findings in the backlog
- [ ] Every touched module's matrix updated; `python3 .claude/scripts/check-security-matrices.py` passes
- [ ] Backlog item only: deleted if fully solved (deletion left uncommitted for the user), or updated with what is left

## Checklist
- [x] T1 — Shared Problem Details answer and code catalog
- [x] T2 — Shared refusals name their codes
- [x] T3 — Host answers as Problem Details
- [x] T4 — Menu refusals name their codes
- [ ] T5 — Organization refusals name their codes
- [ ] T6 — Identity sign-in and account refusals name their codes
- [ ] T7 — Identity members, roles and invitations refusals name their codes
- [ ] T8 — Error codes and retry rule in the OpenAPI document
- [ ] T9 — Remove the code-less error constructor

## Assumptions
- `title` holds the localized message (today's `message`); `detail` only holds the Development-only exception text on 500 — RFC 9457 `title` is the per-problem summary, one per code
- Status codes stay as they are (a used location code stays 400); only the body and codes change — a status change is a behaviour change outside the item
- `request.in_progress` is reserved now and first returned by the Idempotency-Key item; today's only 409 (two invitations to one address at once) is a lasting conflict
- The retry rule is published (catalog flag and OpenAPI), not enforced: the server only guarantees that retry codes are 5xx, 429 and `request.in_progress`
- One plan despite more than 10 files: the change per module is mechanical, and each module is its own batch and commit
