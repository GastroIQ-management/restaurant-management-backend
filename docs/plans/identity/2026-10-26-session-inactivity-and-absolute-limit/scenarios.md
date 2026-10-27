# Scenarios: Sessions end after 7 days unused or 30 days after login

**Plan:** docs/plans/identity/2026-10-26-session-inactivity-and-absolute-limit/plan.md
**Coverage:** 22 scenarios — 6 happy · 4 edge · 8 failure · 2 security · 2 guard

Test files: unit tests are in `tests/RestaurantManagement.Modules.Identity.Tests/` and `tests/RestaurantManagement.Shared.Tests/`; endpoint and database tests are in `tests/RestaurantManagement.Web.Tests/`.

| # | Task | Type | Scenario (plain words) | Test name | Test file |
|---|---|---|---|---|---|
| 1 | T1 | Failure | A session limit of 0 or less is refused, and the message names the setting (4 cases) | `Validate_NonPositiveSessionLimit_FailsNamingTheSetting` | `Shared.Tests/JwtOptionsValidatorTests.cs` |
| 2 | T1 | Failure | An inactivity limit longer than the absolute limit is refused | `Validate_InactivityLongerThanAbsoluteLimit_FailsNamingJwtSessionInactivityDays` | `Shared.Tests/JwtOptionsValidatorTests.cs` |
| 3 | T1 | Edge | Equal inactivity and absolute limits are accepted | `Validate_InactivityEqualToAbsoluteLimit_Succeeds` | `Shared.Tests/JwtOptionsValidatorTests.cs` |
| 4 | T1 | Happy | The shipped 7 and 30 days are accepted | `Validate_SevenDaysInactivityAndThirtyDaysAbsolute_Succeeds` | `Shared.Tests/JwtOptionsValidatorTests.cs` |
| 5 | T1 | Failure | The app does not start when a session limit is 0 or less, and the error names the setting (4 cases) | `Startup_NonPositiveSessionLimit_FailsNamingTheSetting` | `Web.Tests/Web/StartupConfigurationTests.cs` |
| 6 | T1 | Failure | The app does not start when inactivity is longer than the absolute limit | `Startup_SessionInactivityAboveAbsoluteLimit_FailsNamingJwtSessionInactivityDays` | `Web.Tests/Web/StartupConfigurationTests.cs` |
| 7 | T1 | Happy | The shipped file sets access token 15 minutes, inactivity 7 days, absolute 30 days | `Configuration_ShippedTokenLifetimes_MatchTheMatrix` | `Web.Tests/Identity/ShippedLimitsTests.cs` |
| 8 | T2 | Happy | A login stores the login time | `IssueAsync_NewSignIn_StoresTheLoginTime` | `Identity.Tests/RefreshTokenServiceTests.cs` |
| 9 | T2 | Happy | A refresh keeps the original login time | `RotateAsync_SessionStartedTenDaysAgo_ReplacementKeepsTheLoginTime` | `Identity.Tests/RefreshTokenServiceTests.cs` |
| 10 | T2 | Edge | A refresh on day 28 expires at login + 30 days, not 7 days from now | `RotateAsync_SessionStarted28DaysAgo_ReplacementExpiresAtLoginPlus30Days` | `Identity.Tests/RefreshTokenServiceTests.cs` |
| 11 | T2 | Edge | Refreshing again near the end never moves the session end | `RotateAsync_RotatedTwiceNearSessionEnd_SessionEndDoesNotMove` | `Identity.Tests/RefreshTokenServiceTests.cs` |
| 12 | T2 | Guard | A token idle for 7 days is refused and nothing is revoked | `RotateAsync_TokenIdleForEightDays_ReturnsNullWithoutRevokingIt` | `Identity.Tests/RefreshTokenServiceTests.cs` |
| 13 | T2 | Failure | A session 30 days past login is refused even if its token has not expired (a lowered limit applies at once), and nothing is revoked | `RotateAsync_SessionStartedOver30DaysAgoWithUnexpiredToken_ReturnsNullWithoutRevokingIt` | `Identity.Tests/RefreshTokenServiceTests.cs` |
| 14 | T2 | Edge | An already rotated token of a session past its limit is refused without marking the family as reused | `RotateAsync_RotatedTokenOfSessionPastItsLimit_ReturnsNullAndDoesNotRevokeTheFamily` | `Identity.Tests/RefreshTokenServiceTests.cs` |
| 15 | T2 | Failure | Refreshing a session 30 days past login gives the usual 401 `InvalidOrExpiredToken` | `RefreshTokenAsync_SessionStartedOver30DaysAgoWithUnexpiredToken_ThrowsInvalidOrExpiredToken401` | `Identity.Tests/SessionServiceRefreshTests.cs` |
| 16 | T2 | Happy | On PostgreSQL the login time is a required column | `Migrate_CleanDatabase_RefreshTokensLoginTimeColumnIsRequired` | `Web.Tests/Identity/RefreshTokensTableTests.cs` |
| 17 | T2 | Guard | Cleanup keeps a rotated token that is still inside its 7-day life | `PurgeExpiredAsync_RotatedTokenWithinItsSevenDayLife_KeepsItForReuseDetection` | `Identity.Tests/RefreshTokenCleanupServiceTests.cs` |
| 18 | T3 | Happy | Choosing an organization on day 28 keeps the login time and the session still ends 30 days after login | `SelectOrganizationAsync_SessionStarted28DaysAgo_NewTokenKeepsTheLoginTimeAndEndsAtLoginPlus30Days` | `Identity.Tests/SessionServiceSelectOrganizationTests.cs` |
| 19 | T3 | Failure | Choosing an organization with no, empty or unknown cookie gives 401 and stores no token (3 cases) | `SelectOrganizationAsync_NoOrUnknownCookie_ThrowsInvalidOrExpiredTokenWith401AndStoresNoToken` | `Identity.Tests/SessionServiceSelectOrganizationTests.cs` |
| 20 | T3 | Failure | Choosing an organization with a cookie idle for 7 days or 30 days past login gives 401 and starts no new session (2 cases) | `SelectOrganizationAsync_IdleOrPastLimitCookie_ThrowsInvalidOrExpiredTokenWith401AndStartsNoSession` | `Identity.Tests/SessionServiceSelectOrganizationTests.cs` |
| 21 | T3 | Security | Another user's cookie gives 401, issues nothing for the caller, and leaves their token active and unchanged | `SelectOrganizationAsync_CookieOfAnotherUser_ThrowsUnauthorizedAndLeavesTheirTokenUntouched` | `Identity.Tests/SessionServiceSelectOrganizationTests.cs` |
| 22 | T3 | Security | Over HTTP, a signed-in caller with no refresh cookie gets 401 and no new cookie | `SelectOrganization_SignedInWithoutRefreshCookie_ReturnsUnauthorizedAndSetsNoCookie` | `Web.Tests/Identity/SelectOrganizationEndpointTests.cs` |

## Spec Outcome coverage
| Outcome | Scenarios |
|---|---|
| #1 A refresh token unused for 7 days is refused with the usual 401 | 12 (refusal), 8 and the updated F1 expiry tests (a token lives 7 days), 15 |
| #2 A session is refused 30 days after login; refreshing never moves that end | 10, 11, 13, 14, 15 |
| #3 Choosing an organization keeps the original login time | 9, 18, 19, 20, 21, 22 |
| #4 Both limits come from configuration; startup refuses bad values | 1, 2, 3, 4, 5, 6, 7 |
| #5 Identity matrix updated | Matrix rows written now (F1, F11–F13, S6, S8, S11, T4, T9, T17, D35, W18, SH51); checked by `python3 .claude/scripts/check-security-matrices.py`. The matrix header text and the ASVS gap report are done by the implementer (T2) and checked at review |
| #6 Cleanup (T9) still keeps revoked tokens long enough for reuse detection | 17 |

## Existing tests updated
Forced by the plan's changes, not new scenarios.
- Every place that set `RefreshTokenExpireDays` now sets `SessionInactivityDays = 7` and `SessionAbsoluteDays = 30` (`IdentityTestDatabase.TestJwtOptions`, `SessionServiceLoginTests`, `AuthUniformResponseTests`, `SessionServiceVerificationTests`, `LockoutHarness`, `RefreshTokensTableTests`).
- Fail until T2 is built (expiry is now 7 days, not 30): `RefreshTokenServiceTests.IssueAsync_NewSignIn_StoresOneActiveTokenWithDeviceIpAndExpiry`, `RefreshTokenServiceTests.RotateAsync_ActiveToken_ReturnsNewTokenActiveInSameFamilyWithInactivityExpiry` (renamed from `...WithRenewedExpiry`), `SessionServiceRefreshTests.RefreshTokenAsync_ActiveToken_NewTokenExpiresInSessionInactivityDays` (renamed from `...InRefreshTokenExpireDays`), `SessionServiceRefreshTests.LoginAsync_ValidCredentials_CreatesOneActiveTokenForThisDevice`.
- `SessionServiceSelectOrganizationTests`: nine tests that chose an organization without a cookie now log in first and pass the login cookie (they still pass). The three tests that started a new session without the caller's own cookie are replaced by scenarios 19–21.
- `ShippedLimitsTests.Configuration_ShippedTokenLifetimes_MatchTheMatrix` is rewritten (scenario 7); it no longer reads `RefreshTokenExpireDays`.

## Read closely before approving
- Scenario 22 and scenarios 19–21 (test-reviewer) — the one new auth refusal: select-organization returns 401 when the caller has no cookie of their own, or has another user's cookie. This choice replaces the `auth_time` claim alternative, so it is the design decision to confirm. Scenario 22 fails on the status code today (the endpoint returns 200 and starts a new session).
- Scenarios 13, 14 and 21 (test-reviewer) — a refusal must revoke nothing, so a stale or stolen cookie cannot sign out another session. The absolute check running before reuse detection carries this.
- Scenarios 10 and 11 — they hold the core rule: expiry is the earlier of now + 7 days and login + 30 days. Scenario 11 now fails today (it asserts the session end is login + 30 days, 2 days away).

## Assumptions
- Services read `DateTime.UtcNow`, so "refreshed daily for 30 days" is proven with seeded dates (login 28, 30 or 31 days ago) rather than by moving a clock.
- Scenario 12 and the cleanup scenario 17 pass today (Guard): the expiry check and cleanup already behave this way; the behaviour that changes is what expiry a new token gets (scenarios 8–11 and the updated F1 tests).
- Idle sessions at the `SessionService` level (401) are already covered by the existing F2 test `RefreshTokenAsync_ExpiredToken_ThrowsInvalidOrExpiredToken401`; only the past-30-days case gets a new 401 test (scenario 15).
- Stubs so tests compile without touching the database: `JwtOptions.SessionInactivityDays` and `SessionAbsoluteDays` (default 0, T1 gives them 7 and 30) and `RefreshToken.SessionStartedAt` marked `[NotMapped]` so existing PostgreSQL tests still pass until T2 adds the migration. T2 must remove `[NotMapped]`.
- The matrix header paragraph ("refresh token 30 days") and `identity-asvs-gap-report.md` are left for T2, as the batch file says.
- Matrix rows changed by this plan (F1, S6, S8, T4, T17, W18) are ❌ until their new tests pass.
