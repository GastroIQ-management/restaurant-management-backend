# Review: Backlog fixes 2026-11-02 (native app CORS origins, X-Refresh-Token in OpenAPI)

**Verdict:** Ready to merge
**Build:** pass (11 projects, 0 errors, 0 warnings)   **Tests:** full suite runs in CI (Continuous Integration) — merge only when it is green

Small batch with no plan folder, so there is no Completeness check. It was reviewed against the architecture, security and clean-code skills.

Checks done:
- **Commits:** two commits, one item each. `c31fd82` changes only CORS (Cross-Origin Resource Sharing: the browser rules for calls from another origin) settings, its test and row W36. `5199a2c` changes only the OpenAPI filter, its registration, its test and rows WD18–WD19. Both follow Conventional Commits. Neither is amended or combined.
- **Tests:** no existing test edited, deleted, skipped or weakened. The branch only adds two test files.
- **Scope and structure:** no migration, package, public endpoint or Shared contract. `RefreshTokenHeaderOperationFilter.cs` sits next to the other operation filters in `Web/OpenApi/` (7 files, under the limit of 10). The tests are in `Web.Tests/Web/`. No new folders.
- **Security matrices:** `python3 .claude/scripts/check-security-matrices.py` passes. W36, WD18 and WD19 each name tests that exist and check that row's Expected result. The W36 test checks the exact origin, credentials and the `x-refresh-token` header, and checks that `http://localhost` is refused.
- **Security:** the CORS origins match `NativeAppOptionsValidator`'s two exact values. The added `https://localhost` with credentials is the accepted risk recorded in spec `docs/workflow/specs/identity/2026-11-02-native-app-refresh-token-header.md` (lines 38 and 73). The web refresh cookie is `SameSite=Strict`, so a page on `https://localhost` never receives it. No secrets in the diff.
- **Backlog:** no backlog file added or changed.
- **Comments:** only one "why" comment, at `RefreshTokenHeaderOperationFilter.cs:9`.

## Watch out
| # | File:line | Problem | Suggestion |
|---|---|---|---|
| 1 | `src/RestaurantManagement.Web/appsettings.json:28` | Indexes 0 and 1 of `Cors:AllowedOrigins` now hold the native origins. A deployment that sets `Cors__AllowedOrigins__0` for the web frontend would silently replace `https://localhost`, and Android sign-in refresh would stop working. Today the only note about this is in the commit body. | Add a note to the `Cors` section, like the other sections have: `"//": "0 and 1 are the native app origins (keep in step with NativeApp:Origins). Add web origins from index 2 via Cors__AllowedOrigins__<n>; the same index replaces one"`. |

## Check before merging
- **`https://localhost` is allowed with credentials in every environment** (`appsettings.json:28`). Any page served from `https://localhost` on a user's device can call the API from a browser and counts as native, so it gets the token in the body. The spec accepts this risk. The refresh cookie stays safe because of `SameSite=Strict`.
- **Configuration indexes decide which origins survive.** An environment variable at index 0 or 1 replaces a native origin instead of adding one. `launchSettings.json` now uses index 2 for the dev frontend. Every deployment must follow the same rule (Watch out 1).
- **Local affected runs were never fully green.** Web.Tests had 26 to 32 failures that changed between runs, all Postgres "transient failure" errors in database endpoint tests. The new classes passed (Cors 9/9, OpenApi 18/18). Merge only on a fully green CI run.

## Decisions for you
None

## Pull request description
**Native apps can call the API, and generated clients know about the `X-Refresh-Token` header**

Two small Web changes that finish the native app refresh token work from PR #63.

**Web**
- **CORS:** `Cors:AllowedOrigins` now ships with the two native app origins, `https://localhost` (Android) and `capacitor://localhost` (iOS). Before this, the list was empty and native apps could not call the API. Credentials and the `X-Refresh-Token` header are allowed. Other localhost origins are still refused. The dev launch profile now sets the frontend origin at index 2, so it no longer replaces a native origin. Deployments add web origins at a new index (`Cors__AllowedOrigins__2` and up). Matrix row W36.
- **OpenAPI:** `RefreshTokenHeaderOperationFilter` declares the optional `X-Refresh-Token` header (native apps only) on `POST api/v1/auth/refresh`, `select-organization` and `logout`. No other operation shows it. Matrix rows WD18 and WD19.

**Accepted risk (from the native app spec):** `https://localhost` is allowed with credentials, so any page served from that origin counts as a native app. The web refresh cookie is `SameSite=Strict` and is never sent to it.

**Tests:** new `NativeAppCorsTests` and `RefreshTokenHeaderOpenApiTests` pass. Local runs had intermittent Postgres errors in unrelated Web.Tests, so rely on CI.

🤖 Generated with [Claude Code](https://claude.com/claude-code)
