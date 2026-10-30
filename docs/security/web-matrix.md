# Web security matrix

The record of every rule the Web project (the app shell that hosts every module) must keep, and
the test that proves each one: how sign-in tokens are checked and refused, how permissions are
granted, HTTPS, secrets and startup checks, and the shape of error answers. The pipeline
(`.github/workflows/dotnet.yml`) runs every test named here, and
`.claude/scripts/check-security-matrices.py` fails the build when this file and the tests drift
apart.

## Rules for this file
The rules in the root `CLAUDE.md`, "Security matrices", apply: any change to Web updates this file
in the same commit; new behaviour adds ❌ rows before it is built; no row is ✅ without a named
test; every test in `tests/RestaurantManagement.Web.Tests/Web/` is named here and carries
`[Trait("Rule", "<ID>")]`; row IDs start with W (rules) or WD (wiring) and are never reused.

**Status:** ✅ covered by the named test · ⚠️ partly covered (the Expected column says what is
missing) · ❌ no test yet · 🐞 known bug, see the backlog item

**Where the tests live:** `tests/RestaurantManagement.Web.Tests/Web/`. Rules that Web enforces
for one module (rate limits on sign-in endpoints, the refresh cookie) are in that module's matrix.

## 1. Refusals and their answers
| ID | Case | Expected | Status | Test |
|---|---|---|---|---|
| W1 | No access token on a protected endpoint | 401 `auth.unauthorized` as Problem Details (`application/problem+json`) with the translated title, in the caller's language (English, Portuguese, French) | ✅ | `PrivateByDefaultTests.UndeclaredEndpoint_NoAccessToken_ReturnsStandardUnauthorizedMessage`, `PrivateByDefaultTests.UndeclaredEndpoint_NoAccessTokenInPortuguese_ReturnsPortugueseUnauthorizedMessage`, `PrivateByDefaultTests.UndeclaredEndpoint_NoAccessTokenInEachLanguage_AnswersOneCodeWithATitleInThatLanguage` |
| W2 | Forged or expired access token | 401 `auth.invalid_token` as Problem Details with the translated title; a forged and an expired token get the same body; never says why the token failed | ✅ | `InvalidTokenMessageTests.ProtectedEndpoint_TokenSignedWithAnotherKey_ReturnsInvalidTokenMessageInEnglish`, `InvalidTokenMessageTests.ProtectedEndpoint_TokenSignedWithAnotherKeyInPortuguese_ReturnsInvalidTokenMessageInPortuguese`, `InvalidTokenMessageTests.ProtectedEndpoint_ExpiredToken_ReturnsInvalidTokenMessageWithoutSayingItExpired`, `InvalidTokenMessageTests.ProtectedEndpoint_ForgedAndExpiredToken_AnswerTheSameStatusTitleAndInvalidTokenCode` |
| W3 | Signed in but the stored access does not allow it | 403 `access.denied` as Problem Details with the translated title | ✅ | `AccessDeniedMessageTests.MembersManagePolicy_WaiterWithoutMembersAccess_ReturnsAccessDeniedMessageInEnglish`, `AccessDeniedMessageTests.MembersManagePolicy_WaiterWithoutMembersAccessInPortuguese_ReturnsAccessDeniedMessageInPortuguese`, `AccessDeniedMessageTests.MembersManagePolicy_WaiterWithoutMembersAccess_AnswersAccessDeniedCode` |
| W26 | Manage in another organization only, asking for a Members endpoint | The same 403 status, title and `access.denied` as a member with no Members access: the other organization changes nothing | ✅ | `AccessDeniedMessageTests.MembersManagePolicy_ManageInAnotherOrganizationOnly_AnswersTheSameBodyAndCodeAsAWaiter` |

## 2. Who may call what
| ID | Case | Expected | Status | Test |
|---|---|---|---|---|
| W4 | Endpoint that declares no access rule | Private by default: refused without sign-in, open to any signed-in user; the public sign-in endpoints stay open | ✅ | `PrivateByDefaultTests.UndeclaredEndpoint_NoAccessToken_ReturnsUnauthorized`, `PrivateByDefaultTests.UndeclaredEndpoint_SignedInUser_ReturnsOk`, `PrivateByDefaultTests.PublicAuthEndpoint_NoAccessToken_IsNotRefusedAsUnauthorized` |
| W5 | Permission endpoint without a token | 401 | ✅ | `PermissionPolicyTests.MembersManagePolicy_NoAccessToken_ReturnsUnauthorized` |
| W6 | No organization chosen, even for an Owner | 403: access only counts inside the chosen organization | ✅ | `PermissionPolicyTests.MembersManagePolicy_OwnerButNoOrganizationSelected_ReturnsForbidden` |
| W7 | Organization chosen but no stored access in it, or the level at only some of its non-closed locations | 403 | ✅ | `PermissionPolicyTests.Policy_GridWithoutTheLevelAtEveryLocation_ReturnsForbidden` |
| W8 | A lower level than the policy needs (Read for a Manage policy), or a level in another module only | 403 | ✅ | `PermissionPolicyTests.Policy_GridWithoutTheLevelAtEveryLocation_ReturnsForbidden` |
| W9 | The Owner, or the needed level or more at every non-closed location of the chosen organization | Allowed; a policy is named after its module and level (for example `reports.read`) | ✅ | `PermissionPolicyTests.Policy_OwnerOrLevelAtEveryNonClosedLocation_ReturnsOk` |
| W10 | Every module and level of the access grid | Has a policy an endpoint can require (14: seven modules, Read and Manage) | ✅ | `PermissionCatalogPolicyTests.PolicyProvider_EveryModuleAndReadAndManage_HasAPolicyNamedModuleDotLevel` |
| W11 | The signed-in user and organization | Read only from the token; a missing or malformed ID counts as absent | ✅ | `ClaimsCurrentUserTests.UserId_NoHttpContext_IsNull`, `ClaimsCurrentUserTests.OrganizationId_NoHttpContext_IsNull`, `ClaimsCurrentUserTests.UserId_NameIdentifierClaimIsNotAGuid_IsNull`, `ClaimsCurrentUserTests.OrganizationId_OrganizationClaimIsNotAGuid_IsNull`, `ClaimsCurrentUserTests.OrganizationId_NoOrganizationClaim_IsNull`, `ClaimsCurrentUserTests.UserId_ValidNameIdentifierClaim_ReturnsTheUserId`, `ClaimsCurrentUserTests.OrganizationId_ValidOrganizationClaim_ReturnsTheOrganizationId` |

## 3. HTTPS
| ID | Case | Expected | Status | Test |
|---|---|---|---|---|
| W12 | Outside Development, no certificate path | App refuses to start, naming `Https:CertificatePath` | ✅ | `HttpsHostingExtensionsTests.UseHttpsOnlyOutsideDevelopment_ProductionWithoutCertificate_ThrowsNamingCertificatePath`, `StartupConfigurationTests.Startup_NoCertificateInProduction_FailsNamingHttpsCertificatePath` |
| W13 | Certificate file missing | App refuses to start, naming the setting | ✅ | `HttpsHostingExtensionsTests.UseHttpsOnlyOutsideDevelopment_ProductionWithCertificateFileMissing_ThrowsNamingCertificatePath` |
| W14 | Outside Development | Only HTTPS is served; plain HTTP is refused, even when HTTP addresses are configured | ✅ | `HttpsHostingExtensionsTests.UseHttpsOnlyOutsideDevelopment_ProductionWithCertificate_ServesHttps`, `HttpsHostingExtensionsTests.UseHttpsOnlyOutsideDevelopment_ProductionWithCertificate_RefusesPlainHttpOnSamePort`, `HttpsHostingExtensionsTests.UseHttpsOnlyOutsideDevelopment_ProductionWithHttpUrlsConfigured_DoesNotServePlainHttp` |
| W15 | No port configured | Listens on 8443 | ✅ | `HttpsHostingExtensionsTests.UseHttpsOnlyOutsideDevelopment_ProductionWithoutPort_ListensOnPort8443` |
| W16 | Development | Starts without a certificate | ✅ | `HttpsHostingExtensionsTests.UseHttpsOnlyOutsideDevelopment_DevelopmentWithoutCertificate_StartsWithoutThrowing` |

## 4. Secrets and startup checks
| ID | Case | Expected | Status | Test |
|---|---|---|---|---|
| W17 | Key Vault address | Must be an absolute https address, else the app refuses to start; none set adds no secret source | ✅ | `KeyVaultHostingExtensionsTests.AddKeyVaultSecrets_UriNotAbsoluteHttps_ThrowsNamingKeyVaultUri`, `KeyVaultHostingExtensionsTests.AddKeyVaultSecrets_UriNotSet_AddsNoConfigurationSource` |
| W18 | Token signing key missing, under 32 bytes, or the placeholder in production; token lifetime not positive; a session limit not positive; inactivity limit over the absolute limit | App refuses to start, naming the setting | ✅ | `StartupConfigurationTests.Startup_JwtKeyMissingOrUnder32Bytes_FailsNamingJwtKey`, `StartupConfigurationTests.Startup_PlaceholderJwtKeyInProduction_FailsNamingJwtKey`, `StartupConfigurationTests.Startup_NonPositiveJwtExpireMinutes_FailsNamingJwtExpireMinutes`, `StartupConfigurationTests.Startup_NonPositiveSessionLimit_FailsNamingTheSetting`, `StartupConfigurationTests.Startup_SessionInactivityAboveAbsoluteLimit_FailsNamingJwtSessionInactivityDays` |
| W19 | Code fingerprint key missing or under 32 bytes | App refuses to start, naming the setting | ✅ | `StartupConfigurationTests.Startup_VerificationHashKeyEmpty_FailsNamingHashKey`, `StartupConfigurationTests.Startup_VerificationHashKeyEmptyInProduction_FailsNamingHashKey`, `StartupConfigurationTests.Startup_VerificationHashKeyOf31Bytes_FailsNamingHashKey` |
| W20 | Error tracking (Sentry) | Sample rate outside 0–1: app refuses to start, naming the setting. Unset in production: 0.1. A configured rate is used, 0 included. Debug output off in production | ✅ | `StartupConfigurationTests.Startup_SentryTracesSampleRateInvalid_FailsNamingSentryTracesSampleRate`, `StartupConfigurationTests.Startup_SentryTracesSampleRateUnsetInProduction_DefaultsTo0Point1`, `StartupConfigurationTests.Startup_SentryTracesSampleRateConfiguredInProduction_UsesConfiguredValue`, `StartupConfigurationTests.Startup_SentryTracesSampleRateConfiguredInDevelopment_UsesConfiguredValue`, `StartupConfigurationTests.Startup_SentryTracesSampleRateAtLowerBoundary_AcceptsZero`, `StartupConfigurationTests.Startup_SentryDebugInProduction_IsOff` |
| W21 | API documentation page (Swagger) | Served only in Development; not served in production: 401 (private by default, so the document is refused without sign-in) | ✅ | `SwaggerAvailabilityTests.SwaggerDocument_InDevelopment_IsServed`, `SwaggerAvailabilityTests.SwaggerDocument_InProduction_IsNotServed` |

## 5. Answers to callers
| ID | Case | Expected | Status | Test |
|---|---|---|---|---|
| W22 | Invalid request | 400 `validation.failed` as Problem Details with a title translated to English, Portuguese or French (fr-CA); status and field errors kept | ✅ | `ValidationTitleEndpointTests.Register_InvalidRequest_ReturnsLocalizedValidationTitle`, `ValidationTitleEndpointTests.Register_InvalidRequest_KeepsStatusAndFieldErrorsInBody`, `ValidationTitleEndpointTests.Register_InvalidRequest_AnswersValidationFailedCodeAndKeepsFieldErrors` |
| W23 | Browser request from an allowed origin | May send credentials (cookies) | ✅ | `RefreshTokenCookieEndpointTests.Cors_PreflightFromAllowedOrigin_AllowsCredentials` |
| W24 | Browser request from any other origin | Gets no cross-origin permission | ✅ | `CorsUnknownOriginTests.Cors_PreflightFromUnknownOrigin_GetsNoCrossOriginPermission` |
| W25 | Rate-limited (429) answer to an allowed origin | The page may read `Retry-After`, so it knows how long to wait | ✅ | `CorsRetryAfterTests.Register_RateLimitedFromAllowedOrigin_ExposesRetryAfter` |
| W27 | Rate-limited request | 429 `rate_limit.exceeded` as Problem Details with `Retry-After` kept | ✅ | `CorsRetryAfterTests.Register_RateLimited_AnswersProblemDetailsWithRateLimitCodeAndRetryAfter` |
| W28 | Error made by the framework: bodiless error result, unknown route, wrong method, wrong content type | Problem Details with the fallback code of the status (`http.conflict`, `http.not_found`, `http.method_not_allowed`, `http.unsupported_media_type`) and a title in the caller's language; status unchanged | ✅ | `FallbackProblemDetailsTests.FrameworkError_UnknownRouteWrongMethodBodilessResultWrongContentType_AnswersFallbackCodeWithATitlePerLanguage` |
| W29 | Any endpoint route of the app | Starts with `api/v<n>/` of a supported API version (today `v1`); an endpoint outside every supported version fails the build | ✅ | `ApiVersionRoutesTests.EveryEndpoint_RouteTemplate_StartsWithASupportedApiVersion` |
| W30 | The OpenAPI document of a version | Holds only that version's paths and every one of its operations | ✅ | `ApiVersionDocumentsTests.V1Document_InDevelopment_ListsOnlyV1PathsAndEveryV1Operation` |
| W31 | The OpenAPI document of a version that is not supported | Not served (404) | ✅ | `ApiVersionDocumentsTests.Document_VersionThatIsNotSupported_IsNotServed` |
| W32 | Startup with an idempotency retention that is not positive | The app refuses to start, naming `Idempotency:RetentionDays` | ❌ | `IdempotencyKeyWiringTests.Startup_NonPositiveIdempotencyRetentionDays_FailsNamingIdempotencyRetentionDays` |

## 6. Wiring tests
These protect the app setup rather than a rule users meet.

| ID | Test | Protects |
|---|---|---|
| WD1 | `StartupConfigurationTests.Startup_ValidVerificationHashKey_BindsHashKeyFromConfiguration` | The code fingerprint key is read from configuration |
| WD2 | `StartupConfigurationTests.Startup_DefaultConfiguration_RegistersVerificationCodeHasher` | The code fingerprint service is registered |
| WD3 | `StartupConfigurationTests.Startup_DefaultConfiguration_JwtExpireMinutesIs15` | The access token lifetime binds from configuration (rule: Identity L19) |
| WD4 | `ErrorCodesOpenApiTests.SwaggerDocument_InDevelopment_CodeEnumEqualsTheUnionOfEveryCatalog`, `ErrorCodesOpenApiTests.SwaggerDocument_InDevelopment_SchemaDescriptionStatesTheRetryRuleAndListsTheRetryCodes`, `ErrorCodesOpenApiTests.SwaggerDocument_InDevelopment_EveryOperationDeclaresItsErrorAnswersWithTheProblemDetailsSchema` | The OpenAPI document lists every catalog code, states the retry rule and shows the Problem Details schema on every operation |
| WD5 | `ApiVersionsTests.IncludesRoute_RouteAndVersion_IncludesOnlyRoutesWithThatVersionPrefix` | A version includes only routes under its own `api/<version>/` prefix (v1 never takes v10) |
| WD6 | `ApiVersionDocumentsTests.V1Document_InDevelopment_BearerDescriptionPointsToTheVersionedLogin` | The sign-in help names `/api/v1/auth/login` |
| WD7 | `ApiVersionDocumentsTests.V1Document_InDevelopment_DescriptionStatesTheAdditiveOnlyAndThirtyDayRules` | The document states the additive-only and 30-day rules |
| WD8 | `AppVersionHeaderOpenApiTests.V1Document_EveryWriteOperation_DeclaresTheRequiredAppVersionHeader` | Generated clients send `X-App-Version` on every write |
| WD9 | `AppVersionHeaderOpenApiTests.V1Document_ReadOperations_DoNotDeclareTheAppVersionHeader` | Reads do not ask for it |
| WD10 | `AppVersionHeaderOpenApiTests.V1Document_AppVersionHeader_DeclaresItsFormatLengthAndPurpose` | The header shows its format, 32-character limit and purpose |
| WD11 | `ClientIdOpenApiTests.V1Document_CreateRequest_ShowsIdAsRequiredUuid` | The OpenAPI document shows `id` (uuid, required) on the six create requests that take the client's ID |
| WD12 | `VersionOpenApiTests.SwaggerDocument_InDevelopment_ShowsVersionOnEditableRecordsAndOnEveryDelete` | The OpenAPI document shows `version` on every editable record's response and update request (required on the requests), and the required `version` query parameter on the menu, category and location deletes |
| WD13 | `IdempotencyKeyWiringTests.V1Document_SignedInWrites_DeclareTheOptionalIdempotencyKeyHeaderAsUuid` | Generated clients may send `Idempotency-Key` on every signed-in write |
| WD14 | `IdempotencyKeyWiringTests.V1Document_PublicWritesSessionActionsAndReads_DoNotDeclareTheIdempotencyKeyHeader` | The header is not shown where the key is ignored |
| WD15 | `IdempotencyKeyWiringTests.EveryEndpoint_SignedInWrite_BelongsToAModuleWithAKeyStoreOrIgnoresKeys` | No module's signed-in write silently skips the key |
