# Menu security matrix

The record of every rule the Menu module must keep, and the test that proves each one: who may
create, change and read menus and menu categories, and that an organization only ever sees and
changes its own. The pipeline (`.github/workflows/dotnet.yml`) runs every test named here, and
`.claude/scripts/check-security-matrices.py` fails the build when this file and the tests drift
apart.

## Rules for this file
The rules in the root `CLAUDE.md`, "Security matrices", apply: any change to Menu updates this
file in the same commit; new behaviour adds ❌ rows before it is built; no row is ✅ without a
named test; every test in `tests/RestaurantManagement.Modules.Menu.Tests/` and
`tests/RestaurantManagement.Web.Tests/Menu/` is named here and carries `[Trait("Rule", "<ID>")]`;
row IDs start with M (rules) or MD (wiring) and are never reused.

**Status:** ✅ covered by the named test · ⚠️ partly covered (the Expected column says what is
missing) · ❌ no test yet · 🐞 known bug, see the backlog item

**Where the tests live:** unit tests in `tests/RestaurantManagement.Modules.Menu.Tests/`,
endpoint and database tests in `tests/RestaurantManagement.Web.Tests/Menu/`.

## 1. Who may do what
| ID | Case | Expected | Status | Test |
|---|---|---|---|---|
| M4 | A member with no stored access in the menu's organization, even with access in another organization | 403 | ✅ | `MenuServiceTests.GetMenusAsync_MemberWithNoAccessYet_ThrowsForbidden`, `MenuGridEndpointTests.CreateMenu_ManageEverywhereOnlyInAnotherOrganization_ReturnsForbidden` |
| M5 | Menu categories: create, read, change, delete by a member with no stored access | 403 (the Menus level at the category's location is needed: Read to read, Manage to write) | ✅ | `MenuCategoryServiceTests.CreateAsync_MemberWithNoAccessYet_ThrowsForbidden`, `MenuCategoryServiceTests.GetByIdAsync_MemberWithNoAccessYet_ThrowsForbidden`, `MenuCategoryServiceTests.GetAllAsync_MemberWithNoAccessYet_ThrowsForbidden`, `MenuCategoryServiceTests.UpdateAsync_MemberWithNoAccessYet_ThrowsForbidden`, `MenuCategoryServiceTests.DeleteAsync_MemberWithNoAccessYet_ThrowsForbidden` |
| M6 | No organization chosen in the token | 403 "organization required" | ✅ | `MenuTenantEndpointTests.MenuEndpoints_NoOrganizationInToken_ReturnsForbiddenOrganizationRequired` |
| M32 | A member with Menus Read at one location (a Waiter at Batel) reads that location's menus | Allowed; at a location where the member has no level: 403 | ✅ | `MenuGridEndpointTests.GetMenusByLocation_WaiterWithMenusReadAtBatel_ReadsBatelAndIsRefusedAtMercado` |
| M33 | Categories follow the Menus level | A member with Menus Read at the location lists its categories; a member with Menus None does not (403) | ✅ | `MenuGridEndpointTests.GetMenuCategoriesByLocation_MenusReadAllowedAndMenusNoneRefused` |
| M34 | Editing a menu | Menus Manage allows it (a Manager); Menus Read refuses it (a Cashier): 403 | ✅ | `MenuGridEndpointTests.EditMenu_ManagerWithMenusManageAllowedAndCashierWithMenusReadRefused` |
| M35 | A member with Menus Read only creates, edits, deletes, links or unlinks a menu, or creates, changes or deletes a category | 403; nothing stored, changed or removed | ✅ | `MenuGridEndpointTests.MenuAndCategoryWrites_MemberWithMenusReadOnly_ReturnsForbiddenAndChangesNothing` |
| M36 | The same Admin manages Menus at one location and reads them at another | Edits the menu linked to the first, reads the second's menus, and is refused editing a menu linked to the second | ✅ | `MenuGridEndpointTests.EditMenu_AdminManagesBatelAndReadsMercado_EditsBatelMenuReadsMercadoAndIsRefusedEditingMercadoMenu` |
| M37 | A level is lowered between two requests with the same token | The second request is refused: a change applies at once, with no new login | ✅ | `MenuGridEndpointTests.EditMenu_ManageLoweredToReadBetweenTwoRequestsWithTheSameToken_SecondRequestIsRefused` |
| M38 | The Owner with no stored level | Passes every menu and category action | ✅ | `MenuGridEndpointTests.CreateMenu_OwnerWithNoStoredLevels_ReturnsOk` |
| M39 | Manage everywhere in another organization | Gives nothing here: 403 on a menu action | ✅ | `MenuGridEndpointTests.CreateMenu_ManageEverywhereOnlyInAnotherOrganization_ReturnsForbidden` |
| M40 | A menu or category write with Manage at every involved location | Allowed. Involved: the named locations (every non-closed one when none is named) for creating a menu; every location the menu is linked to (every non-closed one when none) for editing or deleting it; the location itself for linking, unlinking and a category of that location (every non-closed one for a category without a location) | ✅ | `MenuGridAccessTests.MenuAndCategoryWrites_ManageAtEveryInvolvedLocation_Succeeds` |
| M41 | The same writes with Manage missing at one involved location (Read there, or no level) | 403; nothing stored, changed or removed | ✅ | `MenuGridAccessTests.MenuAndCategoryWrites_ManageMissingAtAnInvolvedLocation_ThrowsForbiddenAndChangesNothing` |
| M42 | Reads with the Menus level at every involved location | Allowed: a location's menus or categories need Read there; the full menu list, all categories, a menu's categories and a category without a location need Read at every non-closed location | ✅ | `MenuGridAccessTests.MenuAndCategoryReads_MenusReadAtEveryInvolvedLocation_Succeeds` |
| M43 | The same reads with the Menus level missing at one involved location | 403 | ✅ | `MenuGridAccessTests.MenuAndCategoryReads_MenusReadMissingAtAnInvolvedLocation_ThrowsForbidden` |
| M44 | Token whose user identifier is not a user ID, on any menu or menu category endpoint | 401 `auth.unauthorized` with the translated title | ✅ | `MenuUserIdentifierEndpointTests.MenuEndpoints_UserIdentifierNotAUserId_ReturnsUnauthorizedWithLocalizedMessage`, `MenuUserIdentifierEndpointTests.MenuCategoryEndpoints_UserIdentifierNotAUserId_ReturnsUnauthorizedWithLocalizedMessage` |
| M47 | A member with Menus Read only edits a menu | 403 `access.denied` | ✅ | `MenuGridEndpointTests.EditMenu_MemberWithMenusReadOnly_AnswersAccessDeniedCode` |

## 2. Other organizations' data
| ID | Case | Expected | Status | Test |
|---|---|---|---|---|
| M7 | Another organization's menu, by its ID | 404 `menu.not_found` with the translated title, as if it did not exist; nothing changes | ✅ | `MenuTenantEndpointTests.MenuById_MenuOfAnotherOrganization_ReturnsNotFoundAndLeavesItUnchanged`, `MenuMessagesEndpointTests.EditMenu_MenuDoesNotExist_ReturnsNotFoundWithLocalizedMessage` |
| M8 | Another organization's category, by its ID | 404 `menu_category.not_found` with the translated title; nothing changes | ✅ | `MenuTenantEndpointTests.MenuCategoryById_CategoryOfAnotherOrganization_ReturnsNotFoundAndLeavesItUnchanged`, `MenuMessagesEndpointTests.MenuCategoryById_CategoryOfAnotherOrganization_ReturnsNotFoundWithLocalizedMessage` |
| M9 | Lists of menus and categories | Only the token organization's | ✅ | `MenuTenantEndpointTests.GetMenus_MenusInTwoOrganizations_ReturnsOnlyTokenOrganizationsMenus`, `MenuTenantEndpointTests.GetMenuCategories_CategoriesInTwoOrganizations_ReturnsOnlyTokenOrganizationsCategories` |
| M10 | Request body names another organization | Ignored: stored in the token organization | ✅ | `MenuTenantEndpointTests.CreateMenu_BodyNamesAnotherOrganization_StoresMenuInTokenOrganization`, `MenuTenantEndpointTests.CreateMenuCategory_BodyNamesAnotherOrganization_StoresCategoryInTokenOrganization` |
| M11 | Category on another organization's menu or location, or on one that does not exist | 404; nothing stored | ✅ | `MenuCategoryServiceTests.CreateAsync_WhenMenuBelongsToAnotherOrganization_ThrowsNotFoundAndStoresNothing`, `MenuCategoryServiceTests.CreateAsync_WhenMenuDoesNotExist_ThrowsNotFound`, `MenuCategoryServiceTests.CreateAsync_WhenLocationBelongsToAnotherOrganization_ThrowsNotFoundAndStoresNothing`, `MenuCategoryServiceTests.CreateAsync_WhenLocationDoesNotExist_ThrowsNotFoundAndStoresNothing` |
| M12 | Menus of another organization's location | 404, as if the location did not exist | ✅ | `MenuLocationEndpointTests.GetMenusByLocation_LocationOfAnotherOrganization_ReturnsNotFoundWithoutTheMenu` |
| M13 | Linking a menu to another organization's location | 404; no link stored | ✅ | `MenuLocationEndpointTests.AttachMenuToLocation_LocationOfAnotherOrganization_ReturnsNotFoundAndStoresNoLink` |
| M14 | Old routes with the organization in the address | 404 | ✅ | `MenuTenantEndpointTests.OldOrganizationInUrlRoute_Called_ReturnsNotFound` |
| M15 | Every write | Stamped with the token organization, the user and the time; a write that carries or changes to another organization, or has no organization, is refused with 403 and nothing is written | ✅ | `MenuAuditTests.SaveChangesAsync_NewMenuByUserInOrgA_StampsOrganizationAndCreationAudit`, `MenuAuditTests.SaveChangesAsync_UpdatedMenu_SetsUpdateAuditAndKeepsCreatedFields`, `MenuAuditTests.SaveChangesAsync_NewMenuCarriesAnotherOrganizationId_Throws403AndWritesNothing`, `MenuAuditTests.SaveChangesAsync_UserInOrgBUpdatesOrgAMenu_Throws403AndMenuUnchanged`, `MenuAuditTests.SaveChangesAsync_OrganizationIdChangedOnStoredMenu_Throws403AndMenuStaysInOrgA`, `MenuAuditTests.SaveChangesAsync_NewMenuWithoutCurrentOrganization_Throws403OrganizationRequired`, `MenuAuditTests.SaveChangesAsync_NewMenuCategory_StampsOrganizationAndUpdatedAt`, `MenuAuditTests.SaveChangesAsync_NewMenuCategoryWithoutLocation_KeepsLocationNull`, `MenuAuditTests.SaveChangesAsync_NewLocationMenu_GetsOrganizationFromUserAndUpdatedAt`, `MenuAuditTests.SaveChangesAsync_UpdatedMenuCategory_SetsUpdatedAtAndKeepsCreatedAt` |
| M46 | Another organization's menu or category, by its ID | The same 404 status, title and code (`menu.not_found`, `menu_category.not_found`) as one that does not exist | ✅ | `MenuTenantEndpointTests.MenuOrCategory_OfAnotherOrganization_AnswersTheSameStatusTitleAndCodeAsAMissingOne` |

## 3. Menus and categories
| ID | Case | Expected | Status | Test |
|---|---|---|---|---|
| M16 | A member who is allowed creates a menu with location IDs | Menu stored active; the token organization's locations are linked; IDs of other organizations' locations are ignored | ✅ | `MenuLocationEndpointTests.CreateMenu_LocationIdsOfTwoOrganizations_LinksOnlyTheTokenOrganizationsLocation` |
| M17 | Linking a menu to a location twice; unlinking one that is not linked | One link only; unlinking still answers OK and changes nothing | ✅ | `MenuLocationLinkTests.AttachMenuToLocationAsync_LinkedTwice_KeepsOneLink`, `MenuLocationLinkTests.DetachMenuFromLocationAsync_LocationNotLinked_AnswersOkAndChangesNothing` |
| M18 | Deleting a menu | Its categories and location links are removed with it | ✅ | `MenuDeleteCascadeTests.DeleteMenu_MenuWithCategoriesAndLocationLink_RemovesThemAndKeepsOtherMenusCategories` |
| M19 | A member who is allowed creates, reads, changes and deletes a category | Allowed; the location is kept when it belongs to the organization | ✅ | `MenuCategoryServiceTests.CreateAsync_WhenAccessIsAllowed_CreatesCategory`, `MenuCategoryServiceTests.CreateAsync_WhenLocationBelongsToOrganization_StoresLocationId`, `MenuCategoryServiceTests.GetByIdAsync_WhenAccessIsAllowed_ReturnsCategory`, `MenuCategoryServiceTests.UpdateAsync_WhenAccessIsAllowed_UpdatesCategory`, `MenuCategoryServiceTests.DeleteAsync_WhenAccessIsAllowed_DeletesCategory` |
| M20 | Category lists | Ordered by sort order, then name | ✅ | `MenuCategoryOrderTests.GetAllAsync_SortOrdersTieAndDiffer_ReturnsBySortOrderThenName`, `MenuCategoryOrderTests.GetByMenuIdAsync_SortOrdersTieAndDiffer_ReturnsBySortOrderThenName` |
| M45 | A menu or location that does not exist (edit a menu, create a category in a menu, link a menu to a location) | 404 with `menu.not_found` or `location.not_found` | ✅ | `MenuGridAccessTests.MenuAndCategoryServices_MenuOrLocationDoesNotExist_ThrowNotFoundWithTheirOwnCode` |
| M48 | Menu or category endpoints at their unversioned paths (`/api/menus`, `/api/menu-categories`) | 404; they answer only under `/api/v1/` | ✅ | `MenuVersionedRouteTests.UnversionedPath_MenuEndpoints_Answer404` |
| M49 | Create a menu with an `id` that is missing, null, not an ID or all zeros | 400 on the `Id` field, translated; nothing stored | ✅ | `MenuClientIdEndpointTests.CreateMenu_IdMissingNullMalformedOrEmpty_ReturnsBadRequestOnId` |
| M50 | Create a menu with an ID already used in the same organization | 409 `id.already_used`; the transaction is rolled back: no menu and no location link stored; the existing menu is unchanged | ✅ | `MenuClientIdEndpointTests.CreateMenu_IdUsedInSameOrganization_ReturnsConflictAndStoresNothing` |
| M51 | Create a menu with an ID used by another organization | The same status, title and code as M50 and nothing about that organization (no name, no ID) in the body; the other organization's menu is unchanged | ✅ | `MenuClientIdEndpointTests.CreateMenu_IdUsedByAnotherOrganization_AnswersTheSameConflictAndRevealsNothing` |
| M52 | A member without Menus Manage creates a menu with a taken ID | 403: access is checked before the ID, so the caller never learns whether an ID is taken | ✅ | `MenuClientIdEndpointTests.CreateMenu_MemberWithoutMenusManageSendsTakenId_ReturnsForbiddenNotConflict` |
| M53 | Create a menu with a new `id` | 200; the menu is stored under that ID in the token organization and the response carries it | ✅ | `MenuClientIdEndpointTests.CreateMenu_NewId_StoresMenuUnderThatIdInTokenOrganization` |
| M54 | Create a menu category with an `id` that is missing, null, not an ID or all zeros | 400 on the `Id` field, translated | ✅ | `MenuClientIdEndpointTests.CreateMenuCategory_IdMissingNullMalformedOrEmpty_ReturnsBadRequestOnId` |
| M55 | Create a category with an ID already used in the same organization | 409 `id.already_used`; the existing category is unchanged | ✅ | `MenuClientIdEndpointTests.CreateMenuCategory_IdUsedInSameOrganization_ReturnsConflictAndStoresNothing` |
| M56 | Create a category with an ID used by another organization | The same status, title and code as M55 and nothing about that organization in the body; the other organization's category is unchanged | ✅ | `MenuClientIdEndpointTests.CreateMenuCategory_IdUsedByAnotherOrganization_AnswersTheSameConflictAndRevealsNothing` |
| M57 | A member with Menus Read only creates a category with a taken ID | 403, not 409 | ✅ | `MenuClientIdEndpointTests.CreateMenuCategory_MemberWithMenusReadOnlySendsTakenId_ReturnsForbiddenNotConflict` |
| M58 | Create a category with a new `id` | 201; the category is stored under that ID in the token organization and the response carries it | ✅ | `MenuClientIdEndpointTests.CreateMenuCategory_NewId_StoresCategoryUnderThatIdInTokenOrganization` |

## 4. Input rules
| ID | Case | Expected | Status | Test |
|---|---|---|---|---|
| M21 | Menu name missing or over 100 characters | 400 with the translated message | ✅ | `MenuMessagesEndpointTests.CreateMenu_NameMissing_ReturnsLocalizedRequiredError`, `MenuMessagesEndpointTests.EditMenu_NameOver100Characters_ReturnsLocalizedMaxLengthError` |
| M22 | Menu description over 500 characters | 400 with the translated message; exactly 500 is accepted | ✅ | `MenuMessagesEndpointTests.EditMenu_DescriptionOver500Characters_ReturnsLocalizedMaxLengthError`, `MenuMessagesEndpointTests.EditMenu_DescriptionOf500Characters_IsAccepted` |
| M23 | Category name missing, description over 1000 characters, menu ID missing or not an ID | 400 with the translated message | ✅ | `MenuMessagesEndpointTests.CreateMenuCategory_NameMissing_ReturnsLocalizedRequiredError`, `MenuMessagesEndpointTests.UpdateMenuCategory_DescriptionOver1000Characters_ReturnsLocalizedMaxLengthError`, `RequiredIdFieldEndpointTests.IdMissingOrNull_ReturnsLocalizedRequiredError`, `RequiredIdFieldEndpointTests.IdNotAnId_ReturnsLocalizedRequiredError` |
| M24 | Category name over 255 characters | 400 with the translated message; exactly 255 is accepted | ✅ | `MenuMessagesEndpointTests.CreateMenuCategory_NameOver255Characters_ReturnsLocalizedMaxLengthError`, `MenuMessagesEndpointTests.CreateMenuCategory_NameOf255Characters_IsAccepted` |

## 5. Record versions (lost updates)
A menu or category change carries the version the caller read. Order of answers: 404, then 403, then 409 `version.conflict`.

| ID | Case | Expected | Status | Test |
|---|---|---|---|---|
| M59 | Edit or delete a menu without a version | 400 on the version field, translated; the menu is unchanged | ✅ | `MenuVersionEndpointTests.MenuChange_VersionMissing_ReturnsBadRequestOnTheVersionFieldAndChangesNothing` |
| M60 | Edit a menu with a version an earlier read left behind | 409 `version.conflict` with the current menu under `current`; the first change is kept | ✅ | `MenuVersionEndpointTests.EditMenu_VersionOfAnEarlierRead_ReturnsConflictWithTheCurrentMenuAndKeepsTheFirstChange` |
| M61 | Delete a menu with an old version | 409 with the current menu; the menu and its categories are still stored | ✅ | `MenuVersionEndpointTests.DeleteMenu_VersionOfAnEarlierRead_ReturnsConflictAndKeepsTheMenuAndItsCategories` |
| M62 | Edit a menu with the current version | 200; the response carries a new `version`, the stored one | ✅ | `MenuVersionEndpointTests.EditMenu_CurrentVersion_SavesAndReturnsANewVersion` |
| M63 | Delete a menu with the current version | 200; the menu is gone | ✅ | `MenuVersionEndpointTests.DeleteMenu_CurrentVersion_DeletesTheMenu` |
| M64 | Edit or delete another organization's menu with its real version | 404 `menu.not_found`, never 409; nothing about the menu in the body | ✅ | `MenuVersionEndpointTests.MenuChange_MenuOfAnotherOrganizationWithItsRealVersion_ReturnsNotFoundAndRevealsNothing` |
| M65 | A member with Menus Read only edits or deletes a menu with a stale version | 403, not 409: the access check comes first and the current menu is not shown | ✅ | `MenuVersionEndpointTests.MenuChange_MemberWithMenusReadOnlySendingAStaleVersion_ReturnsForbiddenNotConflict` |
| M66 | Menu list, list by location and create | Every menu carries a `version` | ✅ | `MenuVersionEndpointTests.MenuResponses_ListByLocationAndCreate_CarryTheVersion` |
| M67 | Update or delete a category without a version | 400 on the version field, translated; the category is unchanged | ✅ | `MenuCategoryVersionEndpointTests.CategoryChange_VersionMissing_ReturnsBadRequestOnTheVersionFieldAndChangesNothing` |
| M68 | Update or delete a category with an old version | 409 `version.conflict` with the current category under `current`; the category is kept as the first change left it | ✅ | `MenuCategoryVersionEndpointTests.CategoryChange_VersionOfAnEarlierRead_ReturnsConflictWithTheCurrentCategoryAndKeepsTheFirstChange` |
| M69 | Update a category with the current version | 200; the response carries a new `version` | ✅ | `MenuCategoryVersionEndpointTests.UpdateCategory_CurrentVersion_SavesAndReturnsANewVersion` |
| M70 | Delete a category with the current version | 204; the category is gone | ✅ | `MenuCategoryVersionEndpointTests.DeleteCategory_CurrentVersion_DeletesTheCategory` |
| M71 | Update or delete another organization's category with its real version | 404 `menu_category.not_found`, never 409; nothing about the category in the body | ✅ | `MenuCategoryVersionEndpointTests.CategoryChange_CategoryOfAnotherOrganizationWithItsRealVersion_ReturnsNotFoundAndRevealsNothing` |
| M72 | A member with Menus Read only updates or deletes a category with a stale version | 403, not 409; the current category is not shown | ✅ | `MenuCategoryVersionEndpointTests.CategoryChange_MemberWithMenusReadOnlySendingAStaleVersion_ReturnsForbiddenNotConflict` |
| M73 | A member without Menus Manage sends an `Idempotency-Key` on a menu create | 403; no key is stored | ✅ | `MenuIdempotencyEndpointTests.CreateMenu_MemberWithoutMenusManageSendsAKey_ReturnsForbiddenAndStoresNoKey` |
| M74 | A menu create with a reused menu ID and a key | 409 `id.already_used`; the key is released and can run a corrected request | ✅ | `MenuIdempotencyEndpointTests.CreateMenu_ReusedMenuIdWithAKey_ReturnsConflictAndReleasesTheKey` |
| M75 | A category create sent twice with one key | One category; the replay keeps 201 and `Location` | ✅ | `MenuIdempotencyEndpointTests.CreateMenuCategory_SameKeyTwice_CreatesOneCategoryAndReplaysCreatedWithLocation` |
| M76 | A category created with a device time 4 hours old | The new category row stores the corrected action time, the clock error and the received time | ✅ | `MenuCategoryClientTimeEndpointTests.CreateMenuCategory_SignedInWriteFourHoursOld_StoresTheTimesOnTheNewRow` |

## 6. Changes since (device catch-up)
The menu, link and category feeds return what changed after a cursor, plus tombstones of deleted records, at the header location (spec `docs/specs/shared/2026-11-02-changes-since-read-for-device-catch-up.md`).

| ID | Case | Expected | Status | Test |
|---|---|---|---|---|
| M77 | A menus, links or categories feed called without `X-Location-Id` | 400 `location.required` | ✅ | `MenuChangesAccessEndpointTests.GetChanges_NoLocationHeader_Returns400LocationRequired` |
| M78 | A member without Menus Read at the header location reads any menu feed | 403 `access.denied`, never records or tombstones | ✅ | `MenuChangesAccessEndpointTests.GetChanges_MemberWithoutMenusReadAtTheLocation_Returns403AccessDenied` |
| M79 | The Owner with no stored level, and a member with Menus Read, read any menu feed | 200: Read is enough | ✅ | `MenuChangesAccessEndpointTests.GetChanges_OwnerWithNoStoredLevelOrMemberWithMenusRead_Returns200` |
| M80 | A menu feed called with a cursor older than 14 days | 410 `changes.cursor_expired` | ✅ | `MenuChangesAccessEndpointTests.GetChanges_CursorOlderThanFourteenDays_Returns410CursorExpired` |
| M81 | A cursor of organization A sent with an organization B token | 400 `changes.cursor_invalid` | ✅ | `MenuChangesAccessEndpointTests.GetChanges_CursorOfAnotherOrganization_Returns400CursorInvalid` |
| M82 | First sync of the menus feed | Menus linked to the header location, a cursor, no tombstones; a menu linked only to another location is not returned | ✅ | `MenuChangesEndpointTests.GetMenuChanges_FirstSync_ReturnsOnlyMenusLinkedToTheHeaderLocationAndACursor` |
| M83 | One of three menus edited, then a sync with the new cursor and no writes | Exactly the edited menu, then an empty result | ✅ | `MenuChangesEndpointTests.GetMenuChanges_OneOfThreeMenusEdited_ReturnsExactlyItAndThenNothing` |
| M84 | Five menus read with page size 2 | Following the cursors returns each menu once; only the last page says no more remain | ✅ | `MenuChangesEndpointTests.GetMenuChanges_FiveMenusWithPageSizeTwo_FollowingTheCursorsReturnsEachOnce` |
| M85 | Two overlapping writes commit in reverse order | Neither is missed by the menus feed | ✅ | `MenuChangesEndpointTests.GetMenuChanges_TwoOverlappingWritesCommitInReverseOrder_NeitherIsMissed` |
| M86 | Another organization's menus, edits and deletes | Never returned in records or tombstones | ✅ | `MenuChangesEndpointTests.GetMenuChanges_AnotherOrganizationsMenusAndDeletes_NeverAppearForTheCaller` |
| M87 | A menu linked to two locations is detached from one | A tombstone at that location only; the other location sees nothing | ✅ | `MenuChangesEndpointTests.GetMenuChanges_MenuDetachedFromOneOfTwoLocations_IsATombstoneOnlyAtThatLocation` |
| M88 | A member loses Menus Read at the location, then syncs again | 403 `access.denied`, no records or tombstones | ✅ | `MenuChangesEndpointTests.GetMenuChanges_MemberLosesReadAtTheLocation_NextSyncIsRefusedWithoutRecordsOrTombstones` |
| M89 | A menu deleted 8 days ago | Still returned as a tombstone | ✅ | `MenuChangesEndpointTests.GetMenuChanges_MenuDeletedEightDaysAgo_IsStillReturnedAsATombstone` |
| M90 | An unchanged menu attached to the location | Returned as changed at that location | ✅ | `MenuChangesEndpointTests.GetMenuChanges_UnchangedMenuAttachedToTheLocation_IsReturnedAsChanged` |
| M91 | A menu with a location link is deleted | The menu is a tombstone in the menus feed and its link in the links feed | ✅ | `MenuChangesEndpointTests.GetChanges_MenuDeleted_MenuAndItsLinkComeBackAsTombstonesInTheirFeeds` |
| M92 | One of three menu-location links edited | The links feed returns exactly that link with its menu, location and active flag | ✅ | `MenuChangesEndpointTests.GetLinkChanges_OneLinkEdited_ReturnsExactlyThatLinkWithItsMenuAndLocation` |
| M93 | One of three categories edited; categories at another location or organization also edited | Exactly the edited category of the header location; the others never appear | ✅ | `MenuCategoryChangesEndpointTests.GetCategoryChanges_OneOfThreeCategoriesEdited_ReturnsExactlyItAndNeverOtherLocationsOrOrganizations` |
| M94 | A category deleted, and a menu with a category deleted | Both category IDs come back as tombstones | ✅ | `MenuCategoryChangesEndpointTests.GetCategoryChanges_CategoryDeletedAndMenuWithACategoryDeleted_BothComeBackAsTombstones` |

## 7. Schema tests (change stamps and tombstones)
| ID | Test | Protects |
|---|---|---|
| MD1 | `MenuChangeTrackingTests.Write_AnyStampedMenuTable_InsertSetsAndEditRaisesTheChangeStamp` | Menus, categories and links are stamped by the database on insert and on every edit |
| MD2 | `MenuChangeTrackingTests.DeleteMenu_LinkedToTwoLocationsWithOneCategory_LeavesTombstonesAtEachLocationItLeft` | A deleted menu leaves menu and link tombstones at each location and a category tombstone at its location, cascades included |
| MD3 | `MenuChangeTrackingTests.DetachMenu_FromOneLocation_LeavesLinkAndMenuTombstonesAtThatLocationOnly` | Detaching reports the link and the menu at that location only |
| MD4 | `MenuChangeTrackingTests.DeleteMenu_OfTrattoria_TombstonesCarryTrattoriasOrganizationNeverCantinas` | Every tombstone carries the organization of the deleted row |

## Open questions (no row until decided)
- **Categories of an unknown menu or location:** the lists answer 200 with an empty list, while
  an unknown menu elsewhere answers 404. Should these be 404 too?
