# Scenarios: Versioned endpoints and app version on writes

**Plan:** docs/plans/shared/2026-10-26-versioned-endpoints-and-app-version/plan.md
**Coverage:** 26 scenarios — 8 happy · 4 edge · 9 failure · 2 security · 3 guard

Paths: `tests/RestaurantManagement.Shared.Tests/Middleware/` = SM; `tests/RestaurantManagement.Web.Tests/` = WT.

| # | Task | Type | Scenario (plain words) | Test name | Test file |
|---|---|---|---|---|---|
| 1 | T1 | Failure | A POST, PUT, PATCH or DELETE with no app version is refused with 400 `app_version.required` and never reaches the endpoint | `InvokeAsync_WriteWithoutAppVersion_ThrowsRequiredAndStops` | `SM/AppVersionMiddlewareTests.cs` |
| 2 | T1 | Failure | A write whose app version is not a semantic version (`1.2`, `v1.2.3`, `latest`, four parts, trailing space) is refused with 400 `app_version.invalid` | `InvokeAsync_WriteWithAnAppVersionThatIsNotSemantic_ThrowsInvalidAndStops` | `SM/AppVersionMiddlewareTests.cs` |
| 3 | T1 | Failure | A write whose app version is 33 characters is refused with 400 `app_version.invalid` | `InvokeAsync_WriteWithA33CharacterAppVersion_ThrowsInvalidAndStops` | `SM/AppVersionMiddlewareTests.cs` |
| 4 | T1 | Edge | A write whose app version is exactly 32 characters (the limit) reaches the endpoint | `InvokeAsync_WriteWithA32CharacterAppVersion_ReachesTheEndpoint` | `SM/AppVersionMiddlewareTests.cs` |
| 5 | T1 | Happy | A write with a valid version (plain, with `-` suffix, with `+` suffix) reaches the endpoint | `InvokeAsync_WriteWithASemanticAppVersion_ReachesTheEndpoint` | `SM/AppVersionMiddlewareTests.cs` |
| 6 | T1 | Edge | GET, HEAD and the browser preflight (OPTIONS) need no app version | `InvokeAsync_ReadWithoutAppVersion_ReachesTheEndpoint` | `SM/AppVersionMiddlewareTests.cs` |
| 7 | T1 | Failure | Through the real app, a write with no app version answers 400 `app_version.required`, on a public (login) and a signed-in endpoint | `Write_WithoutAppVersion_Answers400RequiredOnPublicAndSignedInEndpoints` | `WT/Shared/AppVersionEndpointTests.cs` |
| 8 | T1 | Failure | Through the real app, a malformed or 33+ character app version answers 400 `app_version.invalid`, on a public and a signed-in endpoint | `Write_WithMalformedOrTooLongAppVersion_Answers400InvalidOnPublicAndSignedInEndpoints` | `WT/Shared/AppVersionEndpointTests.cs` |
| 9 | T1 | Security | An anonymous and a signed-in caller get exactly the same refusal (the check runs before sign-in) | `Write_WithoutAppVersion_AnonymousAndSignedInCallersGetTheSameAnswer` | `WT/Shared/AppVersionEndpointTests.cs` |
| 10 | T1 | Security | The refusal sent to an allowed browser origin carries the cross-origin headers, so the page can read it | `Write_WithoutAppVersionFromAllowedOrigin_RefusalCanBeReadByTheBrowser` | `WT/Shared/AppVersionEndpointTests.cs` |
| 11 | T1 | Happy | The refusal title is translated (Portuguese, French) while the code stays the same | `Write_WithoutAppVersion_TitleFollowsTheLanguageAndCodeStaysTheSame` | `WT/Shared/AppVersionEndpointTests.cs` |
| 12 | T1 | Guard | A read without an app version is not refused for it (an anonymous read still answers 401) | `Read_WithoutAppVersion_AnswersAsBefore` | `WT/Shared/AppVersionEndpointTests.cs` |
| 13 | T1 | Happy | A write with a valid app version reaches the endpoint (the test client sends one by default). Uses `/api/v1/auth/logout-all`, so it turns green only after T5 | `Write_WithAValidAppVersion_ReachesTheEndpoint` | `WT/Shared/AppVersionEndpointTests.cs` |
| 14 | T2 | Failure | The audit log at the old path `/api/audit-log` answers 404 to a signed-in caller | `UnversionedPath_AuditLog_Answers404` | `WT/Shared/AuditLogVersionedRouteTests.cs` |
| 15 | T3 | Failure | Organization and location endpoints at the old paths answer 404 | `UnversionedPath_OrganizationEndpoints_Answer404` | `WT/Organization/OrganizationVersionedRouteTests.cs` |
| 16 | T4 | Failure | Menu and category endpoints at the old paths answer 404 | `UnversionedPath_MenuEndpoints_Answer404` | `WT/Menu/MenuVersionedRouteTests.cs` |
| 17 | T5 | Failure | Identity endpoints (auth, roles, invitations, users, members) at the old paths answer 404 | `UnversionedPath_IdentityEndpoints_Answer404` | `WT/Identity/IdentityVersionedRouteTests.cs` |
| 18 | T6 | Happy | Every endpoint route of the app starts with a supported version prefix (`api/v1/`) | `EveryEndpoint_RouteTemplate_StartsWithASupportedApiVersion` | `WT/Web/ApiVersionRoutesTests.cs` |
| 19 | T6 | Edge | A version includes only routes under its own prefix: `v1` takes `api/v1/...`, never `api/v10/...`, `api/v2/...` or `api/...` | `IncludesRoute_RouteAndVersion_IncludesOnlyRoutesWithThatVersionPrefix` | `WT/Web/ApiVersionsTests.cs` |
| 20 | T6 | Happy | The v1 document lists only `/api/v1/` paths and every v1 operation | `V1Document_InDevelopment_ListsOnlyV1PathsAndEveryV1Operation` | `WT/Web/ApiVersionDocumentsTests.cs` |
| 21 | T6 | Guard | A version that is not supported (`v2`) has no document (404) | `Document_VersionThatIsNotSupported_IsNotServed` | `WT/Web/ApiVersionDocumentsTests.cs` |
| 22 | T6 | Happy | The sign-in help in the document names `/api/v1/auth/login` | `V1Document_InDevelopment_BearerDescriptionPointsToTheVersionedLogin` | `WT/Web/ApiVersionDocumentsTests.cs` |
| 23 | T6 | Happy | The v1 document states the two rules: additive changes only, old version kept 30 days | `V1Document_InDevelopment_DescriptionStatesTheAdditiveOnlyAndThirtyDayRules` | `WT/Web/ApiVersionDocumentsTests.cs` |
| 24 | T7 | Happy | Every POST, PUT, PATCH and DELETE in the v1 document declares a required `X-App-Version` header | `V1Document_EveryWriteOperation_DeclaresTheRequiredAppVersionHeader` | `WT/Web/AppVersionHeaderOpenApiTests.cs` |
| 25 | T7 | Guard | No GET operation declares the header | `V1Document_ReadOperations_DoNotDeclareTheAppVersionHeader` | `WT/Web/AppVersionHeaderOpenApiTests.cs` |
| 26 | T7 | Edge | The header is declared with a 32-character limit, a format pattern and a description | `V1Document_AppVersionHeader_DeclaresItsFormatLengthAndPurpose` | `WT/Web/AppVersionHeaderOpenApiTests.cs` |

## Spec Outcome coverage
| Outcome | Scenarios |
|---|---|
| #1 Endpoints are versioned; changes inside a version are additive only | 14, 15, 16, 17, 18, 19, 20 (endpoints); additive rule: 23 and the architecture skill (checked at review) |
| #2 Every write carries the app version | 1–13, 24, 25, 26 |
| #3 An old version is kept at least 30 days | 23 (document); the architecture skill "API versions" section and `README.md` are checked at review (docs, no test) |
| #4 The OpenAPI document is published per version | 18–26 |

## Existing tests updated
Forced by the plan; no new scenario.
- Every Web.Tests URL under `Identity/`, `Menu/`, `Organization/`, `Shared/` and `Web/` now starts `/api/v1/` (about 60 files; only the test-only `/api/test-probe/...` paths and `/api/does-not-exist` stay). They fail until the module's task moves its routes: Shared audit log T2, Organization T3, Menu T4, Identity T5; a test that calls another module's URL turns green when that module's task is done. About 327 existing Web.Tests tests fail now for this reason.
- `RefreshTokenCookieEndpointTests`, `LogoutEndpointTests`, `PasswordChangeEndpointTests`: cookie path `/api/v1/auth` (matrix L2).
- `AuthRoutePathTests` (matrix T10) and `AuthEndpointSecretsNotInUrlTests`: auth routes start `api/v1/auth`.
- `ErrorCodesOpenApiTests`: reads `/api/v1/menus` in the v1 document (green after T4).
- `AuthApiFactory` (and so `TenantApiFactory`, `PermissionProbeApiFactory`): every client sends `X-App-Version: 1.0.0` by default through `ConfigureClient`. Refusal tests remove it.
- Matrix rows L2 and T10 (Identity) reworded to `/api/v1/auth` and set back to ❌ until T5.

## Read closely before approving
- #9, #10 (SH67, SH68): they carry the security risk. The check must run before sign-in and after CORS. Pipeline order is the only thing that makes both pass.
- #3 and #4 (SH64): the 32 and 33 character boundary. The 33-character case is the one that stops a regex-only implementation.
- #13 and #18 with the AuthApiFactory default header: #13 is green only after T5, and #18 is the route guard for all of T2–T5. The factory now sends `X-App-Version: 1.0.0` on every test client, so the ~327 rewritten tests don't check the header. Only the refusal tests remove it.

## Assumptions
- The Sentry `app_version` tag is not tested: it needs a Sentry hub that no test fixture offers, and the plan says the value is never stored. Checked at review.
- Where the 400 is produced (throwing `BusinessException`, as `LocationAccessMiddleware` does, or writing `ProblemResponse` directly) is left to the implementer; the unit tests expect `BusinessException` with the code and status, the same as `LocationAccessMiddlewareTests`. If the implementer writes the response directly, scenarios 1–3 need a rewrite at the review.
- Code names fixed here: `app_version.required` and `app_version.invalid` (status 400).
- Empty header value (`X-App-Version:`) is not tested: it reads as missing or invalid, both 400.
- The test-only `/api/test-probe` routes stay unversioned: they exist only in `PermissionProbeApiFactory`, which the route guard (scenario 18) does not use.
- Scenarios 21 and 25 (and 12) pass today (Guard): they protect behaviour that must survive the change.
- Scenarios 14–17 send each request to the old path signed in (a valid access pass for a random user). The fallback policy (`RequireAuthenticatedUser`) answers 401 to an anonymous caller before a missing route can answer 404, so the 404 holds only for a signed-in caller. This includes the anonymous-by-design Identity routes (`/api/auth/login`): they are signed in too, because at the old path no endpoint exists to allow anonymous access.
