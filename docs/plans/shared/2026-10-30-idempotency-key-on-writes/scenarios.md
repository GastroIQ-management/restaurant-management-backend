# Scenarios: Idempotency key on writes

**Plan:** docs/plans/shared/2026-10-30-idempotency-key-on-writes/plan.md
**Coverage:** 40 scenarios — 12 happy · 11 edge · 10 failure · 2 security · 5 guard

| # | Task | Type | Scenario (plain words) | Test name | Test file |
|---|---|---|---|---|---|
| 1 | T1 | Edge | A stored key used again while it runs, or with another request, is refused | `IdempotencyKeyStoreTests.ReserveAsync_KeyAlreadyStored_RefusesByStateAndRequest` | `tests/RestaurantManagement.Shared.Tests/Idempotency/IdempotencyKeyStoreTests.cs` |
| 2 | T1 | Security | The same key from another user, another organization or with no organization is separate and never returns the first answer | `IdempotencyKeyStoreTests.ReserveAsync_SameKeyFromAnotherUserOrOrganization_IsNewAndNeverReturnsTheFirstAnswer` | `tests/RestaurantManagement.Shared.Tests/Idempotency/IdempotencyKeyStoreTests.cs` |
| 3 | T1 | Edge | A key past 8 days, or running for over 120 seconds, is taken over; before that it still counts | `IdempotencyKeyStoreTests.ReserveAsync_KeyOlderThanRetentionOrAbandoned_IsTakenOver` | `tests/RestaurantManagement.Shared.Tests/Idempotency/IdempotencyKeyStoreTests.cs` |
| 4 | T1 | Edge | Purge removes only expired keys | `IdempotencyKeyStoreTests.PurgeExpiredAsync_OldAndFreshKeys_RemovesOnlyTheExpiredOnes` | `tests/RestaurantManagement.Shared.Tests/Idempotency/IdempotencyKeyStoreTests.cs` |
| 5 | T1 | Happy | A new key is saved at once as in progress with only the request hash | `IdempotencyKeyStoreTests.ReserveAsync_NewKey_SavesItInProgressAtOnceWithOnlyTheRequestHash` | `tests/RestaurantManagement.Shared.Tests/Idempotency/IdempotencyKeyStoreTests.cs` |
| 6 | T1 | Happy | A finished key replays its saved status, content type, body and Location | `IdempotencyKeyStoreTests.ReserveAsync_KeyCompletedWithTheSameRequest_ReplaysTheSavedAnswer` | `tests/RestaurantManagement.Shared.Tests/Idempotency/IdempotencyKeyStoreTests.cs` |
| 7 | T1 | Failure | On PostgreSQL the same user, organization and key cannot be stored twice, also with no organization | `IdempotencyKeyPostgresTests.SaveChanges_SameUserOrganizationAndKeyTwice_IsRefusedByTheDatabaseAlsoWithNoOrganization` | `tests/RestaurantManagement.Web.Tests/Shared/IdempotencyKeyPostgresTests.cs` |
| 8 | T1 | Edge | Five simultaneous requests with one key: one runs, the rest are told it is in progress | `IdempotencyKeyPostgresTests.ReserveAsync_FiveRequestsAtTheSameMoment_OneRunsAndTheOthersAreInProgress` | `tests/RestaurantManagement.Web.Tests/Shared/IdempotencyKeyPostgresTests.cs` |
| 9 | T1 | Happy | Unconfigured settings keep keys 8 days, 120 seconds, purge on every 60 minutes | `IdempotencyOptionsValidatorTests.Options_NothingConfigured_KeepsKeysEightDaysAndAcceptsThem` | `tests/RestaurantManagement.Shared.Tests/Idempotency/IdempotencyOptionsValidatorTests.cs` |
| 10 | T1 | Failure | A setting that is not positive is refused and named | `IdempotencyOptionsValidatorTests.Validate_SettingNotPositive_FailsNamingTheSetting` | `tests/RestaurantManagement.Shared.Tests/Idempotency/IdempotencyOptionsValidatorTests.cs` |
| 11 | T1 | Happy | The purge job deletes expired keys from every registered store | `IdempotencyKeyCleanupWorkerTests.StartAsync_ExpiredKeysInTwoStores_DeletesThemFromBothAndKeepsFreshOnes` | `tests/RestaurantManagement.Shared.Tests/Idempotency/IdempotencyKeyCleanupWorkerTests.cs` |
| 12 | T1 | Edge | A change in method, path or query, location or body gives another hash; the hash holds no body | `IdempotencyRequestHashTests.Compute_AnyPartOfTheRequestChanges_GivesAnotherHash` | `tests/RestaurantManagement.Shared.Tests/Idempotency/IdempotencyRequestHashTests.cs` |
| 13 | T1 | Happy | The three refusal titles read as real text in English, Portuguese and French | `IdempotencyMessageTests.Localizer_IdempotencyTitle_ReturnsRealTextInEachLanguage` | `tests/RestaurantManagement.Shared.Tests/Localization/IdempotencyMessageTests.cs` |
| 14 | T2 | Happy | A service with no open transaction owns one and its commit saves the work | `JoinableTransactionTests.BeginOrJoinAsync_NoOpenTransaction_OwnsItAndCommitSavesTheWork` | `tests/RestaurantManagement.Shared.Tests/Idempotency/JoinableTransactionTests.cs` |
| 15 | T2 | Edge | A service joining an open transaction saves nothing until the opener commits | `JoinableTransactionTests.CommitAsync_JoinedTransaction_SavesNothingUntilTheOpenerCommits` | `tests/RestaurantManagement.Shared.Tests/Idempotency/JoinableTransactionTests.cs` |
| 16 | T2 | Failure | The opener's rollback discards the joined work | `JoinableTransactionTests.RollbackAsync_OpenerRollsBackAfterAJoinedCommit_DiscardsTheJoinedWork` | `tests/RestaurantManagement.Shared.Tests/Idempotency/JoinableTransactionTests.cs` |
| 17 | T2 | Failure | An undo step runs exactly once after a rollback or a failed commit | `JoinableTransactionTests.UndoStep_TransactionEndsWithoutACommit_RunsExactlyOnce` | `tests/RestaurantManagement.Shared.Tests/Idempotency/JoinableTransactionTests.cs` |
| 18 | T2 | Edge | An undo step never runs after a successful commit | `JoinableTransactionTests.UndoStep_TransactionCommits_NeverRuns` | `tests/RestaurantManagement.Shared.Tests/Idempotency/JoinableTransactionTests.cs` |
| 19 | T2 | Edge | An undo step registered by a joined handle runs once when the opener rolls back | `JoinableTransactionTests.UndoStep_RegisteredByAJoinedHandle_RunsOnceWhenTheOpenerRollsBack` | `tests/RestaurantManagement.Shared.Tests/Idempotency/JoinableTransactionTests.cs` |
| 20 | T3 | Happy | A menu create sent twice with one key runs once and answers the same both times | `IdempotencyKeyEndpointTests.CreateMenu_SameKeyAndSameRequestTwice_RunsOnceAndAnswersTheSameBothTimes` | `tests/RestaurantManagement.Web.Tests/Shared/IdempotencyKeyEndpointTests.cs` |
| 21 | T3 | Failure | The same key with another body or target is refused with 422 and changes nothing | `IdempotencyKeyEndpointTests.CreateMenu_SameKeyWithAnotherBodyOrTarget_ReturnsUnprocessableAndChangesNothing` | `tests/RestaurantManagement.Web.Tests/Shared/IdempotencyKeyEndpointTests.cs` |
| 22 | T3 | Failure | A key whose first request is still running is refused with 409 and nothing runs | `IdempotencyKeyEndpointTests.CreateMenu_SameKeyWhileTheFirstRequestIsStillRunning_ReturnsConflictAndRunsNothing` | `tests/RestaurantManagement.Web.Tests/Shared/IdempotencyKeyEndpointTests.cs` |
| 23 | T3 | Guard | The same key from another user or organization runs their own write and never shows the first answer | `IdempotencyKeyEndpointTests.CreateMenu_SameKeyFromAnotherUserOrOrganization_RunsTheirOwnWriteAndNeverReturnsTheFirstAnswer` | `tests/RestaurantManagement.Web.Tests/Shared/IdempotencyKeyEndpointTests.cs` |
| 24 | T3 | Failure | A key that is not a UUID is refused with 400 and nothing runs | `IdempotencyKeyEndpointTests.CreateMenu_KeyThatIsNotAUuid_ReturnsBadRequestAndRunsNothing` | `tests/RestaurantManagement.Web.Tests/Shared/IdempotencyKeyEndpointTests.cs` |
| 25 | T3 | Guard | A failed first attempt saves nothing, so a corrected request with the same key runs | `IdempotencyKeyEndpointTests.CreateMenu_FirstAttemptFailsValidation_CorrectedRequestWithTheSameKeyRuns` | `tests/RestaurantManagement.Web.Tests/Shared/IdempotencyKeyEndpointTests.cs` |
| 26 | T3 | Failure | If saving the key fails, the menu is not saved either | `IdempotencyKeyEndpointTests.CreateMenu_FailureWhileSavingTheKey_StoresNoMenuEither` | `tests/RestaurantManagement.Web.Tests/Shared/IdempotencyKeyEndpointTests.cs` |
| 27 | T3 | Edge | A key older than 8 days runs as new and its old row is replaced | `IdempotencyKeyEndpointTests.CreateMenu_KeyOlderThanTheRetentionTime_RunsAsNewAndReplacesTheOldRow` | `tests/RestaurantManagement.Web.Tests/Shared/IdempotencyKeyEndpointTests.cs` |
| 28 | T3 | Guard | An anonymous write, a read, and a write with no key are answered as today | `IdempotencyKeyEndpointTests.Request_AnonymousWriteReadOrWriteWithNoKey_RunsAsToday` | `tests/RestaurantManagement.Web.Tests/Shared/IdempotencyKeyEndpointTests.cs` |
| 29 | T4 | Happy | The API document shows an optional uuid Idempotency-Key header on every signed-in write | `IdempotencyKeyWiringTests.V1Document_SignedInWrites_DeclareTheOptionalIdempotencyKeyHeaderAsUuid` | `tests/RestaurantManagement.Web.Tests/Web/IdempotencyKeyWiringTests.cs` |
| 30 | T4 | Guard | The header is not shown on public writes, the three session actions or reads | `IdempotencyKeyWiringTests.V1Document_PublicWritesSessionActionsAndReads_DoNotDeclareTheIdempotencyKeyHeader` | `tests/RestaurantManagement.Web.Tests/Web/IdempotencyKeyWiringTests.cs` |
| 31 | T4 | Failure | Startup refuses a retention that is not positive and names it | `IdempotencyKeyWiringTests.Startup_NonPositiveIdempotencyRetentionDays_FailsNamingIdempotencyRetentionDays` | `tests/RestaurantManagement.Web.Tests/Web/IdempotencyKeyWiringTests.cs` |
| 32 | T4 | Edge | Every signed-in write belongs to a module with a key store or is marked to ignore keys (green after B3-B5) | `IdempotencyKeyWiringTests.EveryEndpoint_SignedInWrite_BelongsToAModuleWithAKeyStoreOrIgnoresKeys` | `tests/RestaurantManagement.Web.Tests/Web/IdempotencyKeyWiringTests.cs` |
| 33 | T5 | Security | A member without Menus Manage who sends a key gets 403 and no key is stored | `MenuIdempotencyEndpointTests.CreateMenu_MemberWithoutMenusManageSendsAKey_ReturnsForbiddenAndStoresNoKey` | `tests/RestaurantManagement.Web.Tests/Menu/MenuIdempotencyEndpointTests.cs` |
| 34 | T5 | Guard | A reused menu ID with a key still answers id.already_used and releases the key | `MenuIdempotencyEndpointTests.CreateMenu_ReusedMenuIdWithAKey_ReturnsConflictAndReleasesTheKey` | `tests/RestaurantManagement.Web.Tests/Menu/MenuIdempotencyEndpointTests.cs` |
| 35 | T5 | Happy | A category create sent twice with one key makes one category and replays 201 with Location | `MenuIdempotencyEndpointTests.CreateMenuCategory_SameKeyTwice_CreatesOneCategoryAndReplaysCreatedWithLocation` | `tests/RestaurantManagement.Web.Tests/Menu/MenuIdempotencyEndpointTests.cs` |
| 36 | T6 | Failure | Create organization with a key whose save fails leaves no organization and no Owner access | `OrganizationIdempotencyEndpointTests.CreateOrganization_FailureWhileSavingTheKey_LeavesNoOrganizationAndNoOwnerAccess` | `tests/RestaurantManagement.Web.Tests/Organization/OrganizationIdempotencyEndpointTests.cs` |
| 37 | T6 | Happy | Create organization twice with one key and no organization in the token makes one and replays the answer | `OrganizationIdempotencyEndpointTests.CreateOrganization_SameKeyTwiceWithNoOrganizationInTheToken_CreatesOneAndReplaysTheAnswer` | `tests/RestaurantManagement.Web.Tests/Organization/OrganizationIdempotencyEndpointTests.cs` |
| 38 | T6 | Happy | Create location twice with one key makes one location and identical answers | `OrganizationIdempotencyEndpointTests.CreateLocation_SameKeyTwice_CreatesOneLocationAndAnswersTheSameBothTimes` | `tests/RestaurantManagement.Web.Tests/Organization/OrganizationIdempotencyEndpointTests.cs` |
| 39 | T7 | Edge | logout-all sent twice with one key runs both times and stores no key | `IdentityIdempotencyEndpointTests.LogoutAll_SameKeyTwice_RunsBothTimesAndStoresNoKey` | `tests/RestaurantManagement.Web.Tests/Identity/IdentityIdempotencyEndpointTests.cs` |
| 40 | T7 | Happy | Invitation create sent twice with one key makes one invitation, sends one email, answers the same | `IdentityIdempotencyEndpointTests.CreateInvitation_SameKeyTwice_StoresOneInvitationSendsOneEmailAndAnswersTheSame` | `tests/RestaurantManagement.Web.Tests/Identity/IdentityIdempotencyEndpointTests.cs` |

## Existing tests updated
- `ErrorCodeCatalogTests.Read_SharedCatalog_ListsEveryNamedCodeWithItsStatus` (SH52): two rows added for `idempotency_key.invalid` (400) and `idempotency_key.reused` (422). They fail until T1 adds the codes.

## Spec Outcome coverage
| Outcome | Scenarios |
|---|---|
| #1 Repeated write with the same key runs once | 6, 20, 35, 37, 38, 40 |
| #2 Same key, different request is refused | 1, 12, 21 |
| #3 Concurrent duplicate is refused | 8, 22 |
| #4 No leak between users or organizations | 2, 23 |
| #5 Writes without a key are unchanged | 28, 30 (and every existing write test, which sends no key) |
| #6 Invalid key is rejected | 24 |
| #7 Failed first attempts can be retried | 25, 34 |
| #8 Write and key are saved together | 14-19, 26, 36 |
| #9 Keys expire and are purged | 3, 4, 11, 27 |
| #10 Works on every module's writes | 29, 32, 33, 35, 36, 37, 38, 39, 40 |

## Read closely before approving
- Scenarios 23 and 33: cross-tenant and 403 are proven once, through Menu. Organization and Identity are not repeated.
- Scenario 26 (SH105): the PostgreSQL trigger forces "completing the key must UPDATE the reserved row". An implementation that inserts the row at completion would pass or fail wrongly. This fixes the design.
- Scenarios 8 and 22: Outcome 3 (two simultaneous requests over HTTP) is tested only at store level plus a seeded in-progress row. There is no real concurrent HTTP test.
- Scenario 39 proves only that `logout-all` ignores keys; `select-organization` and `change-password` are covered only by the marker check in scenario 32.

## Assumptions
- The filter is an MVC resource filter (`IAsyncResourceFilter`) in Shared, because only the matched action tells which module's context holds the key; the stub fixes the names `IdempotencyKeyFilter`, `JoinableTransaction`, `IdempotencyKeyStore`, `IdempotencyStoreRegistration` and `AddIdempotencyKeyStore<TContext>(Assembly)`.
- Budget (L: at most 40) forced one test per behaviour: the filter is tested end to end through Menu create (scenarios 20-28), not again per module. Because of that, cross-tenant and "member without access gets 403" are proven for Menu only (scenarios 23, 33); T6 and T7 DoD lines for the same two cases are covered by that one generic proof, not repeated.
- Scenarios 23, 25, 28, 30 and 34 are marked Guard: they pass today because the behaviour is already right without a filter; they protect against a leaking or over-eager filter.
- "Saving the key fails" is forced with a PostgreSQL trigger that rejects updates on the module's key table (`menu_idempotency_keys`, `organization_idempotency_keys`). The key row is therefore assumed to be completed with an UPDATE of the reserved row, and those two table names are fixed.
- Several end-to-end tests (scenarios 20-22, 26, 27, 33, 35-40) fail first with a missing table or missing `IdempotencyKey` mapping on the module context, until T5/T6/T7 add the mapping and migration.
- The in-progress end-to-end test seeds a running key using `IdempotencyRequestHash.Compute` (method, path with query, `X-Location-Id`, raw body); true simultaneity is covered at store level (scenario 8).
- The refusal title texts in `IdempotencyMessageTests` are proposed by the tester.
- Decision (user, Gate 2): cross-tenant and 403 with a key are proven for Menu only (scenarios 23 and 33); Organization and Identity are not repeated.
- Decision (user, Gate 2): the en/pt/fr refusal titles in `IdempotencyMessageTests` are accepted as written.
