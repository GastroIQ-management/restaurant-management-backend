# Scenarios: Problem Details with stable error codes

**Plan:** docs/plans/shared/2026-10-26-problem-details-with-stable-error-codes/plan.md
**Coverage:** 40 scenarios — 6 happy · 7 edge · 13 failure · 14 security · 0 guard

Source: the backlog item `backlog/offline/2026-10-26-problem-details-with-stable-error-codes.md` (no spec file exists); its Acceptance criteria are the Outcomes #1 to #5 of the plan.

| # | Task | Type | Scenario (plain words) | Test name | Test file |
|---|---|---|---|---|---|
| 1 | T1 | Happy | A business refusal with a code answers Problem Details: its status, localized title, fixed code and a trace ID, and no old `message` property | `InvokeAsync_BusinessExceptionWithCode_AnswersProblemDetailsWithStatusTitleCodeAndTraceId` | `tests/RestaurantManagement.Shared.Tests/ExceptionHandlingMiddlewareTests.cs` |
| 2 | T1 | Edge | An unexpected failure in production answers 500 `server.unexpected_error` and shows no exception text | `InvokeAsync_UnexpectedExceptionInProduction_AnswersServerUnexpectedErrorWithoutDetail` | `tests/RestaurantManagement.Shared.Tests/ExceptionHandlingMiddlewareTests.cs` |
| 3 | T1 | Edge | The error writer does nothing and does not fail when the response has already started | `WriteAsync_ResponseAlreadyStarted_WritesNothingAndDoesNotThrow` | `tests/RestaurantManagement.Shared.Tests/ProblemResponseTests.cs` |
| 4 | T1 | Happy | The Shared catalog holds every named cross-module code with its one status | `Read_SharedCatalog_ListsEveryNamedCodeWithItsStatus` | `tests/RestaurantManagement.Shared.Tests/ErrorCodeCatalogTests.cs` |
| 5 | T1 | Edge | Each status the host can answer (400, 401, 403, 404, 405, 409, 415, 429, 500) has a fallback code that is never a retry code | `Fallback_StatusTheHostCanAnswer_ReturnsItsFallbackCodeWithThatStatus` | `tests/RestaurantManagement.Shared.Tests/ErrorCodeCatalogTests.cs` |
| 6 | T1 | Happy | A real endpoint failing unexpectedly answers Problem Details with `server.unexpected_error` and no detail | `Login_UnexpectedFailure_AnswersProblemDetailsWithServerUnexpectedErrorCode` | `tests/RestaurantManagement.Web.Tests/Shared/UnexpectedErrorEndpointTests.cs` |
| 7 | T1 | Edge | Every code of every module is `subject.reason` in lowercase snake_case and never reused | `AllCatalogs_EveryCode_HasTheSubjectDotReasonFormatAndIsUnique` | `tests/RestaurantManagement.Web.Tests/Shared/ErrorCodeCatalogsAcrossModulesTests.cs` |
| 8 | T1 | Edge | Exactly three codes are retry codes, and `request.in_progress` is the only 409 one | `AllCatalogs_RetryFlag_IsSetOnExactlyTheThreeRetryCodes` | `tests/RestaurantManagement.Web.Tests/Shared/ErrorCodeCatalogsAcrossModulesTests.cs` |
| 9 | T2 | Security | A member with no stored access is refused by every guard check with `access.denied` | `Require_MemberWithNoStoredCells_ThrowsAccessDeniedCode` | `tests/RestaurantManagement.Shared.Tests/Auth/AccessGuardTests.cs` |
| 10 | T2 | Security | Another organization's location and a location without access answer the same status, title and code | `GetLocations_AnotherOrganizationsLocationAndALocationWithoutAccess_AnswerTheSameStatusTitleAndCode` | `tests/RestaurantManagement.Web.Tests/Shared/LocationAccessEndpointTests.cs` |
| 11 | T2 | Failure | A reversed audit date range and an unknown audit action each answer their own code | `GetAuditLog_InvalidFilter_AnswersItsOwnCode` | `tests/RestaurantManagement.Web.Tests/Shared/AuditLogEndpointTests.cs` |
| 12 | T2 | Failure | Saving a row of another organization is refused with `tenant.mismatch` | `SaveChangesAsync_NewTenantRowCarriesAnotherOrganizationId_ThrowsTenantMismatchCode` | `tests/RestaurantManagement.Shared.Tests/AuditSaveChangesInterceptorTests.cs` |
| 13 | T3 | Security | A forged and an expired token get the same status, title and `auth.invalid_token`, never saying why | `ProtectedEndpoint_ForgedAndExpiredToken_AnswerTheSameStatusTitleAndInvalidTokenCode` | `tests/RestaurantManagement.Web.Tests/Web/InvalidTokenMessageTests.cs` |
| 14 | T3 | Failure | No token answers `auth.unauthorized` with one code and a title in English, Portuguese and French | `UndeclaredEndpoint_NoAccessTokenInEachLanguage_AnswersOneCodeWithATitleInThatLanguage` | `tests/RestaurantManagement.Web.Tests/Web/PrivateByDefaultTests.cs` |
| 15 | T3 | Security | A waiter without Members access gets 403 `access.denied` | `MembersManagePolicy_WaiterWithoutMembersAccess_AnswersAccessDeniedCode` | `tests/RestaurantManagement.Web.Tests/Web/AccessDeniedMessageTests.cs` |
| 16 | T3 | Security | Manage in another organization only gives the same 403 body and code as a waiter | `MembersManagePolicy_ManageInAnotherOrganizationOnly_AnswersTheSameBodyAndCodeAsAWaiter` | `tests/RestaurantManagement.Web.Tests/Web/AccessDeniedMessageTests.cs` |
| 17 | T3 | Failure | A rate-limited request answers `rate_limit.exceeded` as Problem Details and keeps `Retry-After` | `Register_RateLimited_AnswersProblemDetailsWithRateLimitCodeAndRetryAfter` | `tests/RestaurantManagement.Web.Tests/Web/CorsRetryAfterTests.cs` |
| 18 | T3 | Failure | An invalid request answers `validation.failed` and keeps its field errors | `Register_InvalidRequest_AnswersValidationFailedCodeAndKeepsFieldErrors` | `tests/RestaurantManagement.Web.Tests/Web/ValidationTitleEndpointTests.cs` |
| 19 | T3 | Edge | A bodiless error result, an unknown route, a wrong method and a wrong content type answer the fallback code of their status and a title per language | `FrameworkError_UnknownRouteWrongMethodBodilessResultWrongContentType_AnswersFallbackCodeWithATitlePerLanguage` | `tests/RestaurantManagement.Web.Tests/Web/FallbackProblemDetailsTests.cs` |
| 20 | T4 | Failure | A missing menu or location in the Menu services answers `menu.not_found` or `location.not_found` | `MenuAndCategoryServices_MenuOrLocationDoesNotExist_ThrowNotFoundWithTheirOwnCode` | `tests/RestaurantManagement.Modules.Menu.Tests/MenuGridAccessTests.cs` |
| 21 | T4 | Security | Another organization's menu and category answer the same 404 status, title and code as a missing one | `MenuOrCategory_OfAnotherOrganization_AnswersTheSameStatusTitleAndCodeAsAMissingOne` | `tests/RestaurantManagement.Web.Tests/Menu/MenuTenantEndpointTests.cs` |
| 22 | T4 | Security | A member with Menus Read only who edits a menu gets 403 `access.denied` | `EditMenu_MemberWithMenusReadOnly_AnswersAccessDeniedCode` | `tests/RestaurantManagement.Web.Tests/Menu/MenuGridEndpointTests.cs` |
| 23 | T5 | Failure | A location code already used answers `location.code_already_used` | `CreateLocation_CodeAlreadyUsedInOrganization_AnswersLocationCodeAlreadyUsedCode` | `tests/RestaurantManagement.Web.Tests/Organization/LocationCodeEndpointTests.cs` |
| 24 | T5 | Security | Another organization's location answers the same 404 status, title and code as a missing one | `EditLocation_LocationOfAnotherOrganization_AnswersTheSameStatusTitleAndCodeAsAMissingOne` | `tests/RestaurantManagement.Web.Tests/Organization/OrganizationTenantEndpointTests.cs` |
| 25 | T5 | Security | A member without Locations Manage who edits a location gets 403 `access.denied` | `EditLocation_MemberWithoutLocationsManage_AnswersAccessDeniedCode` | `tests/RestaurantManagement.Web.Tests/Organization/OrganizationGridEndpointTests.cs` |
| 26 | T6 | Security | Unknown email, wrong password, unverified and deactivated accounts all give the same 401 `auth.invalid_credentials` | `LoginAsync_UnknownEmailWrongPasswordUnverifiedOrDeactivatedAccount_ThrowTheSameInvalidCredentialsCode` | `tests/RestaurantManagement.Modules.Identity.Tests/SessionServiceLoginTests.cs` |
| 27 | T6 | Security | An unknown email and a wrong verification code both give `auth.invalid_code` | `VerifyEmailAsync_UnknownEmailAndWrongCode_BothThrowTheInvalidCodeCode` | `tests/RestaurantManagement.Modules.Identity.Tests/VerificationServiceTests.cs` |
| 28 | T6 | Failure | Registering an email already in use answers `registration.email_already_registered` | `RegisterAsync_EmailAlreadyRegistered_ThrowsEmailAlreadyRegisteredCode` | `tests/RestaurantManagement.Modules.Identity.Tests/RegistrationServiceTests.cs` |
| 29 | T6 | Security | Choosing an organization: an empty ID gets `session.organization_required`; another user's or an unknown organization gets the same `session.not_a_member` | `SelectOrganizationAsync_NoOrganizationNotAMemberOrUnknownOrganization_ThrowsTheirOwnCode` | `tests/RestaurantManagement.Modules.Identity.Tests/SessionServiceSelectOrganizationTests.cs` |
| 30 | T6 | Failure | A wrong current password and a new password equal to the current one each answer their own code | `ChangePasswordAsync_WrongCurrentPasswordOrSameAsCurrent_ThrowsTheirOwnCode` | `tests/RestaurantManagement.Modules.Identity.Tests/PasswordChangeServiceTests.cs` |
| 31 | T6 | Failure | Refreshing with an unknown token answers `session.invalid_or_expired` | `RefreshTokenAsync_UnknownToken_ThrowsSessionInvalidOrExpiredCode` | `tests/RestaurantManagement.Modules.Identity.Tests/SessionServiceRefreshTests.cs` |
| 32 | T7 | Security | Another organization's member, location and role answer the same 404 status, title and code as missing ones | `SetAccess_MemberLocationOrRoleOfAnotherOrganization_AnswersTheSameStatusTitleAndCodeAsAMissingOne` | `tests/RestaurantManagement.Web.Tests/Identity/MemberAccessEndpointTests.cs` |
| 33 | T7 | Security | A waiter without Members Manage who sets access gets 403 `access.denied` | `SetAccess_WaiterWithoutMembersManage_AnswersAccessDeniedCode` | `tests/RestaurantManagement.Web.Tests/Identity/MemberAccessEndpointTests.cs` |
| 34 | T7 | Failure | An Admin who gives a level above their own gets `access.level_above_own`, apart from `access.denied` | `RequireAsync_AdminGivesALevelAboveTheirOwn_ThrowsLevelAboveOwnCode` | `tests/RestaurantManagement.Modules.Identity.Tests/MemberDelegationRulesTests.cs` |
| 35 | T7 | Failure | Two invitations to one address at once: every loser gets 409 `invitation.already_open`, a lasting conflict | `CreateInvitation_SameEmailAtTheSameMoment_EveryLoserAnswersTheLastingConflictCode` | `tests/RestaurantManagement.Web.Tests/Identity/InvitationCreateRefusalEndpointTests.cs` |
| 36 | T7 | Failure | A role name used in another letter case answers `role.name_already_used` | `CreateAsync_NameUsedInAnotherLetterCase_ThrowsRoleNameAlreadyUsedCode` | `tests/RestaurantManagement.Modules.Identity.Tests/RoleServiceCreateTests.cs` |
| 37 | T8 | Happy | The API document lists every code of every catalog, with spot checks on Menu, Shared retry and validation codes | `SwaggerDocument_InDevelopment_CodeEnumEqualsTheUnionOfEveryCatalog` | `tests/RestaurantManagement.Web.Tests/Web/ErrorCodesOpenApiTests.cs` |
| 38 | T8 | Happy | The API document states the retry rule and lists the three retry codes | `SwaggerDocument_InDevelopment_SchemaDescriptionStatesTheRetryRuleAndListsTheRetryCodes` | `tests/RestaurantManagement.Web.Tests/Web/ErrorCodesOpenApiTests.cs` |
| 39 | T8 | Happy | Every operation declares its client and server error answers with the Problem Details schema | `SwaggerDocument_InDevelopment_EveryOperationDeclaresItsErrorAnswersWithTheProblemDetailsSchema` | `tests/RestaurantManagement.Web.Tests/Web/ErrorCodesOpenApiTests.cs` |
| 40 | T9 | Edge | A refusal cannot be built without a code: no public constructor lacks one | `Constructors_AllPublic_RequireAnErrorCode` | `tests/RestaurantManagement.Shared.Tests/BusinessExceptionTests.cs` |

## Spec Outcome coverage
| Outcome | Scenarios |
|---|---|
| #1 Every error is Problem Details with a stable `code` | 1, 2, 6, 9, 10, 11, 12, 13, 14, 15, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31, 32, 33, 34, 36, 40 |
| #2 Codes are never translated; `title` and `detail` stay localized | 1, 2, 14, 19, plus the existing language tests that now read `title` (see "Existing tests updated") |
| #3 Each 409 says "in progress" (retry) or a lasting conflict | 8, 35 (`request.in_progress` is only reserved today: the Idempotency-Key item returns it) |
| #4 The app retries only on network errors, 5xx, 409 "in progress" and 429 | 2, 8, 17, 38 (the rule is published, not enforced: see Assumptions) |
| #5 The codes appear in the OpenAPI document | 37, 38, 39 |

## Existing tests updated
Rewritten because the body changed shape (`message` becomes `title`); they fail until T1 and T3 (and the module task of the endpoint) are built, then pass:
- `ExceptionHandlingMiddlewareTests` (both tests replaced by scenarios 1 and 2)
- `ExceptionHandlingMiddlewareLocalizationTests` (both tests read `title`; no `detail` property in production)
- `LocalizedResponses.MessageAsync` now reads `title`; `CodeAsync` and `ProblemAsync` added (used by every endpoint test below)
- `UnexpectedErrorEndpointTests`, `InvalidTokenMessageTests`, `AccessDeniedMessageTests`, `PrivateByDefaultTests` (two tests read the Problem Details body and content type), `AuthRateLimitingTests` (two L16 tests read `title`)
- `OrganizationTenantEndpointTests` and `MenuTenantEndpointTests` (the `ErrorBody` record reads `Title`)
- `RefreshTokenCookieEndpointTests` (two uses of the code-less constructor moved to `BusinessException(SessionErrorCodes.InvalidOrExpiredSession, ...)`, needs T1 and T6)
- Every other endpoint test that reads an error body through `LocalizedResponses.MessageAsync` (Menu, Organization, Shared audit log) changes with the helper
- Test support: `PermissionProbeController` gained `BodilessConflict` (a bodiless 409 probe for scenario 19)

## Read closely before approving
- Scenarios 10, 13, 16, 21, 24 and 32: the anti-enumeration checks. Each asserts the same status, title and code for another organization's record and a missing one, so a wrong code or title would leak that the record exists.
- Scenario 26 and scenario 29: the collapsed login and organization-selection codes. The no-enumeration guarantee rests on one code per group.
- Scenarios 7, 8 and 37: format, uniqueness and retry-set checks over all catalogs, which T8 and every module depend on. The "exactly three retry codes" rule is fixed here.

## Assumptions
- The error codes and their statuses are fixed here (plan T1 asked the tester to fix them): the names are in the stubs' comments and in scenarios 4, 20, 23, 26 to 36; `ErrorCode(Value, Status, IsRetry)` is a record and the new `BusinessException(ErrorCode, string)` takes the status from it, so a code can never answer two statuses; catalogs expose public static `ErrorCode` properties and are read by `ErrorCodeCatalogs.Read(assemblies)` (a new Shared file, not named in the plan)
- The Problem Details schema in the OpenAPI document is named `ApiProblemDetails` and operations declare `4XX` and `5XX` ranges; the fallback codes are `http.bad_request`, `http.unauthorized`, `http.forbidden`, `http.not_found`, `http.method_not_allowed`, `http.conflict`, `http.unsupported_media_type`, `http.too_many_requests`, `http.internal_server_error`
- In production a 500 has no `detail` property at all (not `null`)
- Gaps left on purpose to stay inside 40 scenarios (the budget for an L plan): no unit test that `location.invalid_id` and `organization.required` (guard) are thrown (both are in the catalog test, scenario 4); no test of the unique-violation race path for a used location code, registered email or role name (only the pre-check answer is tested; the guide tells the implementer to use the same constant); no test of the Menu `organization.not_found`, `organization.required` and bad-user-identifier codes; no Organization unit test (the Organization codes are checked through the endpoints, scenarios 23 to 25); the locked-account login case is not in scenario 26 (already covered as one of the collapsed cases by the existing L5 tests)
- The retry rule is published (catalog flag and OpenAPI), not enforced, as the plan says
- Scenario 35 needs a real race on PostgreSQL: when no request loses the race the test passes without checking a code. It failed for the right reason on the run that built this file
- Matrix rows: new rows were added (status ❌) instead of editing the existing ones; the existing rows SH17, SH18, W1, W2, W3 and W22 were reworded to the Problem Details answer and set to ❌. Rows whose existing tests were only changed to read `title` stay ✅ even though those tests fail until T1 and T3 are built
