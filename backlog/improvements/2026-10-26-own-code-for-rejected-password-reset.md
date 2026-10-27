# Own error code for a rejected password reset

**Status:** proposed
**Severity:** low
**Origin:** docs/plans/shared/2026-10-26-problem-details-with-stable-error-codes/review.md · Task T9
**Date:** 2026-10-26

## Problem
A rejected password reset (wrong, expired, used or replaced code, or unknown email) answers 401
with `auth.unauthorized`, the same code as a missing or expired sign-in. A frontend that reacts to
`auth.unauthorized` with "session over, go to login" would do so on the reset screen too, where
the person is not signed in at all.

## Evidence
- `src/RestaurantManagement.Modules.Identity/Services/Passwords/PasswordResetService.cs:147` — `InvalidRequest()` uses `ErrorCodes.Unauthorized`
- `docs/plans/shared/2026-10-26-problem-details-with-stable-error-codes/review.md` — Watch out 2

## Acceptance criteria
- [ ] Every rejected reset answers 401 with one reset-specific code (for example `password.reset_code_invalid`) in `PasswordErrorCodes`, still the same for all rejection reasons
- [ ] A named test checks that code and identity-matrix row P7 names it

## Related
None
