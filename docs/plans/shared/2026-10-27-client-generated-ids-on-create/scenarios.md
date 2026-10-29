# Scenarios: Client-generated IDs on create endpoints

**Plan:** docs/plans/shared/2026-10-27-client-generated-ids-on-create/plan.md
**Coverage:** 38 scenarios — 9 happy · 3 edge · 14 failure · 6 security · 6 guard

Failure and security scenarios fail today (the ID is ignored). Guard scenarios (#9, #14, #19, #24, #29, #34) pass today and must stay green: they prove access and the trial limit are checked before a clash can be seen.

| # | Task | Type | Scenario (plain words) | Test name | Test file |
|---|---|---|---|---|---|
| 1 | T1 | Happy | The reused-ID check says yes for a PostgreSQL primary key violation | `Matches_UniqueViolationOnAPrimaryKey_ReturnsTrue` | `tests/RestaurantManagement.Shared.Tests/PrimaryKeyViolationTests.cs` |
| 2 | T1 | Edge | The check says no for another unique index (location code, role name, email) and for another PostgreSQL error | `Matches_AnotherUniqueIndexOrAnotherPostgresError_ReturnsFalse` | `tests/RestaurantManagement.Shared.Tests/PrimaryKeyViolationTests.cs` |
| 3 | T1 | Edge | The check says no for an error that does not come from PostgreSQL | `Matches_ErrorThatIsNotFromPostgreSql_ReturnsFalse` | `tests/RestaurantManagement.Shared.Tests/PrimaryKeyViolationTests.cs` |
| 4 | T1 | Edge | The check says no when the save error has no database error behind it | `Matches_ErrorWithNoInnerException_ReturnsFalse` | `tests/RestaurantManagement.Shared.Tests/PrimaryKeyViolationTests.cs` |
| 5 | T1 | Happy | The "ID already used" title reads as real text in English, Portuguese and French | `Localizer_IdAlreadyUsed_ReturnsRealTextInEachLanguage` | `tests/RestaurantManagement.Shared.Tests/Localization/IdAlreadyUsedMessageTests.cs` |
| 6 | T2 | Failure | A menu create with no ID, a null ID, a text that is not an ID or all zeros is refused (400 on the ID field) | `CreateMenu_IdMissingNullMalformedOrEmpty_ReturnsBadRequestOnId` | `tests/RestaurantManagement.Web.Tests/Menu/MenuClientIdEndpointTests.cs` |
| 7 | T2 | Failure | A menu ID already used in the same organization is refused (409), nothing stored, no location link left | `CreateMenu_IdUsedInSameOrganization_ReturnsConflictAndStoresNothing` | `tests/RestaurantManagement.Web.Tests/Menu/MenuClientIdEndpointTests.cs` |
| 8 | T2 | Security | A menu ID used by another organization gets the same 409 and reveals nothing about that organization | `CreateMenu_IdUsedByAnotherOrganization_AnswersTheSameConflictAndRevealsNothing` | `tests/RestaurantManagement.Web.Tests/Menu/MenuClientIdEndpointTests.cs` |
| 9 | T2 | Guard | A member without Menus Manage sending a taken menu ID gets 403, not 409 | `CreateMenu_MemberWithoutMenusManageSendsTakenId_ReturnsForbiddenNotConflict` | `tests/RestaurantManagement.Web.Tests/Menu/MenuClientIdEndpointTests.cs` |
| 10 | T2 | Happy | A menu created with a new ID is stored under it and returned with it | `CreateMenu_NewId_StoresMenuUnderThatIdInTokenOrganization` | `tests/RestaurantManagement.Web.Tests/Menu/MenuClientIdEndpointTests.cs` |
| 11 | T3 | Failure | A category create with no ID, a null ID, a text that is not an ID or all zeros is refused (400 on the ID field) | `CreateMenuCategory_IdMissingNullMalformedOrEmpty_ReturnsBadRequestOnId` | `tests/RestaurantManagement.Web.Tests/Menu/MenuClientIdEndpointTests.cs` |
| 12 | T3 | Failure | A category ID already used in the same organization is refused (409), the existing category unchanged | `CreateMenuCategory_IdUsedInSameOrganization_ReturnsConflictAndStoresNothing` | `tests/RestaurantManagement.Web.Tests/Menu/MenuClientIdEndpointTests.cs` |
| 13 | T3 | Security | A category ID used by another organization gets the same 409 and reveals nothing about that organization | `CreateMenuCategory_IdUsedByAnotherOrganization_AnswersTheSameConflictAndRevealsNothing` | `tests/RestaurantManagement.Web.Tests/Menu/MenuClientIdEndpointTests.cs` |
| 14 | T3 | Guard | A member with Menus Read only sending a taken category ID gets 403, not 409 | `CreateMenuCategory_MemberWithMenusReadOnlySendsTakenId_ReturnsForbiddenNotConflict` | `tests/RestaurantManagement.Web.Tests/Menu/MenuClientIdEndpointTests.cs` |
| 15 | T3 | Happy | A category created with a new ID is stored under it and returned with it | `CreateMenuCategory_NewId_StoresCategoryUnderThatIdInTokenOrganization` | `tests/RestaurantManagement.Web.Tests/Menu/MenuClientIdEndpointTests.cs` |
| 16 | T4 | Failure | A location create with no ID, a null ID, a text that is not an ID or all zeros is refused (400 on the ID field) | `CreateLocation_IdMissingNullMalformedOrEmpty_ReturnsBadRequestOnId` | `tests/RestaurantManagement.Web.Tests/Organization/OrganizationClientIdEndpointTests.cs` |
| 17 | T4 | Failure | A location ID already used in the same organization is refused with 409, not reported as a taken code | `CreateLocation_IdUsedInSameOrganization_ReturnsConflictNotCodeError` | `tests/RestaurantManagement.Web.Tests/Organization/OrganizationClientIdEndpointTests.cs` |
| 18 | T4 | Security | A location ID used by another organization gets the same 409 and reveals nothing about that organization | `CreateLocation_IdUsedByAnotherOrganization_AnswersTheSameConflictAndRevealsNothing` | `tests/RestaurantManagement.Web.Tests/Organization/OrganizationClientIdEndpointTests.cs` |
| 19 | T4 | Guard | A non-Owner with Locations Manage sending a taken location ID gets 403, not 409 | `CreateLocation_NonOwnerWithLocationsManageSendsTakenId_ReturnsForbiddenNotConflict` | `tests/RestaurantManagement.Web.Tests/Organization/OrganizationClientIdEndpointTests.cs` |
| 20 | T4 | Happy | A location created with a new ID is stored under it and returned with it | `CreateLocation_NewId_StoresLocationUnderThatIdInTokenOrganization` | `tests/RestaurantManagement.Web.Tests/Organization/OrganizationClientIdEndpointTests.cs` |
| 21 | T5 | Failure | An organization create with no ID, a null ID, a text that is not an ID or all zeros is refused (400 on the ID field) | `CreateOrganization_IdMissingNullMalformedOrEmpty_ReturnsBadRequestOnId` | `tests/RestaurantManagement.Web.Tests/Organization/OrganizationClientIdEndpointTests.cs` |
| 22 | T5 | Failure | The ID of one of the user's own organizations is refused with 409 (not 500) and creates nothing, no Owner access included | `CreateOrganization_IdOfUsersOwnOrganization_ReturnsConflictAndCreatesNothing` | `tests/RestaurantManagement.Web.Tests/Organization/OrganizationClientIdEndpointTests.cs` |
| 23 | T5 | Security | The ID of another user's organization gets the same 409, reveals nothing, and gives the caller no access | `CreateOrganization_IdOfAnotherUsersOrganization_AnswersTheSameConflictAndRevealsNothing` | `tests/RestaurantManagement.Web.Tests/Organization/OrganizationClientIdEndpointTests.cs` |
| 24 | T5 | Guard | A user whose organization is on the trial plan sending a taken ID still gets the trial-limit refusal first | `CreateOrganization_UserInTrialSendsTakenId_ReturnsTrialLimitNotConflict` | `tests/RestaurantManagement.Web.Tests/Organization/OrganizationClientIdEndpointTests.cs` |
| 25 | T5 | Happy | An organization created with a new ID is stored under it, returned with it, and its creator becomes Owner | `CreateOrganization_NewId_StoresOrganizationUnderThatIdWithOwnerAccess` | `tests/RestaurantManagement.Web.Tests/Organization/OrganizationClientIdEndpointTests.cs` |
| 26 | T6 | Failure | A role create with no ID, a null ID, a text that is not an ID or all zeros is refused (400 on the ID field), nothing stored | `CreateRole_IdMissingNullMalformedOrEmpty_ReturnsBadRequestOnIdAndStoresNothing` | `tests/RestaurantManagement.Web.Tests/Identity/RoleClientIdEndpointTests.cs` |
| 27 | T6 | Failure | A role ID already used in the same organization is refused (409); no role, level row or audit entry is stored | `CreateRole_IdUsedInSameOrganization_ReturnsConflictAndStoresNothing` | `tests/RestaurantManagement.Web.Tests/Identity/RoleClientIdEndpointTests.cs` |
| 28 | T6 | Security | A role ID used by another organization gets the same 409 and reveals nothing about that organization | `CreateRole_IdUsedByAnotherOrganization_AnswersTheSameConflictAndRevealsNothing` | `tests/RestaurantManagement.Web.Tests/Identity/RoleClientIdEndpointTests.cs` |
| 29 | T6 | Guard | A member without Members Manage sending a taken role ID gets 403, not 409 | `CreateRole_MemberWithoutMembersManageSendsTakenId_ReturnsForbiddenNotConflict` | `tests/RestaurantManagement.Web.Tests/Identity/RoleClientIdEndpointTests.cs` |
| 30 | T6 | Happy | A role created with a new ID is stored with its levels under it; an organization named in the body is ignored | `CreateRole_NewId_StoresRoleUnderThatId` | `tests/RestaurantManagement.Web.Tests/Identity/RoleClientIdEndpointTests.cs` |
| 31 | T7 | Failure | An invitation with no ID, a null ID, a text that is not an ID or all zeros is refused (400 on the ID field); nothing stored, no email | `CreateInvitation_IdMissingNullMalformedOrEmpty_ReturnsBadRequestOnIdAndSendsNothing` | `tests/RestaurantManagement.Web.Tests/Identity/InvitationClientIdEndpointTests.cs` |
| 32 | T7 | Failure | A retried invitation with the same ID is refused (409); the first stays open and no second email goes out | `CreateInvitation_RetriedWithTheSameId_ReturnsConflictKeepsTheFirstOpenAndSendsNoSecondEmail` | `tests/RestaurantManagement.Web.Tests/Identity/InvitationClientIdEndpointTests.cs` |
| 33 | T7 | Security | An invitation ID used by another organization gets the same 409 and reveals nothing about that organization | `CreateInvitation_IdUsedByAnotherOrganization_AnswersTheSameConflictAndRevealsNothing` | `tests/RestaurantManagement.Web.Tests/Identity/InvitationClientIdEndpointTests.cs` |
| 34 | T7 | Guard | A member without Members Manage sending a taken invitation ID gets 403, not 409 | `CreateInvitation_MemberWithoutMembersManageSendsTakenId_ReturnsForbiddenNotConflict` | `tests/RestaurantManagement.Web.Tests/Identity/InvitationClientIdEndpointTests.cs` |
| 35 | T7 | Happy | An invitation created with a new ID is stored under it, returned with it; an organization named in the body is ignored; the accept token is separate | `CreateInvitation_NewId_StoresInvitationUnderThatId` | `tests/RestaurantManagement.Web.Tests/Identity/InvitationClientIdEndpointTests.cs` |
| 36 | T2–T7 | Happy | The API description shows a required UUID `id` on each of the six create requests (six cases) | `V1Document_CreateRequest_ShowsIdAsRequiredUuid` | `tests/RestaurantManagement.Web.Tests/Web/ClientIdOpenApiTests.cs` |
| 37 | T4 | Failure | A retried location create (same ID, same code) is refused with 409 `id.already_used`, not a taken-code error; nothing new stored (added after review) | `CreateLocation_RetriedWithTheSameIdAndCode_ReturnsIdConflictAndCreatesNothingNew` | `tests/RestaurantManagement.Web.Tests/Organization/LocationClientIdRetryEndpointTests.cs` |
| 38 | T6 | Failure | A retried role create (same ID, same name) is refused with 409 `id.already_used`, not a taken-name error; nothing new stored (added after review) | `CreateRole_RetriedWithTheSameIdAndName_ReturnsIdConflictAndCreatesNothingNew` | `tests/RestaurantManagement.Web.Tests/Identity/RoleClientIdRetryEndpointTests.cs` |

## Spec Outcome coverage
Outcomes are the backlog item's Acceptance criteria (`backlog/offline/2026-10-26-client-generated-ids-on-create.md`), numbered in order.

| Outcome | Scenarios |
|---|---|
| #1 Create requests accept an `id` and the server stores the record under it | 10, 15, 20, 25, 30, 35 |
| #2 An empty UUID (and a missing, null or malformed one) returns 400 | 6, 11, 16, 21, 26, 31 |
| #3 A UUID already used in the same organization returns 409 | 7, 12, 17, 22, 27, 32, 37, 38 |
| #4 A UUID used in another organization returns the same plain 409 with nothing about that organization | 8, 13, 18, 23, 28, 33 (and 1–5 for the shared check, code and text) |
| #5 The organization still comes only from the token | 10, 30, 35 (body names another organization); existing M10 / OR16 tests for menu, category and location (updated, see below); not applicable to organizations (T5) |
| #6 The OpenAPI document shows `id` on each create request | 36 |

Access order (403 before 409, trial limit before 409) is covered by the Guard scenarios 9, 14, 19, 24, 29, 34.

## Existing tests updated
Only what the plan's change forces. All still pass today; the `id` is ignored until the tasks are built.
- Web.Tests posts of these creates now send an `id`: `MenuGridEndpointTests`, `MenuLocationEndpointTests`, `MenuTenantEndpointTests`, `MenuUserIdentifierEndpointTests`, `MenuMessagesEndpointTests` (one accepted-name case), `TokenRolesEndpointTests`, `OrganizationGridEndpointTests`, `OrganizationTenantEndpointTests`, `LocationCodeEndpointTests`, `OrganizationUserIdentifierEndpointTests`, `OrganizationFieldsEndpointTests`, `OrganizationPlanEndpointTests`, `RoleEndpointTests`, `InvitationEmailValidationEndpointTests`, `InvitationSizeLimitEndpointTests`, `InvitationCreateRefusalEndpointTests`; the shared helper `InviteBody` in `InvitationWebSupport` sends a new `id` per call (an optional `id` argument is added).
- `InvitationCreateRefusalEndpointTests` (T89, T145): each of the eight concurrent requests now has its own ID, so the race still tests the open-invitation rule, not a reused ID.
- Unit tests that run past the ID now set `Id = Guid.NewGuid()` (the service stores it and uses it for linked rows): `OrganizationCreationScopeTests`, `OrganizationServiceCreateTests`, `OrganizationServiceReservedNameTests`, `OrganizationSettingsTests`, `RoleServiceCreateTests` (levels case), the `Invite` helper in `InvitationTestSupport`.
- `ErrorCodeCatalogTests` (SH52): one data row for `id.already_used`, 409. It passes today because the stub holds the code as data.
- Left as they are: the 400 message tests (`MenuMessagesEndpointTests`, `OrganizationMessagesEndpointTests`, `LocationMessagesEndpointTests`, `RequiredIdFieldEndpointTests`) still pass because they only look for their own message among the field errors.

## Read closely before approving
- #8, #13, #18, #23, #28, #33 (cross-organization 409): the tenant leak risk. Each must show the same status, title and code as the same-organization 409, nothing about the other organization in the body, and that organization's row unchanged.
- #22 and #23 (organization create): a clash with the user's own organization must give 409, not 500, and leave no Owner access row. #24 keeps the trial limit before the ID.
- #9, #14, #19, #29, #34 (Guard, 403 before 409): a missing or malformed ID gives 400 before the access check (attribute validation), so 403 is promised only for a well-formed request. Also #17: a primary key clash must not come back as a taken-code error.

## Assumptions
- The new classes are Web.Tests (PostgreSQL) only: the clash is a database primary key violation and the 400 / 403 / 409 answers are HTTP behaviour. No new module unit tests; the existing unit tests were only given an `Id`.
- Accepted by the user (decision 2): the primary key check matches lower-case `pk_<table>`. Primary key constraint names are lower case (`pk_menus`), set by the snake case naming convention, not `PK_<table>` as the batch text says; the Shared tests use `pk_...`.
- The 400 for an empty UUID reports on the field `Id` with the translated required message ("The Id field is required."), like `MenuId` in Shared row SH20.
- Accepted by the user (decision 1): a missing or malformed ID gives 400 before the access check, as in SH20. Role and invitation validation by attribute (400) happens before the access checks, so a caller without the right who also sends a missing ID gets 400; 403 is promised only for a well-formed request.
- A user's own organization needs a non-trial plan in the test so the trial limit does not answer first; the tests move Cantina to `ENTERPRISE` for that.
- Scenario 36 sits in `Web.Tests/Web/` with matrix row WD11 (web matrix) because one test covers all six modules.
- Empty GUID keys are replaced by EF Core on add, so unit tests that do not set an `Id` and never read it back keep working; the ones that read it back were updated.
