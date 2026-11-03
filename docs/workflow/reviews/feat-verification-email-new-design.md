# Review: Verification email in the new GastroIQ design

**Verdict:** Fix first — the code in the subject now reaches the development log, which breaks matrix row SH27, and SH24 claims a setting link that no test checks.
**Build:** pass (0 errors, 6 warnings, all in existing test files)   **Tests:** full suite runs in CI — merge only when it is green

Small change (`/small`, one commit `5d8ae8a`): no spec or plan, so Completeness is skipped. The user waived the small-change file limit (11 files).

## Fix first
| # | File:line | Problem | Change |
|---|---|---|---|
| 1 | `src/RestaurantManagement.Shared/Services/Email/EmailService.cs:52`, `tests/RestaurantManagement.Shared.Tests/LocalizedCodeEmailTests.cs:143`, `docs/security/shared-matrix.md:110` | In Development without an SMTP (Simple Mail Transfer Protocol) host, the service logs the subject. The verification subject now carries the code, so the code is written to the log. The SH50 test asserts this (`"Seu código do GastroIQ: 482913"` in the log). SH27 still says "never the body (so codes stay out of logs)". Its test still passes only because it uses a reset subject, which has no code. The row is stale. | Apply the option chosen in Decision 1. Make SH27 and its test match the code. |
| 2 | `docs/security/shared-matrix.md:107` | SH24 now says "the logo address comes from the `Email:LogoUrl` setting". `Build_WithLogoUrl_LogoImageUsesIt` only proves that the template uses the URL it is given. No test checks that `EmailService.cs:83` passes `EmailOptions.LogoUrl`, so this ✅ has no matching test. | Reword the Expected text to "the logo image uses the logo address it is given", or add a test that builds `EmailService` with a `LogoUrl` and checks the sent body. |

## Watch out
| # | File:line | Problem | Suggestion |
|---|---|---|---|
| 1 | `src/RestaurantManagement.Shared/Templates/verification-email.html:34`, `src/RestaurantManagement.Shared/Options/EmailOptions.cs:11`, `src/RestaurantManagement.Web/appsettings.json:88` | The default `LogoUrl` is `""`, which renders `<img src="">`. Many mail clients show a broken-image icon, not the alt text. The comment says "the email shows the text GastroIQ", but no code does that. | Leave the `<img>` out when `LogoUrl` is empty and show a text wordmark, or fail at startup when it is missing. Make the comment match. |
| 2 | `src/RestaurantManagement.Shared/Services/Email/VerificationEmailTemplate.cs:61` | `Subject` HTML-encodes the code, but a subject is a plain-text header. It does no harm with numeric codes, but it is the wrong encoding for that place. The `<title>` in `Build` gets its own encoded value. | Pass `code` raw in `Subject`. |
| 3 | `src/RestaurantManagement.Shared/Services/Email/VerificationEmailTemplate.cs:87-103` | `LoadLayout`, `Resolve` and the blank-name greeting repeat `LocalizedCodeEmail` almost line for line. There are now two ways to build a code email. | Move resource loading into one internal helper that both classes use. |
| 4 | `src/RestaurantManagement.Shared/Services/Email/VerificationEmailTemplate.cs:75`, `tests/RestaurantManagement.Shared.Tests/VerificationEmailTemplateTests.cs:100` | The year comes from `DateTime.UtcNow`, and the test reads the clock again. The test fails if it runs across midnight on 31 December. The codebase uses `TimeProvider` elsewhere. | Pass the year (or a `TimeProvider`) in, and fix it in the test. |
| 5 | `tests/RestaurantManagement.Shared.Tests/VerificationEmailTemplateTests.cs:142` | `ShouldContain("&quot;")` now also matches the encoded logo URL, so it no longer proves that the address was encoded. | Assert `ana+&quot;x&quot;@cantina.com.br`. |

## Checks asked for
- **Implementer's test failures:** the "471 passed, 11 failed" run was `Modules.Identity.Tests`, not Organization. Run here: Identity has 471 passed and 11 failed. All 11 fail with `DockerUnavailableException` or in the `IdentityPostgresFixture` constructor (Testcontainers), and Docker is not running. `Modules.Organization.Tests` uses SQLite and passes 124/124. `Shared.Tests` passes 364/364 and `Modules.Menu.Tests` passes 71/71. `IEmailService` is unchanged, and no test outside Shared.Tests refers to the old subjects. The Web.Tests failures are the Docker problem too. They are not regressions, but CI must confirm them.
- **`LocalizedCodeEmail` and the password-reset email are unchanged:** confirmed. The diff does not touch `LocalizedCodeEmail.cs`, `PasswordResetEmailTemplate.cs` or the three `password-reset-email*.html` files.
- **HTML encoding:** the first name, email and code are `WebUtility.HtmlEncode`d before filling. The logo URL is encoded, and the `src` attribute is double-quoted, so a `"` cannot break out. Language texts are constants with no user input.
- **Single-pass replacement:** confirmed. `Regex.Replace` with an evaluator never re-scans what it inserts. Each text is filled from the values, then the layout is filled once from the filled texts. A name like `{{heading}}` stays as typed, and the test proves it. Every layout placeholder has a value.
- **SH24 and SH26:** every named test exists and carries the matching `Rule` trait. `check-security-matrices.py` passes. SH26 matches its tests. SH24 has one unmatched claim (Fix first 2).

## Check before merging
- `EmailService.cs:52` plus the code in the subject: a verification code reaches the logs (Fix first 1).
- `appsettings.json:88`: each environment must set `Email:LogoUrl` (environment variable `Email__LogoUrl`). Otherwise every verification email shows a broken logo (Watch out 1).
- The code in the subject shows in lock-screen and inbox previews. This is what was asked for, but it is a visible change.

## Decisions for you
| # | Decision | Options | Recommended |
|---|---|---|---|
| 1 | Should a verification code ever appear in the development log? (Fix first 1) | **A.** Keep codes out of logs: the development branch of `SendEmailAsync` stops logging the subject (recipient only). The SH27 and SH50 tests and rows change to match. Developers without a mail server cannot read the code from the console. · **B.** Allow it in Development only: reword SH27 to "never the body; the verification subject carries the code, so in Development without a mail server the code is logged", and keep the tests. Easier for local work, but a shared environment run as Development without SMTP would log real codes. | A — SH27 and the security skill keep codes out of logs on purpose. B depends on no shared environment ever running that way. |

## Pull request description
**Verification email in the new GastroIQ design**

The verification-code email now uses the GastroIQ design. One HTML layout replaces the three per-language files, and the English, Portuguese and French texts live in `VerificationEmailTemplate`. Any other language falls back to English, as before. The subject now shows the code (for example "Your GastroIQ code: 482913"), so users can read it without opening the email.

- **Shared:** `VerificationEmailTemplate` fills the layout in one pass and HTML-encodes the name, address, code and logo address. `EmailOptions` gains `LogoUrl`. The `verification-email-pt.html` and `verification-email-fr.html` files are removed. The password-reset email is unchanged.
- **Web:** `appsettings.json` adds an empty `Email:LogoUrl` placeholder.
- **Security matrix:** SH24 and SH26 in `docs/security/shared-matrix.md` are updated with their tests.

Deployment: set `Email__LogoUrl` to the public address of the logo in every environment.

No migration, no new package, no endpoint or Shared contract change.

🤖 Generated with [Claude Code](https://claude.com/claude-code)
