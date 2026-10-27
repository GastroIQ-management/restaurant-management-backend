# Review: Sessions end after 7 days unused or 30 days after login

**Verdict:** Ready to merge
**Build:** pass (0 errors; 6 warnings, all in test files this branch does not touch)   **Tests:** full suite runs in CI (Continuous Integration) — merge only when it is green

Checked:
- Completeness: T1–T3 are ticked, each Delivered note is written, and Outcomes #1–#6 are met. One Overall DoD (Definition of Done: the checks that must pass to count as finished) item stays unticked until CI runs, with a note.
- Tests: none changed since the baseline `adcc664`.
- Scope: everything matches the plan's tasks. The first commit `dbb67c0` is a workflow change the user asked for on this branch.
- Structure: new files are only the generated migration and the plan folder.
- Security matrices: `check-security-matrices.py` passes. F1, F11–F13, S6, S8, S11, T4, T9, T17, D35, W18 and SH51 match the code.
- Secrets: none. No packages were added.
- Backlog: the source item's deletion is left uncommitted for the user, as the rules require.

## Watch out
| # | File:line | Problem | Suggestion |
|---|---|---|---|
| 1 | `src/RestaurantManagement.Modules.Identity/Services/Sessions/SessionService.cs:152` | `ClientInfo client` is no longer used now that the `IssueAsync` fallback is gone. `ISessionService.cs:9` and `AuthController.cs:90` still build and pass it | Removing it changes test calls, and tests are read-only after Gate 2. Add a backlog item to drop the parameter from the interface, controller and tests |
| 2 | `src/RestaurantManagement.Modules.Identity/Services/Sessions/SessionService.cs:169` | `user.UpdatedAt` is saved before the cookie check, so a refused switch (401 at line 177) still writes to the user row | Move the `UpdatedAt` save after `RotateAsync` succeeds, in a later change. The Implementation guide allowed it to stay |
| 3 | `src/RestaurantManagement.Modules.Identity/Services/Sessions/RefreshTokenService.cs:141` | `BuildRow` now takes 6 parameters. The clean-code limit is 4, and it already had 5 | Group them into a small record (user, organization, family, device, IP address, login time) when this class is next touched |
| 4 | `src/RestaurantManagement.Modules.Identity/Entities/Sessions/RefreshToken.cs:25` | A stray blank line was left above `SessionStartedAt` when the stub was removed | Delete the blank line next time the file changes |
| 5 | `docs/plans/identity/2026-10-26-session-inactivity-and-absolute-limit/plan.md:52` | Checklist ticks carry no commit hashes. They are T1 `3712e0f`, T2 `cf2a41f`, T3 `aafe0c2` | Add the hashes if you keep that convention. Recent plans do |

## Check before merging
- **`POST /api/auth/select-organization` now returns 401 without the caller's own active refresh cookie** (`SessionService.cs:173-177`). Before, it started a new session. Any client that calls it without sending the cookie now fails, for example a cross-origin call without credentials. A user whose session hit 7 days unused or 30 days after login must sign in again before switching. Confirm the frontend sends the cookie on this call.
- **The 30-day check in `RotateAsync` runs before the other-user and reuse checks** (`RefreshTokenService.cs:35-36`). A rotated token replayed after the session ends gets a plain 401 and no longer revokes its family (F12). This is the intended behaviour, because the session is already dead. It is still a deliberate change to reuse detection.
- **The migration adds a required `session_started_at` column with default `0001-01-01`.** Any refresh token already in a developer database counts as past its limit, so those users must sign in again. There is no real data before launch, so nothing else is affected.

## Pull request description

**Sessions end after 7 days unused or 30 days after login**

A refresh token now expires 7 days after it was last used. A session now ends 30 days after
login, however often it is refreshed. Switching organization can no longer restart the 30 days.
Before this change, each refresh set a new 30-day expiry, so a session that kept being used
never ended. That covers ASVS (Application Security Verification Standard) V7.3.1, V7.3.2 and V7.1.1.

**Changes per module**
- Shared: `JwtOptions` replaces `RefreshTokenExpireDays` with `SessionInactivityDays` (7) and
  `SessionAbsoluteDays` (30). The old setting is removed with no fallback. The app refuses to
  start when either value is 0 or less, or when inactivity is longer than the absolute limit.
- Identity: each refresh token stores its session's login time (`SessionStartedAt`), and every
  rotation copies it. A new token expires at the earlier of now + 7 days and login + 30 days.
  Rotation refuses a session 30 days past login, and revokes nothing. Select-organization only
  rotates the caller's own active cookie. Without one it returns 401 `InvalidOrExpiredToken`
  and issues no token.
- Web: `appsettings.json` ships the two new values.
- Migration `AddRefreshTokenSessionStartedAt`: adds a required column to `refresh_tokens`,
  with no backfill.
- Docs: identity, web and shared security matrices (F1, F11–F13, S6, S8, S11, T4, T9, T17, D35,
  W18, SH51) and the Identity ASVS gap report.
- Workflow (separate commit `dbb67c0`): agent instructions now end each step with decisions
  only, and the planner can plan a backlog item that has no spec.

**Outcomes delivered** (the backlog item's Acceptance criteria)
1. A refresh token unused for 7 days is refused with the usual 401
2. A session is refused 30 days after login; refreshing never moves that end
3. Choosing an organization keeps the original login time
4. Both limits come from configuration; startup refuses bad values
5. Identity matrix updated
6. Cleanup still keeps revoked tokens long enough for reuse detection

**Risk:** High. Session lifetime rules change, a required column is added to `refresh_tokens`,
and `POST /api/auth/select-organization` has a new 401 refusal. Clients must send the refresh
cookie on that call.

🤖 Generated with [Claude Code](https://claude.com/claude-code)
