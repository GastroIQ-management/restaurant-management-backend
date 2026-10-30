# Scenarios: Record versions to prevent lost updates (optimistic concurrency)

**Plan:** docs/plans/shared/2026-10-29-optimistic-concurrency-versions/plan.md
**Coverage:** 40 scenarios — 9 happy · 5 edge · 15 failure · 11 security · 0 guard

Rule IDs are in the matrices (`docs/security/*-matrix.md`). Paths below are relative to `tests/RestaurantManagement.`. "Version" is the record's PostgreSQL `xmin` (the hidden per-row number that changes on every update).

| # | Task | Type | Scenario (plain words) | Test name | Test file |
|---|---|---|---|---|---|
| 1 | T1 | Failure | The check refuses a change sent with a version different from the stored one and carries the record as stored now | `Require_SentVersionDiffersFromStored_ThrowsVersionConflictWithCurrentRecord` | `Shared.Tests/Data/VersionCheckTests.cs` |
| 2 | T1 | Happy | A change sent with the stored version passes and is saved (also on SQLite, where the module tests run) | `Require_SentVersionEqualsStored_PassesAndTheChangeIsSaved` | `Shared.Tests/Data/VersionCheckTests.cs` |
| 3 | T1 | Failure | A row changed between the read and the save makes the save end as the same version conflict, and the losing change is not applied | `SaveAsync_RowChangedAfterItWasRead_ThrowsVersionConflictWithTheFreshRecord` | `Shared.Tests/Data/VersionCheckTests.cs` |
| 4 | T1 | Failure | A version conflict is answered 409 with code `version.conflict`, the record under `current` and a title in English, Portuguese and French | `InvokeAsync_VersionConflict_Answers409WithCodeCurrentRecordAndTranslatedTitle` | `Shared.Tests/Middleware/ExceptionHandlingMiddlewareVersionConflictTests.cs` |
| 5 | T1 | Security | The conflict body holds only the problem fields and the given record, no stack trace or detail, even in Development | `InvokeAsync_VersionConflictInDevelopment_BodyHoldsOnlyTheProblemFieldsAndTheCurrentRecord` | `Shared.Tests/Middleware/ExceptionHandlingMiddlewareVersionConflictTests.cs` |
| 6 | T1 | Happy | On PostgreSQL a versioned row gets a new version on every update, read back after the save, and no version column is stored | `SaveChanges_VersionedRowUpdated_VersionChangesAndNoVersionColumnIsStored` | `Web.Tests/Shared/RowVersionPostgresTests.cs` |
| 7 | T1 | Failure | On PostgreSQL a save made with an old version fails and the newer change stays | `SaveAsync_RowUpdatedByAnotherContextAfterTheRead_ThrowsVersionConflict` | `Web.Tests/Shared/RowVersionPostgresTests.cs` |
| 8 | T2 | Failure | Editing or deleting a menu without a version is refused with 400 on the version field, and the menu is unchanged | `MenuChange_VersionMissing_ReturnsBadRequestOnTheVersionFieldAndChangesNothing` | `Web.Tests/Menu/MenuVersionEndpointTests.cs` |
| 9 | T2 | Failure | Editing a menu with a version an earlier read left behind answers 409 with the current menu; the first change is kept (this is also the "second edit with the old version" case) | `EditMenu_VersionOfAnEarlierRead_ReturnsConflictWithTheCurrentMenuAndKeepsTheFirstChange` | `Web.Tests/Menu/MenuVersionEndpointTests.cs` |
| 10 | T2 | Failure | Deleting a menu with an old version answers 409; the menu and its categories are still stored | `DeleteMenu_VersionOfAnEarlierRead_ReturnsConflictAndKeepsTheMenuAndItsCategories` | `Web.Tests/Menu/MenuVersionEndpointTests.cs` |
| 11 | T2 | Happy | Editing a menu with the current version saves it and returns a new version | `EditMenu_CurrentVersion_SavesAndReturnsANewVersion` | `Web.Tests/Menu/MenuVersionEndpointTests.cs` |
| 12 | T2 | Happy | Deleting a menu with the current version deletes it | `DeleteMenu_CurrentVersion_DeletesTheMenu` | `Web.Tests/Menu/MenuVersionEndpointTests.cs` |
| 13 | T2 | Security | Another organization's menu, sent with its real version, is "not found", never a conflict, and nothing about it is in the body | `MenuChange_MenuOfAnotherOrganizationWithItsRealVersion_ReturnsNotFoundAndRevealsNothing` | `Web.Tests/Menu/MenuVersionEndpointTests.cs` |
| 14 | T2 | Security | A member who can only read menus and sends a stale version gets 403, not 409, and is not shown the menu | `MenuChange_MemberWithMenusReadOnlySendingAStaleVersion_ReturnsForbiddenNotConflict` | `Web.Tests/Menu/MenuVersionEndpointTests.cs` |
| 15 | T2 | Edge | The menu list, the list by location and a new menu all carry a version | `MenuResponses_ListByLocationAndCreate_CarryTheVersion` | `Web.Tests/Menu/MenuVersionEndpointTests.cs` |
| 16 | T3 | Failure | Updating or deleting a category without a version is refused with 400 on the version field | `CategoryChange_VersionMissing_ReturnsBadRequestOnTheVersionFieldAndChangesNothing` | `Web.Tests/Menu/MenuCategoryVersionEndpointTests.cs` |
| 17 | T3 | Failure | Updating or deleting a category with an old version answers 409 with the current category; the first change is kept | `CategoryChange_VersionOfAnEarlierRead_ReturnsConflictWithTheCurrentCategoryAndKeepsTheFirstChange` | `Web.Tests/Menu/MenuCategoryVersionEndpointTests.cs` |
| 18 | T3 | Happy | Updating a category with the current version saves it and returns a new version | `UpdateCategory_CurrentVersion_SavesAndReturnsANewVersion` | `Web.Tests/Menu/MenuCategoryVersionEndpointTests.cs` |
| 19 | T3 | Happy | Deleting a category with the current version deletes it | `DeleteCategory_CurrentVersion_DeletesTheCategory` | `Web.Tests/Menu/MenuCategoryVersionEndpointTests.cs` |
| 20 | T3 | Security | Another organization's category with its real version is "not found", never a conflict | `CategoryChange_CategoryOfAnotherOrganizationWithItsRealVersion_ReturnsNotFoundAndRevealsNothing` | `Web.Tests/Menu/MenuCategoryVersionEndpointTests.cs` |
| 21 | T3 | Security | A member who can only read menus and sends a stale version gets 403, not 409 | `CategoryChange_MemberWithMenusReadOnlySendingAStaleVersion_ReturnsForbiddenNotConflict` | `Web.Tests/Menu/MenuCategoryVersionEndpointTests.cs` |
| 22 | T4 | Failure | Editing or closing a location without a version is refused with 400 on the version field | `LocationChange_VersionMissing_ReturnsBadRequestOnTheVersionFieldAndChangesNothing` | `Web.Tests/Organization/LocationVersionEndpointTests.cs` |
| 23 | T4 | Failure | Editing or closing a location with an old version answers 409 with the current location; it stays open and no audit entry is written | `LocationChange_VersionOfAnEarlierRead_ReturnsConflictWithTheCurrentLocationChangesNothingAndWritesNoAuditEntry` | `Web.Tests/Organization/LocationVersionEndpointTests.cs` |
| 24 | T4 | Edge | A stale edit that also uses a code another location has answers the version conflict, not the taken-code error | `EditLocation_StaleVersionWithACodeAnotherLocationUses_ReturnsVersionConflictNotTheCodeError` | `Web.Tests/Organization/LocationVersionEndpointTests.cs` |
| 25 | T4 | Happy | Editing a location with the current version saves it and returns a new version | `EditLocation_CurrentVersion_SavesAndReturnsANewVersion` | `Web.Tests/Organization/LocationVersionEndpointTests.cs` |
| 26 | T4 | Security | Another organization's location with its real version is "not found", never a conflict | `LocationChange_LocationOfAnotherOrganizationWithItsRealVersion_ReturnsNotFoundAndRevealsNothing` | `Web.Tests/Organization/LocationVersionEndpointTests.cs` |
| 27 | T4 | Security | A member who only reads locations (edit) and a non-Owner (close) get 403, not 409, with a stale version | `LocationChange_MemberWhoMayNotMakeIt_SendingAStaleVersion_ReturnsForbiddenNotConflict` | `Web.Tests/Organization/LocationVersionEndpointTests.cs` |
| 28 | T5 | Failure | Editing the organization without a version is refused with 400 on the version field | `EditOrganization_VersionMissing_ReturnsBadRequestOnTheVersionFieldAndChangesNothing` | `Web.Tests/Organization/OrganizationVersionEndpointTests.cs` |
| 29 | T5 | Failure | Editing the organization with an old version answers 409 with the current organization; organization and settings are unchanged and no audit entry is written | `EditOrganization_VersionOfAnEarlierRead_ReturnsConflictWithTheCurrentOrganizationChangesNothingAndWritesNoAuditEntry` | `Web.Tests/Organization/OrganizationVersionEndpointTests.cs` |
| 30 | T5 | Edge | Changing only a setting (not the name) still gives the organization a new version | `EditOrganization_OnlyASettingChanges_ReturnsANewVersion` | `Web.Tests/Organization/OrganizationVersionEndpointTests.cs` |
| 31 | T5 | Security | A member who is not the Owner, with a stale version, gets 403, not 409 | `EditOrganization_MemberWhoIsNotTheOwnerSendingAStaleVersion_ReturnsForbiddenNotConflict` | `Web.Tests/Organization/OrganizationVersionEndpointTests.cs` |
| 32 | T5 | Security | An Owner of two organizations edits only the one in the token; the other keeps its data and version | `EditOrganization_OwnerOfTwoOrganizations_ChangesOnlyTheTokenOrganizationAndItsVersion` | `Web.Tests/Organization/OrganizationVersionEndpointTests.cs` |
| 33 | T5 | Edge | The organization list and a new organization both carry a version | `OrganizationResponses_ListAndCreate_CarryTheVersion` | `Web.Tests/Organization/OrganizationVersionEndpointTests.cs` |
| 34 | T6 | Failure | Renaming a role or updating its levels without a version is refused with 400 on the version field | `EditRole_VersionMissing_ReturnsBadRequestOnTheVersionFieldAndChangesNothing` | `Web.Tests/Identity/RoleVersionEndpointTests.cs` |
| 35 | T6 | Failure | A levels update, a rename and a same-name rename sent with an old version (read before another levels update) all answer 409 with the current role and its levels, and apply and audit nothing | `EditRole_VersionOfAnEarlierRead_ReturnsConflictWithTheCurrentRoleAndAppliesNothing` | `Web.Tests/Identity/RoleVersionEndpointTests.cs` |
| 36 | T6 | Happy | A rename, and a levels update alone, with the current version save and return a new version | `EditRole_CurrentVersion_SavesAndReturnsANewVersion` | `Web.Tests/Identity/RoleVersionEndpointTests.cs` |
| 37 | T6 | Security | Another organization's role with its real version is "not found", never a conflict | `EditRole_RoleOfAnotherOrganizationWithItsRealVersion_ReturnsNotFoundAndRevealsNothing` | `Web.Tests/Identity/RoleVersionEndpointTests.cs` |
| 38 | T6 | Security | An Admin the role rules refuse (no Cashier level, editing Cashier) with a stale version gets 403, not 409 | `EditRole_AdminTheRoleRulesRefuseSendingAStaleVersion_ReturnsForbiddenNotConflict` | `Web.Tests/Identity/RoleVersionEndpointTests.cs` |
| 39 | T6 | Edge | The role list and the single role show the same stored version | `RoleResponses_ListAndGet_CarryTheSameVersion` | `Web.Tests/Identity/RoleVersionEndpointTests.cs` |
| 40 | T2–T6 | Happy | The OpenAPI document shows `version` on the 11 response and update-request schemas (required on the six requests) and a required `version` query parameter on the menu, category and location deletes | `SwaggerDocument_InDevelopment_ShowsVersionOnEditableRecordsAndOnEveryDelete` | `Web.Tests/Web/VersionOpenApiTests.cs` |

## Spec Outcome coverage
The source is the backlog item's acceptance criteria (no spec file).

| Outcome | Scenarios |
|---|---|
| #1 `xmin` is the concurrency token | 1, 2, 3, 6, 7 |
| #2 GET responses include `version` | 15, 33, 39 (and every scenario that reads a version first) |
| #3 Update and delete send back the version they read | 8, 16, 22, 28, 34, 40 |
| #4 A stale version answers 409 with the current record | 1, 3, 4, 5, 7, 9, 10, 17, 23, 24, 29, 35 |
| #5 The current version succeeds and returns the new version | 2, 6, 11, 12, 18, 19, 25, 30, 36 |
| #6 The OpenAPI document shows the `version` fields | 40 (the `version.conflict` code in the document is already checked by the existing `ErrorCodesOpenApiTests`, WD4, which reads every catalog) |

Security order of answers (404, then 403, then 409, plan Flags) is proved by 13, 14, 20, 21, 26, 27, 31, 37, 38.

## Existing tests updated
Forced by the plan: every edit and delete of a menu, category, location, organization or role now needs a version, and a missing one answers 400 before the 404 and 403 checks.
- Refusal tests (403, 404, validation, 401 identifier) now send `version` 1 in the body or `?version=1` in the URL; they pass before and after the change: `MenuGridEndpointTests` (M35, M37 second edit, M47), `MenuTenantEndpointTests` (M7, M8, M46), `MenuMessagesEndpointTests`, `MenuUserIdentifierEndpointTests`, `OrganizationTenantEndpointTests` (OR14, OR46), `OrganizationMessagesEndpointTests`, `LocationMessagesEndpointTests`, `OrganizationUserIdentifierEndpointTests`, `OrganizationGridEndpointTests` (OR39 refusal, OR47), `RoleEditEndpointTests` (T124–T129); helpers `LocalizedResponses.Request` (a delete gets `?version=1`).
- 25 success tests (counting each row of a parameterized test) now send the version they read, so they fail until the task that builds the version is done; the other 125 tests of the same classes pass (listed in each task's Implementation guide): `MenuGridEndpointTests` (M34 Manager, M36, M37 first edit), `MenuDeleteCascadeTests` (M18), `MenuMessagesEndpointTests` (M22 accepted) — T2; `LocationCodeEndpointTests` (OR27, OR28 closing steps and taken-code edit), `OrganizationGridEndpointTests` (OR39 first edit, OR40 three rows), `AuditLogRecordingEndpointTests` (SH16) — T4; `OrganizationFieldsEndpointTests` (OR22, OR7, OR16), `OrganizationTenantEndpointTests` (OR16), `OrganizationGridEndpointTests` (OR42 first edit), `AuditLogRecordingEndpointTests` (SH13) — T5; `RoleEditEndpointTests` (T121, T122, T123) — T6.
- Module unit tests: the `Delete*` service methods gained a `uint version` parameter (unused by the stub), so the calls in `MenuCategoryServiceTests`, `MenuGridAccessTests`, `LocationServiceAuditTests` and `LocationServiceTests` pass `0` (the version a SQLite row has); they pass before and after.
- Shared helper added: `Web.Tests/VersionedRequests.cs` (reads a record's version and asserts it is not 0; sends deletes with `?version=`). No new fixture and no new test project.

## Read closely before approving
- #13, #14, #20, #21, #26, #27, #31, #37, #38: the order 404, then 403, then 409. If a 409 body reaches a caller who is refused, it leaks a record.
- #1, #3 and #35: #1 and #3 are the core of the check. #35 requires a levels-only change to bump the role's version, because child rows alone would not.
- #8, #16, #22, #28, #34: a missing version answers 400 before 404 and 403. This changes about 14 existing test classes. Confirm you accept 400 ahead of 404.

## Assumptions
- A stale version on a record deleted meanwhile answering 404 (plan Assumptions) has no scenario: the scenario budget for size L is full; it is the ordinary not-found path, which the 404 scenarios already cover.
- The new endpoint test classes use the shared namespace `RestaurantManagement.Web.Tests`, like every existing class in the module folders.
- Test run: Shared.Tests, Menu, Organization and Identity unit projects pass except the 7 new Shared tests (they fail on `NotImplementedException` or on the missing 409). The 14 Web.Tests classes that were edited: 125 pass, 25 fail for the missing `version` as listed above. The one full `affected` run (Web.Tests included) was stopped after about 40 minutes with nothing finished: it was stuck on CPU-bound test classes whose databases stayed empty, a problem of the local Docker or machine that I could not tie to these changes (the same classes run in seconds alone). CI (Continuous Integration) runs the full suite; please confirm it there.
- A missing `version` answers 400 before the 404 and 403 checks (the request is refused while binding, as the plan states); only a present version is checked after them.
- A race lost between the read and the save is proved in Shared (#3 on SQLite, #7 on PostgreSQL); the endpoint tests use a stale version from an earlier read, since a real race cannot be timed over HTTP.
- On SQLite the version token is a plain value that never changes (stays 0), so version behaviour that needs `xmin` is tested only on PostgreSQL (#6, #7 and all endpoint tests).
- Stubs for the entities are not added: a `Version` property on an entity without a migration would break every existing PostgreSQL test. Only response and request models (`Version`) and the delete service signatures changed, with the existing bodies kept, so the build and existing tests stay green.
- The 400 for a missing version is asserted on the field name `version` (case ignored), not on its translated text.
- The conflict title texts are fixed by test #4 (English, Portuguese, French); the implementer may change the wording only by changing the test before Gate 2.
- Create endpoints (menu, organization) carry a version (#15, #33); category, location and role creates are built by the same mapping functions as the lists and updates, so they are not tested separately.
