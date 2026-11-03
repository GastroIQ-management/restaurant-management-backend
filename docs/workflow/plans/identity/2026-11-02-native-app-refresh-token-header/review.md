# Review: Native App Refresh Token in a Header

**Verdict:** Fix first — the new `Cache-Control: no-store` on native token responses has no matrix row and no test
**Build:** pass (0 errors, 6 warnings, all in test files this branch does not touch)   **Tests:** full suite runs in CI (Continuous Integration) — merge only when it is green

Second review, after one fix round (commits `d4c1a9f`, `367da3e`, `391f3bf`). The first review's Fix first is resolved: `SentryHostingExtensions.cs:32` now also calls `SetBeforeSendTransaction`, and both callbacks share one header removal. Rows T173 and W33 now cover traces, and the new test `RefreshTokenHeaderLeakTests.SentryTransaction_RequestCarriesRefreshTokenHeader_LeavesWithoutTheTokenAndKeepsOtherHeaders` (scenario 29) names them. It checks that the transaction was sent, so it cannot pass by sending nothing.

Other checks: `python3 .claude/scripts/check-security-matrices.py` passes. No test edited, deleted or weakened since the baseline commit `c9e7687`: the only test change is one added method (32 lines added, none removed). No package, migration, public endpoint or Shared contract added. Every new file is in a folder the plan names. Conventional Commits with scenario numbers. No secrets. No backlog files.

## Fix first
| # | File:line | Problem | Change |
|---|---|---|---|
| 1 | `src/RestaurantManagement.Modules.Identity/Controllers/RefreshTokenTransport.cs:42` | Commit `d4c1a9f` adds new Identity behaviour: native token responses now send `Cache-Control: no-store`. `docs/security/identity-matrix.md` has no row for it and no test checks it. The matrix rules say every new behaviour gets a row and a named test in the same change. Without a test, a later edit can drop the header with nothing failing | Add a row after T174 in `docs/security/identity-matrix.md`, for example T175: "Native login, refresh or choose organization succeeds; Expected: the response carries `Cache-Control: no-store`". Add a new test method in `tests/RestaurantManagement.Web.Tests/Identity/NativeAppSessionEndpointTests.cs`, for example `Login_NativeOrigin_ResponseIsNotCacheable`. It checks `no-store` on a native login and a native refresh. Do not change existing tests. Name the test in the row, run `check-security-matrices.py --fix-tags`, and add scenario 30 to `scenarios.md` |

## Watch out
| # | File:line | Problem | Suggestion |
|---|---|---|---|
| 1 | `docs/workflow/plans/identity/2026-11-02-native-app-refresh-token-header/scenarios.md:51` | The "Spec Outcome coverage" row for #9 still lists only scenarios 26 and 27. Scenario 29 (Sentry traces) also covers Outcome #9 | Change the row to `26, 27, 29`. Add scenario 30 (from Fix first 1) to the rows for #1 and #2 |

## Check before merging
- **Origin decides who gets the token in the body** (`RefreshTokenTransport.cs:19-20`, `NativeAppOptionsValidator.cs`). The match is exact and ordinal, and the startup validator allows only the two native origins. Only this check keeps the token away from browser page scripts. Accepted risk from the spec: any page served from `https://localhost` counts as native. The deployment's CORS (Cross-Origin Resource Sharing) settings must allow both native origins. That is out of scope here, and without it native apps cannot call the API.
- **Affected tests were never green in a single local run.** Every affected run had about 33–36 Web.Tests failures, all Postgres connection errors ("too many clients already"). The failing tests changed between runs, and reruns of those classes passed. `plan.md:28` is ticked on that basis. Merge only on a fully green CI run. If CI shows the same errors, fix the shared test database before merging.
- **The Sentry trace test runs the filter, not the full request pipeline.** It sends a hand-built transaction through a `SentryClient` built from the app's options. It proves the configured `BeforeSendTransaction` removes the header. It does not prove what Sentry's ASP.NET Core integration copies onto real traces. That is enough while the filter covers every transaction.

## Decisions for you
None

## Pull request description
**Native apps keep users signed in with a refresh token header**

Native apps (Capacitor, origins `https://localhost` and `capacitor://localhost`) cannot use the refresh cookie. Requests from those two exact origins now get the refresh token and its expiry in the response body of login, refresh and choose organization. They send it back in an `X-Refresh-Token` header on refresh, choose organization and logout. Native requests never set or read the cookie, and native token responses are marked `Cache-Control: no-store`. Every other origin, or no origin, keeps the cookie flow exactly as before. Web responses do not contain the new fields at all. Rotation, reuse detection, the 7-day idle limit, the 30-day session limit, rate limits and the device-time headers apply the same way to both flows.

**Identity**
- New setting `NativeApp:Origins`, shipped with the two native origins. The app refuses to start if it holds any other value.
- `RefreshTokenTransport` picks cookie or header and body per request. `AuthController` uses it for login, refresh, choose organization, logout, logout-all and change password.
- `AuthResponse` has two optional fields, `refreshToken` and `refreshTokenExpiresAt`, which are left out when empty.
- Matrix rows L39–L40, F15–F20, S14–S17, O7–O11, C12 and T169–T175 are added.

**Web**
- The `X-Refresh-Token` header is removed from Sentry error events and performance traces. Matrix rows W33–W35 are added.

**Spec Outcomes delivered:** 1–9.

**Risk: High** (auth change: a refresh token in a response body for native origins). Flags:
- T1: a setting decides who receives a token in the body.
- T2 and T3: auth change on login, refresh, logout, choose organization, logout-all and change password.
- T4: the token is kept out of Sentry.

**Precondition, not in this PR (pull request):** the deployment's CORS settings must allow both native origins. Accepted risk: any page served from `https://localhost` counts as a native app.

**Tests:** the implementer's local affected runs had intermittent Postgres "too many clients" errors in unrelated Web.Tests. Rely on CI.

🤖 Generated with [Claude Code](https://claude.com/claude-code)
