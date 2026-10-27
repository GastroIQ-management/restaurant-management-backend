# Review: Versioned endpoints and app version on writes

**Verdict:** Ready to merge
**Build:** pass (0 errors, 6 existing test warnings)   **Tests:** full suite runs in CI (Continuous Integration) — merge only when it is green

Second and final review, after the fix round (commits `92f111e`, `1eca794`, `8d52694`).

Checks run: `dotnet build RestaurantManagement.sln` passes. `python3 .claude/scripts/check-security-matrices.py` passes. No package was added, so `dotnet list package --vulnerable` was not needed. No secrets in the diff. No new folders. Every new file is in an allowed folder and listed in a task's "Files" row. Since the baseline `ed2f439`, the only test change is `62ca641` (sign-in on the four `*VersionedRouteTests`), which the user approved. The fix round changed no test.

Fix round results (all three earlier Fix first items are solved):
- **App version rule:** `AppVersionMiddleware` now has public `MaxLength` and `PatternBody` (ASCII digits `[0-9]`, non-empty suffix). The server regex and the OpenAPI `Pattern` and `MaxLength` are both built from them, so the server and the document state one rule.
- **Stale ❌ rows:** M48, OR48 and SH66 are ✅. The B1, B2 and B3 Delivered notes record this.
- **Unversioned paths in the Identity matrix:** all 15 rows now name `api/v1/...`. No row in `docs/security/` names an unversioned path. Older files in `docs/reviews/` still do, but they are history and stay as written.

## Watch out
| # | File:line | Problem | Suggestion |
|---|---|---|---|
| 1 | `docs/plans/shared/2026-10-26-versioned-endpoints-and-app-version/plan.md:39-45` (Checklist) | The ticked tasks still do not show their commit hashes. The fix round did not change this | Add the hashes: T1 `a28f4df`, T2 `a38e748`, T3 `0c850b0`, T4 `5e00be7`, T5 `0ec224e`, T6 `ae23052`, T7 `c5bbfae`. If not, drop this item |

## Check before merging
- **Every write now needs `X-App-Version`.** This includes the public `register`, `login`, `refresh` and `logout`. A frontend build that does not send the header cannot sign in. Ship the frontend change together with this branch.
- **Pipeline order is the security control.** `Program.cs:345` places `AppVersionMiddleware` after `UseCors` and before `UseRateLimiter` and `UseAuthentication`. Rows SH67 (same answer signed in or not) and SH68 (browser can read the refusal) depend on this order.
- **Refresh cookie path is now `/api/v1/auth`** (rows L2 and T10). An auth v2 must decide its own cookie path when it ships.

## Decisions for you
None

## Pull request description
**Versioned endpoints and app version on writes**

Every endpoint now answers under `/api/v1/`. Every write must send the app version. This lets a device replay writes it queued offline (up to 7 days) after a backend deploy: an old version stays served while the new one ships.

What changed per module:
- **Shared:** new `AppVersionMiddleware`. A POST, PUT, PATCH or DELETE without `X-App-Version` gets 400 `app_version.required`. A value that is not `major.minor.patch` (optional `-`/`+` suffix, ASCII digits only), or is over 32 characters, gets 400 `app_version.invalid`. Titles are in English, Portuguese and French. Reads and CORS (Cross-Origin Resource Sharing) preflight are not checked. A valid value is tagged on Sentry events; it is never stored or used for access. The audit log moved to `/api/v1/audit-log`.
- **Organization:** `/api/v1/organizations`, `/api/v1/locations`.
- **Menu:** `/api/v1/menus` and the category routes under `/api/v1`.
- **Identity:** `/api/v1/auth`, `/api/v1/users/me`, `/api/v1/members`, `/api/v1/roles`, `/api/v1/invitations`. The refresh cookie path is now `/api/v1/auth`; its other flags are unchanged.
- **Web:** one OpenAPI document per supported version (`/swagger/v1/swagger.json`, Development only). Every write in the document declares the required `X-App-Version` header, with the same length limit and pattern the server enforces. A route guard test fails for any endpoint outside a supported version.
- **Docs:** architecture skill section "API versions", README examples, and the security matrices (all paths now `api/v1`).

Outcomes delivered (backlog item `backlog/offline/2026-10-26-versioned-endpoints-and-app-version.md`):
1. Endpoints are versioned. Changes inside a version are additive only (written rule).
2. Every write carries the app version.
3. An old version is kept at least 30 days after the next one ships (written rule).
4. The OpenAPI document is published per version.

Risk: High. Every URL changes, every write's contract changes, and the refresh cookie path (auth) changes. There is no data or schema change, and nothing is live yet. Flags: T1 adds a new client-supplied header that is bounded and never used for access. T5 changes the refresh cookie path. T6 keeps Swagger in Development only (W21).

The frontend must call `/api/v1/...` and send `X-App-Version` on every write.

The backlog item file is deleted in the working tree and left uncommitted for you to check.

🤖 Generated with [Claude Code](https://claude.com/claude-code)
