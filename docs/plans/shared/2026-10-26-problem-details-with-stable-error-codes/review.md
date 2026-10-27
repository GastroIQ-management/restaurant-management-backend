# Review: Problem Details with stable error codes

**Verdict:** Fix first — two security matrix rows do not match their tests: SH55/SH56 are still ❌ although their test exists and every catalog is filled, and P7 now promises a code that no named test checks
**Build:** pass (0 errors, 6 warnings in test files, none new in this diff)   **Tests:** full suite runs in CI (Continuous Integration) — merge only when it is green

Scope note: PR (pull request) #54 already merged T1–T8 (up to `9b9c329`) into `development`.
This branch adds only 3 commits on top: `4823b9e` (T9 plus the missed reset-code refusal),
`10fb631` and `c5a34a5` (plan bookkeeping). The review covers those 3 commits. It also covers the
plan's completeness and the test changes since the baseline `72d3251`.

## Fix first
| # | File:line | Problem | Change |
|---|---|---|---|
| 1 | docs/security/shared-matrix.md:81-82 | SH55 and SH56 are still ❌. B1's Delivered note says they waited for the module catalogs (T4–T7), and those catalogs are now filled. Nobody flipped the rows, so the matrix says these rules are not proven when they are | Confirm `ErrorCodeCatalogsAcrossModulesTests` passes, then set SH55 and SH56 to ✅ in the T9 follow-up commit |
| 2 | docs/security/identity-matrix.md:194 | P7 now says the reset refusal has code `auth.unauthorized`. None of the five tests it names checks `Code`: `PasswordResetServiceResetTests` checks only `StatusCode` and `Message`. A ✅ row whose Expected result no test checks breaks the matrix rules | Either remove "with code `auth.unauthorized`" from P7's Expected result, or, with the user's approval (test files are read-only after Gate 2), add `rejection.Code.ShouldBe("auth.unauthorized", ...)` to a named P7 test |

## Watch out
| # | File:line | Problem | Suggestion |
|---|---|---|---|
| 1 | docs/plans/shared/2026-10-26-problem-details-with-stable-error-codes/plan.md:42 | The DoD (Definition of Done: the checks that must pass to count as finished) item "full suite green in CI" is ticked after a local run only. CI has not run | Untick it until CI is green on this pull request, or reword it to "full suite green locally" |
| 2 | src/RestaurantManagement.Modules.Identity/Services/Passwords/PasswordResetService.cs:147 | The user chose `auth.unauthorized` for a rejected reset code. It is the same code as a missing token (`Program.cs:104`). A frontend that answers `auth.unauthorized` with "session over, go to login" or a token refresh will do the wrong thing on the anonymous reset screen. No enumeration risk: every rejection still goes through `InvalidRequest()` | Keep it for now (user decision). Consider a backlog item for a dedicated `password_reset.invalid_code` (401) in `PasswordErrorCodes` |
| 3 | src/RestaurantManagement.Shared/Utils/Exceptions/ErrorCodes.cs:81 | The comment says `Fallback` also answers "a code-less refusal". T9 made that impossible, so the comment is now stale | Change it to "The code that answers a framework-made error of this status." |
| 4 | docs/plans/shared/2026-10-26-problem-details-with-stable-error-codes/plan.md:48-56 | The Checklist ticks have no commit hashes | Add them: T1 `5ed32e0`, T2 `431ec69`, T3 `4120a16`, T4 `d9ee2f2`, T5 `92eba53`, T6 `d93d218`, T7 `9110209`, T8 `9b9c329`, T9 `4823b9e` |
| 5 | git log (`10fb631`, `c5a34a5`) | T9's Delivered note, its tick and the DoD ticks are in two docs-only commits, apart from the task commit `4823b9e`. That is not wrong, but it adds noise to "one commit per task" | Next time put the Delivered note and the tick in the task commit |

## Check before merging
- **`BusinessException.cs` (constructor removed).** Any refusal without a code now fails to
  compile. The build is green, so no code-less throw is left in `src/`. Any branch opened from
  `development` before this merge will break on rebase if it adds one.
- **`PasswordResetService.cs:147`.** The reset refusal changes from `http.unauthorized` (the
  fallback) to `auth.unauthorized`. Status (401) and message are unchanged, and every rejection
  path still answers the same code. Check the frontend does not treat this code as "session
  expired" on the reset screen.
- **Test changes since the baseline.** All match what the user approved: the scenario 6 host runs
  in Production, the empty-grid mock for Pedro, `ProblemWithoutTraceIdAsync` in the lockout test,
  and formatting only in five Identity test files (checked with `git show -w`). No test was
  deleted, skipped or weakened.

## Pull request description
**Require an error code on every refusal (Problem Details, part 2)**

Finishes the "Problem Details with stable error codes" plan. PR #54 already shipped T1–T8:
every error is RFC 9457 Problem Details (`application/problem+json`) with a stable,
never-translated `code`, a localized `title`, per-module code catalogs, and the codes plus the
retry rule in the OpenAPI document.

This PR adds the last task (T9) and the plan bookkeeping:
- **Shared:** removed the code-less `BusinessException(string, int)` constructor. A refusal
  without a code no longer compiles (scenario 40, matrix row SH57 now ✅).
- **Identity:** the rejected password-reset code (wrong, expired, used or replaced code, or an
  unknown email) now answers `auth.unauthorized` instead of the fallback `http.unauthorized`.
  Status 401 and message are unchanged, and every rejection still gives one answer (no account
  enumeration). Matrix row P7 is updated.
- **Docs:** T9's Delivered note, the plan marked Done, and the Overall DoD ticked.

Outcomes delivered (whole plan): #1 every error is Problem Details with a stable `code` · #2 codes
are never translated, `title` and `detail` are localized · #3 each 409 says "in progress" (retry)
or a lasting conflict · #4 the retry rule is published (network errors, 5xx, `request.in_progress`,
429) · #5 the codes are in the OpenAPI document.

Risk (from plan.md): **High.** This is a new Shared contract that changes the error body of every
endpoint, including sign-in refusals. There is no data or schema change. Flags: 401 and 403
change shape; a forged and an expired token keep one code; collapsed login, verify and reset
failures keep one status, one body and one code; a 500 hides the exception text outside
Development.

The backlog item `backlog/offline/2026-10-26-problem-details-with-stable-error-codes.md` is
fully solved. Its deletion is left for the user to commit.

🤖 Generated with [Claude Code](https://claude.com/claude-code)
