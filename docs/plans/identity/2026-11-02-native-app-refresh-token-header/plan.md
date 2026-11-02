# Plan: Native App Refresh Token in a Header

**Source:** docs/specs/identity/2026-11-02-native-app-refresh-token-header.md   **Date:** 2026-11-02   **Status:** Approved
**Risk:** High — auth change: the refresh token (long-lived session secret) is returned in a response body for requests from native app origins

## Summary
4 tasks in 3 batches. A configured, startup-checked list of native origins comes first. Then one HTTP-only helper in the Identity controllers picks the carrier per request: cookie for web, request header and response body for native. The session service and the token rules do not change. Last, error reports never carry the header token.

## Tasks
| Order | ID | Task | Batch | Priority | Depends on | Outcome |
|---|---|---|---|---|---|---|
| 1 | T1 | Native origins setting, checked at startup | B1 | P1 | — | #7 |
| 2 | T2 | Native login, refresh and choose organization | B2 | P1 | T1 | #1, #2, #3, #4, #5, #6, #7, #8 |
| 3 | T3 | Native logout, logout-all and change password | B2 | P1 | T2 | #5, #6, #8 |
| 4 | T4 | Header token kept out of error reports | B3 | P2 | T2 | #9 |

## Flags
| Task | New integration | New library | Security |
|---|---|---|---|
| T1 | — | — | Auth change: a setting decides which callers may receive the token in a body |
| T2 | — | — | Auth change on public endpoints (login, refresh) and choose organization: token in the body, read from a header |
| T3 | — | — | Auth change on public logout and on signed-in logout-all and change password |
| T4 | — | — | Secret kept out of Sentry (error tracking service) events |

## Overall Definition of Done
- [ ] All tasks done, tests passing
- [ ] All Outcomes met (spec Outcomes)
- [ ] Affected tests green after the last batch; full suite green in CI (Continuous Integration)
- [ ] No out-of-scope changes
- [ ] `docs/security/identity-matrix.md` and `docs/security/web-matrix.md` rows ✅ with their tests; `python3 .claude/scripts/check-security-matrices.py` passes

## Checklist
- [ ] T1 — Native origins setting, checked at startup
- [ ] T2 — Native login, refresh and choose organization
- [ ] T3 — Native logout, logout-all and change password
- [ ] T4 — Header token kept out of error reports

## Assumptions
- Header `X-Refresh-Token`; body fields `refreshToken` and `refreshTokenExpiresAt`, left out of web responses entirely (not sent as null) — names not set by the spec
- Native origins live in `NativeApp:Origins` (shipped with both); startup refuses any value other than `https://localhost` or `capacitor://localhost`, so a web origin can never be configured as native
- Origin compared exactly (case-sensitive, no port, no trailing slash); a missing Origin means web
- Native login reads no header token (the spec reads it only on refresh, choose organization and logout); a cookie sent on a native request is ignored
- Request headers can reach Sentry, so the refresh header is removed from every error event, not only on auth routes
