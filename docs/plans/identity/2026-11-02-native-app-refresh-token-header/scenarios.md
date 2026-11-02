# Scenarios: Native App Refresh Token in a Header

**Plan:** docs/plans/identity/2026-11-02-native-app-refresh-token-header/plan.md
**Coverage:** 27 scenarios — 10 happy · 3 edge · 7 failure · 1 security · 6 guard

Test folders: `I.Tests` = `tests/RestaurantManagement.Modules.Identity.Tests/`; `W/Identity` = `tests/RestaurantManagement.Web.Tests/Identity/`; `W/Web` = `tests/RestaurantManagement.Web.Tests/Web/`.
Guard = already correct today and must stay so (the test passes before the change).

| # | Task | Type | Scenario (plain words) | Test name | Test file |
|---|---|---|---|---|---|
| 1 | T1 | Failure | A native origin setting that is a web origin, `http://localhost`, has a port, other letter case, a trailing slash or is empty makes the app refuse to start, naming the setting | `Validate_OriginIsNotOneOfTheTwoNativeOrigins_FailsNamingTheSetting` | `I.Tests/NativeAppOptionsValidatorTests.cs` |
| 2 | T1 | Happy | Each of the two native origins is accepted | `Validate_EachNativeOrigin_Succeeds` | `I.Tests/NativeAppOptionsValidatorTests.cs` |
| 3 | T1 | Edge | An empty origin list is accepted and turns native mode off | `Validate_EmptyList_SucceedsAndTurnsNativeModeOff` | `I.Tests/NativeAppOptionsValidatorTests.cs` |
| 4 | T1 | Failure | The whole app refuses to start when a web origin is configured as native, naming `NativeApp:Origins` | `Startup_NativeAppOriginIsAWebOrigin_FailsNamingNativeAppOrigins` | `W/Web/StartupConfigurationTests.cs` |
| 5 | T1 | Happy | The shipped settings file holds exactly the two native origins | `Configuration_ShippedNativeAppOrigins_AreExactlyTheTwoNativeOrigins` | `W/Identity/ShippedNativeOriginsTests.cs` |
| 6 | T2 | Guard | Login from any web origin, or no origin, even with the native header: cookie set, no refresh token in the body | `Login_WebOrigin_SetsCookieAndBodyHasNoRefreshToken` | `W/Identity/NativeAppCarrierEndpointTests.cs` |
| 7 | T2 | Guard | Web refresh ignores the native header, reads the cookie, and the body has no refresh token fields | `Refresh_WebOrigin_IgnoresTheHeaderReadsTheCookieAndBodyHasNoRefreshToken` | `W/Identity/NativeAppCarrierEndpointTests.cs` |
| 8 | T2 | Failure | Native refresh with only a cookie reads no token and is refused | `Refresh_NativeOriginWithOnlyACookie_ReadsNoTokenAndIsRefused` | `W/Identity/NativeAppCarrierEndpointTests.cs` |
| 9 | T2 | Happy | Native login (either native origin) returns the refresh token and its expiry in the body and sets no cookie | `Login_NativeOrigin_ReturnsRefreshTokenInBodyAndSetsNoCookie` | `W/Identity/NativeAppCarrierEndpointTests.cs` |
| 10 | T2 | Edge | Native login ignores a cookie sent with it (no previous token is passed on) | `Login_NativeOriginSendingACookie_PassesNoPreviousTokenToSession` | `W/Identity/NativeAppCarrierEndpointTests.cs` |
| 11 | T2 | Happy | Native refresh reads the header token (not the cookie) and returns the replacement in the body with no cookie | `Refresh_NativeOriginWithHeader_SendsHeaderTokenToSessionAndReturnsReplacementInBody` | `W/Identity/NativeAppCarrierEndpointTests.cs` |
| 12 | T2, T3 | Guard | Native refresh and native logout over the rate limit get 429, as on the web | `NativeRefreshOrLogout_LimitExceeded_ReturnsTooManyRequests` | `W/Identity/NativeAppCarrierEndpointTests.cs` |
| 13 | T2 | Failure | A rotated header token sent again is refused, its whole device family is revoked, and the user's other device still refreshes | `Refresh_NativeRotatedTokenSentAgain_IsRefusedAndRevokesTheFamilyButNotOtherDevices` | `W/Identity/NativeAppSessionEndpointTests.cs` |
| 14 | T2 | Failure | A missing or unknown header token gives 401 `session.invalid_or_expired` and issues nothing, even with a valid cookie sent | `Refresh_NativeHeaderMissingOrUnknown_ReturnsUnauthorizedAndIssuesNothing` | `W/Identity/NativeAppSessionEndpointTests.cs` |
| 15 | T2 | Failure | (a valid token of the same user refreshes first as a control) An expired, 7-day idle or past-30-day header token gives the same 401 and issues nothing | `Refresh_NativeTokenExpiredIdleOrPastThirtyDays_ReturnsUnauthorizedAndIssuesNothing` | `W/Identity/NativeAppSessionEndpointTests.cs` |
| 16 | T2 | Security | (the user's own token works first as a control) Choosing an organization with another user's header token gives 401 and leaves their token active | `SelectOrganization_NativeHeaderHoldsAnotherUsersToken_ReturnsUnauthorizedAndLeavesTheirTokenActive` | `W/Identity/NativeAppSessionEndpointTests.cs` |
| 17 | T2 | Guard | Choosing an organization of another tenant, or an unknown one, gives the same 403 `session.not_a_member` | `SelectOrganization_NativeOrganizationOfAnotherTenantOrUnknown_ReturnsTheSameForbidden` | `W/Identity/NativeAppSessionEndpointTests.cs` |
| 18 | T2 | Happy | Native refresh rotates: new token in the body, old one revoked as rotated, same family, 7-day expiry | `Refresh_NativeHeaderToken_RotatesInTheSameFamilyAndReturnsReplacementInBody` | `W/Identity/NativeAppSessionEndpointTests.cs` |
| 19 | T2 | Happy | Native choose organization rotates the header token (replacement in the body, no cookie) and the old token is refused afterwards | `SelectOrganization_NativeHeaderToken_RotatesTheTokenAndRefusesTheOldOne` | `W/Identity/NativeAppSessionEndpointTests.cs` |
| 20 | T2, T3 | Guard | Native choose organization, logout-all and change password without the two device-time headers get 400 `client_time.required` | `NativeSessionAction_NoDeviceTimeHeaders_ReturnsBadRequest` | `W/Identity/NativeAppSessionEndpointTests.cs` |
| 21 | T3 | Happy | Native logout revokes only that device's token (as logout), answers 204, sets no cookie; the user's other device still refreshes and another user's token stays active | `Logout_NativeHeaderToken_RevokesOnlyThatDeviceAndSetsNoCookie` | `W/Identity/NativeAppSessionEndpointTests.cs` |
| 22 | T3 | Failure | Native logout with a reused token revokes that device's family | `Logout_NativeRotatedTokenSentAgain_RevokesThatDevicesFamily` | `W/Identity/NativeAppSessionEndpointTests.cs` |
| 23 | T3 | Edge | Native logout with no header or an unknown token answers 204, changes nothing and does not read a sent cookie | `Logout_NativeHeaderMissingOrUnknown_ReturnsNoContentAndChangesNothing` | `W/Identity/NativeAppSessionEndpointTests.cs` |
| 24 | T3 | Happy | Native logout-all revokes every token of the caller, sets no cookie; earlier header tokens are refused | `LogoutAll_NativeSignedIn_RevokesEveryTokenOfTheCallerAndSetsNoCookie` | `W/Identity/NativeAppSessionEndpointTests.cs` |
| 25 | T3 | Happy | Native change password revokes every session, sets no cookie; earlier header tokens are refused | `ChangePassword_NativeCorrectCurrentPassword_RefusesEarlierHeaderTokensAndSetsNoCookie` | `W/Identity/NativeAppSessionEndpointTests.cs` |
| 26 | T4 | Happy | A Sentry event whose request carries `X-Refresh-Token` (any letter case) leaves without the token value and keeps other headers | `SentryEvent_RequestCarriesRefreshTokenHeader_LeavesWithoutTheTokenAndKeepsOtherHeaders` | `W/Web/RefreshTokenHeaderLeakTests.cs` |
| 27 | T4 | Guard | A refused native refresh puts the token in no log entry and not in the 401 answer | `Refresh_NativeTokenRefused_NeitherLogsNorAnswerContainTheToken` | `W/Web/RefreshTokenHeaderLeakTests.cs` |

## Spec Outcome coverage
| Outcome | Scenarios |
|---|---|
| #1 Native login returns the token in the body, no cookie | 9, 10 |
| #2 Native refresh rotates | 11, 18 |
| #3 Reuse detection works | 13 |
| #4 Invalid tokens refused alike | 8, 14, 15, 16 |
| #5 Choose organization and logout work on native | 17, 19, 21, 22, 23 (logout-all and change password: 24, 25) |
| #6 Web is untouched | 6, 7; existing cookie, logout and password tests stay green and unchanged (in the final affected run) |
| #7 Browser pages cannot opt in | 1, 2, 3, 4, 5, 6, 7 |
| #8 Same limits | 12, 20 |
| #9 Token not logged | 26, 27 |

## Existing tests updated
None. No existing test is edited. Test files touched: one new test added to `StartupConfigurationTests.cs`.

## Read closely before approving
- #13 and #22 (reuse: family revoked, other device untouched) carry the most security risk.
- #9, #10 and #6 (carrier chosen by exact Origin): a wrong match lets a web page receive the body token (Outcome 7).
- #1 and #4 (startup validator): the only gate that keeps a web origin from being configured as native.
- #27 is a weak guard (session service substituted); it proves little beyond the controller and middleware not logging or echoing the header.

## Assumptions
- No size label in the plan: treated as size L (4 tasks, High risk, 3 batches), so the 40-scenario budget applies; 28 used.
- Six scenarios (6, 7, 12, 17, 20, 27) already pass today because the service rules already refuse these cases, or because the web path does not change; they are guards, not failing tests.
- Stubs: only `NativeAppOptions` and `NativeAppOptionsValidator` (validator body throws `NotImplementedException`) in `Services/NativeApps/`. Not wired in `Program.cs` or `appsettings.json`: the implementer does that (scenarios 4 and 5 fail until then).
- The carrier helper, `AuthResponse` fields and `X-Refresh-Token` constant are not stubbed: tests go through HTTP and use the header literal, so they compile without them.
- The Sentry test builds a real `SentryClient` from the app's own `SentryAspNetCoreOptions` with a recording transport, so it passes whichever mechanism the implementer picks (before-send callback or event processor).
- Scenario 27 is a weak guard: it substitutes the session service, so it proves the controller and middleware never log or echo the header.
- The two Web-folder tests are named in the identity matrix too (T173, T174); `RefreshTokenHeaderLeakTests` is added to that matrix's list of Web.Tests classes outside `Identity/`.
- Idle (7 days) and expired are the same stored state (`ExpiresAt` in the past); both are tested as separate theory rows for clarity.
