# Scenarios: Record Client Action Time

**Plan:** docs/workflow/shared/2026-10-30-record-client-action-time/plan.md
**Coverage:** 41 scenarios — 16 happy · 9 edge · 13 failure · 1 security · 2 guard

Test folders: `S` = `tests/RestaurantManagement.Shared.Tests/`, `O` = `tests/RestaurantManagement.Modules.Organization.Tests/`, `W` = `tests/RestaurantManagement.Web.Tests/`.

| # | Task | Type | Scenario (plain words) | Test name | Test file |
|---|---|---|---|---|---|
| 1 | T1 | Failure | A missing action time or a missing device clock is refused with 400 `client_time.required` | `Validate_ActionTimeOrSentAtMissing_Throws400Required` | `S/ClientTimeValidatorTests.cs` |
| 2 | T1 | Failure | Garbage, a time without an offset, a date only, or an over-long value is refused with 400 `client_time.invalid` | `Validate_ValueUnreadableWithoutOffsetOrTooLong_Throws400Invalid` | `S/ClientTimeValidatorTests.cs` |
| 3 | T1 | Failure | A header sent twice (two values) is refused with 400 `client_time.invalid` | `Validate_HeaderSentWithSeveralValues_Throws400Invalid` | `S/ClientTimeValidatorTests.cs` |
| 4 | T1 | Failure | A corrected time more than 5 minutes ahead, or older than 7 days, is refused with 422 | `Validate_CorrectedTimeBeyondTheBounds_Throws422` | `S/ClientTimeValidatorTests.cs` |
| 5 | T1 | Edge | Exactly 5 minutes ahead and exactly 7 days old are still accepted (4 minutes and 7 days minus 1 minute too) | `Validate_CorrectedTimeInsideTheBounds_IsAccepted` | `S/ClientTimeValidatorTests.cs` |
| 6 | T1 | Happy | An action made 5 hours ago on a right clock keeps its time, and the received time is recorded | `Validate_ActionFiveHoursOld_KeepsTheActionTimeAndRecordsTheReceivedTime` | `S/ClientTimeValidatorTests.cs` |
| 7 | T1 | Happy | A device clock 10 minutes fast (or slow) gives a clock error of +10 (or -10) minutes and shifts the action time by it | `Validate_DeviceClockOffByTenMinutes_ShiftsTheActionTimeByTheError` | `S/ClientTimeValidatorTests.cs` |
| 8 | T1 | Edge | An action made just now on a clock 10 minutes fast looks 10 minutes ahead but is accepted, because the bounds use the corrected time | `Validate_FastClockMakesTheRawTimeLookTenMinutesAhead_UsesTheCorrectedTimeForTheBounds` | `S/ClientTimeValidatorTests.cs` |
| 9 | T1 | Edge | Times with `Z`, `-03:00` and `+05:30` become the same universal moment and keep their offset in minutes | `Validate_ActionTimeWithAnOffset_StoresTheInstantInUtcAndTheOffsetInMinutes` | `S/ClientTimeValidatorTests.cs` |
| 10 | T2 | Happy | A new record gets the device action time, offset, corrected time, clock error and received time | `SaveChangesAsync_NewRowWithAcceptedTimes_StampsAllFiveTimes` | `S/ClientTimeSaveChangesInterceptorTests.cs` |
| 11 | T2 | Happy | A changed record gets the five times refreshed | `SaveChangesAsync_ChangedRowWithAcceptedTimes_RefreshesTheTimes` | `S/ClientTimeSaveChangesInterceptorTests.cs` |
| 12 | T2 | Edge | A save with no signed-in write (registration, background job) stamps nothing | `SaveChangesAsync_NoAcceptedTimes_StampsNothing` | `S/ClientTimeSaveChangesInterceptorTests.cs` |
| 13 | T2 | Happy | A new record tied to a location gets the business day the Organization contract answers for the corrected time | `SaveChangesAsync_NewLocationRow_GetsTheBusinessDayOfTheCorrectedTime` | `S/ClientTimeSaveChangesInterceptorTests.cs` |
| 14 | T2 | Edge | Editing a location record never changes its business day | `SaveChangesAsync_EditedLocationRow_KeepsItsBusinessDay` | `S/ClientTimeSaveChangesInterceptorTests.cs` |
| 15 | T2 | Failure | When the contract does not know the location, the business day stays empty and the times are still stamped | `SaveChangesAsync_ContractKnowsNoSuchLocation_LeavesTheBusinessDayEmpty` | `S/ClientTimeSaveChangesInterceptorTests.cs` |
| 16 | T2 | Edge | A record not tied to a location saves with no business day and without needing the contract (Organization's own context carries the interceptor) | `SaveChangesAsync_RowNotTiedToALocationAndNoContractRegistered_SavesWithoutResolvingTheContract` | `S/ClientTimeSaveChangesInterceptorTests.cs` |
| 17 | T2 | Failure | A location record saved with a synchronous save fails loudly instead of skipping the business day | `SaveChanges_NewLocationRowOnASynchronousSave_FailsInsteadOfSkippingTheBusinessDay` | `S/ClientTimeSaveChangesInterceptorTests.cs` |
| 18 | T1 | Failure | A signed-in write (POST on a menu, PUT on an organization edit) without either header is refused with 400 and nothing is stored | `CreateMenu_ClientTimeHeaderMissing_ReturnsBadRequestAndRunsNothing` | `W/Shared/ClientTimeEndpointTests.cs` |
| 19 | T1 | Failure | A signed-in write with an unreadable time or a time with no offset is refused with 400 and nothing is stored | `CreateMenu_ClientTimeUnreadableOrWithoutOffset_ReturnsBadRequestAndRunsNothing` | `W/Shared/ClientTimeEndpointTests.cs` |
| 20 | T1 | Failure | A write 6 minutes ahead, or 7 days and 1 minute old, is refused with 422 and nothing is stored | `CreateMenu_CorrectedTimeOutsideTheBounds_ReturnsUnprocessableAndStoresNothing` | `W/Shared/ClientTimeEndpointTests.cs` |
| 21 | T1 | Edge | A write 4 minutes ahead, or just under 7 days old, is accepted and stored | `CreateMenu_CorrectedTimeJustInsideTheBounds_IsAccepted` | `W/Shared/ClientTimeEndpointTests.cs` |
| 22 | T1 | Guard | An anonymous write (login) and a read, sent with no device times, are answered as before | `Request_AnonymousWriteOrRead_IsAnsweredWithoutDeviceTimes` | `W/Shared/ClientTimeEndpointTests.cs` |
| 23 | T1 | Failure | A write refused for its time frees its idempotency key, so the corrected request with the same key runs | `CreateMenu_RefusedForItsTimeThenCorrectedWithTheSameKey_RunsTheCorrectedRequest` | `W/Shared/ClientTimeEndpointTests.cs` |
| 24 | T1 | Edge | A replay with the same key and unreadable times still gets the first answer (key lookup runs before the time check) | `CreateMenu_ReplayedWithTheSameKeyAndUnreadableTimes_GetsTheFirstAnswerUntouched` | `W/Shared/ClientTimeEndpointTests.cs` |
| 25 | T2 | Happy | A menu created with an action 5 hours old stores the corrected action time and a later received time | `CreateMenu_ActionFiveHoursOld_StoresTheActionTimeAndALaterReceivedTime` | `W/Shared/ClientTimeStampEndpointTests.cs` |
| 26 | T2 | Happy | A menu created from a clock 10 minutes fast stores the corrected time 10 minutes earlier and the error | `CreateMenu_DeviceClockTenMinutesFast_StoresTheCorrectedTimeAndTheError` | `W/Shared/ClientTimeStampEndpointTests.cs` |
| 27 | T2 | Edge | The same idempotency key sent again later with other times leaves the stored times unchanged | `CreateMenu_SameKeyReplayedLaterWithOtherTimes_LeavesTheStoredTimesUnchanged` | `W/Shared/ClientTimeStampEndpointTests.cs` |
| 28 | T2 | Happy | The audit log entry written by an organization edit carries the times of the write | `EditOrganization_AuditEntryWritten_CarriesTheTimesOfTheWrite` | `W/Shared/ClientTimeStampEndpointTests.cs` |
| 29 | T2 | Happy | Identity, Menu, Organization and audit log contexts all carry the client-time interceptor | `DbContext_Resolved_HasTheClientTimeInterceptorAttached` | `W/Shared/ClientTimeInterceptorWiringTests.cs` |
| 30 | T3 | Happy | The business-day contract is answered by the Organization implementation | `ILocationBusinessDay_Resolved_IsTheOrganizationImplementation` | `W/Shared/ClientTimeInterceptorWiringTests.cs` |
| 31 | T1 | Happy | The accepted times are held per request and never shared between requests | `IClientTimeContext_ResolvedFromTwoRequestScopes_IsADifferentInstanceEachTime` | `W/Shared/ClientTimeInterceptorWiringTests.cs` |
| 32 | T3 | Happy | An action at 23:30 local that is already the next day in universal time keeps the local date, for Sao Paulo and Toronto; before the organization's cutoff (04:00 default, 05:30 for Trattoria) it is the day before | `GetBusinessDayAsync_LocationOfTheTokenOrganization_ReturnsTheLocalBusinessDay` | `O/LocationBusinessDayTests.cs` |
| 33 | T3 | Failure | A location that does not exist has no business day (null) | `GetBusinessDayAsync_UnknownLocation_ReturnsNull` | `O/LocationBusinessDayTests.cs` |
| 34 | T3 | Security | Another organization's location is answered as not found (null) | `GetBusinessDayAsync_LocationOfAnotherOrganization_ReturnsNullAsIfItDidNotExist` | `O/LocationBusinessDayTests.cs` |
| 35 | T4 | Happy | An organization edit with a time 2 hours old stores the times on the organization row | `EditOrganization_SignedInWriteTwoHoursOld_StoresTheTimesOnTheChangedRow` | `W/Organization/OrganizationClientTimeEndpointTests.cs` |
| 36 | T5 | Failure | A session action (choose organization, logout-all, change password) without the device-time headers is refused with 400 | `SessionAction_NoDeviceTimeHeaders_ReturnsBadRequest` | `W/Identity/RoleClientTimeEndpointTests.cs` |
| 37 | T5 | Happy | A role created with a time 3 hours old stores the times on the new role row | `CreateRole_SignedInWriteThreeHoursOld_StoresTheTimesOnTheNewRow` | `W/Identity/RoleClientTimeEndpointTests.cs` |
| 38 | T6 | Happy | A menu category created with a time 4 hours old stores the times on the new category row | `CreateMenuCategory_SignedInWriteFourHoursOld_StoresTheTimesOnTheNewRow` | `W/Menu/MenuCategoryClientTimeEndpointTests.cs` |
| 39 | T7 | Happy | Every signed-in write in the OpenAPI document declares both headers as required date-times with a length limit and a purpose | `V1Document_SignedInWrites_DeclareBothDeviceTimeHeadersAsRequiredDateTimes` | `W/Web/ClientTimeHeadersOpenApiTests.cs` |
| 40 | T7 | Guard | Reads and anonymous writes do not declare the two headers | `V1Document_PublicWritesAndReads_DoNotDeclareTheDeviceTimeHeaders` | `W/Web/ClientTimeHeadersOpenApiTests.cs` |
| 41 | T1 | Failure | Both headers readable but far apart (year 9999 against year 0001) are refused with 422, not a server error | `Validate_BothHeadersReadableButFarApart_Throws422InsteadOfOverflowing` | `S/ClientTimeValidatorTests.cs` |

## Existing tests updated
- `ErrorCodeCatalogTests.Read_SharedCatalog_ListsEveryNamedCodeWithItsStatus` (rule SH52): four new rows (`client_time.required` 400, `client_time.invalid` 400, `client_time.in_future` 422, `client_time.too_old` 422). Fails until T1 adds the codes.
- `AuthApiFactory.ConfigureClient` (test helper, used by `TenantApiFactory`): every client sends both headers with the current time by default, as T1 requires. No existing test changes behaviour today; it keeps them green once T1 is built.

## Spec Outcome coverage
| Outcome | Scenarios |
|---|---|
| #1 Action time and received time are both stored | 6, 10, 25 |
| #2 Clock error is corrected | 7, 8, 26 |
| #3 Business day uses the location time zone | 13, 14, 32 (Sao Paulo and Toronto), 34 |
| #4 Future times are refused | 4, 5, 20, 21 |
| #5 Too-old times are refused | 4, 5, 20, 21 |
| #6 Missing or invalid times are refused | 1, 2, 3, 18, 19, 36, 39 |
| #7 Replay keeps the original times | 23, 24, 27 |
| #8 Works on every module's writes | 29, 35, 37, 38 |

## Read closely before approving
- #17: the sync save throws `InvalidOperationException`. The spec says only "fails loudly", so this is a decision made by the tester.
- #4, #5, #20, #21: the bounds are inclusive and use the corrected time. The spec says "more than" and "older than", and the tests pin the exact 5-minute and 7-day edges.
- #23, #24, #27: the key lookup runs before the time check, a refusal frees the key, and a replay keeps the stored times. #24 and #27 read the row back to prove the replay rule.

## Assumptions
- SH124 is intentionally unused in the matrix (a scenario dropped for the budget); wiring checks #29-#31 are accepted as wiring, not behaviour.
- Names chosen by the tester for the stubs: `ClientTimeHeaders`, `ClientTimes`, `IClientTimeContext` (`Times`, `Set`), `ClientTimeValidator.Validate(actionTime, sentAt, receivedAt)`, `ClientTimeSaveChangesInterceptor(IClientTimeContext, IServiceProvider)`, `ILocationBusinessDay.GetBusinessDayAsync(locationId, utcMoment)`, `LocationBusinessDay`. Column names on `BaseEntity`: `ClientActionTime`, `ClientActionOffsetMinutes`, `CorrectedActionTime`, `ClockError` (interval), `ReceivedAt`; on `LocationEntity`: `BusinessDay` (date).
- The new entity properties are stubbed with `[NotMapped]` so the existing "no pending changes" tests (SHD9 to SHD11, SHD18) stay green until the migrations exist. T2 removes the attribute and adds the audit log migration; T4 to T6 add the module migrations.
- Length limit of each header: 64 characters (a full RFC 3339 value with 7 fractional digits is 33). Accepted by the user.
- Scenario #17: a synchronous save with a new location record throws `InvalidOperationException`. Accepted by the user.
- New test helpers, flagged: `tests/RestaurantManagement.Web.Tests/ClientTimeWebSupport.cs` (builds requests with their own headers); `ClientTimeTestDbContext.cs` in Shared.Tests (SQLite context and entities for the interceptor, in the style of `AuditTestDbContext`). No new fixture or test project.
- Endpoint tests use the real clock with tolerances of one minute; bounds are tested a minute inside or outside so latency cannot flip a result.
- PostgreSQL tests were run once with Docker up: all failures are assertion failures on the missing behaviour (status code, missing stamped column, missing wiring), none is a setup error. Passing today: guards #22 and #40 only; #21 and #24 now also read the stored times back, so they fail until T2.
- Scenarios #12 and #16 are Edge, not Guard: they exercise the new interceptor, which is a stub, so they fail until it is built.
- No role rejection tests: the spec changes no access rule (plan DoD). Cross-tenant: only the new Organization contract (#34); the existing tenant tests cover the rest.
- Budget: 40 scenarios (the L ceiling); SH124 (plain row, synchronous save stamps) was dropped to stay inside it.
