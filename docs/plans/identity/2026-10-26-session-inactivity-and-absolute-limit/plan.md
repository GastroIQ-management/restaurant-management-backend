# Plan: Sessions end after 7 days unused or 30 days after login

**Source:** backlog/improvements/2026-10-16-session-inactivity-and-absolute-limit.md   **Date:** 2026-10-26   **Status:** In progress
**Risk:** High — session lifetime rules change, a required column is added to `refresh_tokens`, and select-organization gets a new 401 refusal

## Summary
3 tasks in 3 batches. Each refresh token row stores its session's login time, which is copied on every rotation. A token expires at the earlier of "now + 7 days" and "login + 30 days", so the existing expiry check refuses both an idle session and an old one. Select-organization only rotates the caller's own active cookie, so it can never start a fresh 30 days.

## Tasks
| Order | ID | Task | Batch | Priority | Depends on | Outcome |
|---|---|---|---|---|---|---|
| 1 | T1 | Session limit settings and startup checks | B1 | P1 | — | #4, #5 |
| 2 | T2 | Refresh tokens keep the login time; expiry from both limits | B2 | P1 | T1 | #1, #2, #5, #6 |
| 3 | T3 | Choosing an organization keeps the login time | B3 | P1 | T2 | #3, #5 |

Outcomes are the backlog item's Acceptance criteria, in order:

| # | Acceptance criterion |
|---|---|
| #1 | A refresh token unused for 7 days is refused with the usual 401 |
| #2 | A session is refused 30 days after login; refreshing never moves that end |
| #3 | Choosing an organization keeps the original login time |
| #4 | Both limits come from configuration; startup refuses bad values |
| #5 | Identity matrix updated (header, F1, new rows, shipped values like T17) |
| #6 | Cleanup (T9) still keeps revoked tokens long enough for reuse detection |

## Flags
| Task | New integration | New library | Security |
|---|---|---|---|
| T1 | — | — | Auth configuration: the app refuses to start when a session limit is not positive or inactivity exceeds the absolute limit |
| T2 | — | — | Auth change: new session lifetime rules. Migration adds a required column to `refresh_tokens` (no real data before launch, no backfill) |
| T3 | — | — | Auth change: `POST /api/auth/select-organization` returns 401 when the caller has no active refresh cookie of their own. Today it starts a new session instead (matrix rows S6, S8). The other option, an `auth_time` access-token claim, is described in `batches/B3.md` |

## Assumptions
| Item | Assumption |
|---|---|
| A1 | Each row stores `SessionStartedAt` (the login time). Rotation also checks login + 30 days directly, so a lowered limit applies at once |
| A2 | Select-organization without the caller's own active cookie gets the usual 401 instead of a new session (T3 flag) |
| A3 | `Jwt:SessionInactivityDays` (7) and `Jwt:SessionAbsoluteDays` (30) replace `Jwt:RefreshTokenExpireDays`, which is removed with no fallback. Equal values are accepted |
| A4 | An idle or past-limit token is refused like an expired one today (F2): neither the token nor its family is revoked |
| A5 | Cleanup still deletes tokens 30 days after they expire. Reuse detection already stops when a token expires, which is now at most 7 days after it was issued |

## Overall Definition of Done
- [ ] All tasks done, tests passing
- [ ] All Outcomes met (the backlog item's Acceptance criteria)
- [ ] Affected tests green after the last batch; full suite green in CI (Continuous Integration). Shared changes, so `affected` runs the full suite
- [ ] No out-of-scope changes; findings in the backlog
- [ ] Backlog item only: deleted if fully solved (deletion left uncommitted for the user), or updated with what is left

## Checklist
- [x] T1 — Session limit settings and startup checks
- [ ] T2 — Refresh tokens keep the login time; expiry from both limits
- [ ] T3 — Choosing an organization keeps the login time
