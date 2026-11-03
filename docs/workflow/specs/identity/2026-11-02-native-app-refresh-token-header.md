# Spec: Native App Refresh Token in a Header

**Type:** Feature   **Date:** 2026-11-02   **Requested by:** igormori

## Business Goals
- Let the native mobile apps (built with Capacitor: a tool that wraps the web app as an Android/iOS app) keep users signed in. Today the sign-in is kept only in a browser cookie that these apps cannot use reliably.
- Keep the web app exactly as secure as today: the long-lived sign-in secret stays out of reach of scripts running in a browser page.

## Objective
Native apps can sign in, stay signed in (refresh), switch organization and sign out by sending and receiving the refresh token (the long-lived secret that renews the short-lived access token) in headers and the response body, with the same rotation and reuse detection as the cookie flow. The web flow does not change.

## Current State
The refresh token lives only in a protected cookie (not readable by scripts, same-site only) and is never in a response body. Login, refresh, choose organization and logout read it from the cookie; logout-all and change password only clear it. Native apps run from their own origins (Android `https://localhost`, iOS `capacitor://localhost`), so the cookie is not usable for them. Rotation and reuse detection do not depend on where the token came from.

## Future State
| Question | Answer |
|---|---|
| **What** will change? | A second way to carry the refresh token, for native apps only: the server returns it in the response body of login, refresh and choose organization, and reads it from a request header on refresh, choose organization and logout. |
| **How** should it behave? | See Rules below. |
| **Who** is impacted? | Users of the native apps (all roles). Web users see no change. |
| **Where** is the impact? | Identity: login, refresh, choose organization, logout, logout-all, change password. |

### Rules
| Situation | Behaviour |
|---|---|
| Web request (anything not recognised as native) | Unchanged: token only in the cookie, never in a body, never read from the header |
| Native request | Token returned in the body and read from the header; the cookie is neither set nor read |
| How the server recognises a native request | Only by the browser-set Origin header: native mode applies only when Origin is exactly `https://localhost` or `capacitor://localhost`. No extra header or secret |
| A browser page tries to receive the token | Refused or treated as web: no token in the body. A script on a web page can never opt in |
| Rotation, reuse, idle limit, 30-day session limit, deactivated user, removed member | Same rules and same responses as the cookie flow (identity matrix sections 5 to 7) |
| Missing, unknown, expired or another user's header token | Same 401 as the cookie flow; nothing issued |
| Reused token on native | The whole device family is revoked; the user's other devices (web or native) stay signed in |
| Logout (native) | Revokes that device's token, as for web |
| Logout-all and change password (native) | Revoke the user's tokens, as for web; no cookie to clear |
| Rate limit on refresh | Same limit and 429 answer |
| Device-time headers on session actions | Same requirement as the web app (row S13): choose organization, logout-all and change password need both |
| Logging | The token never appears in logs or error messages |
| Precondition | The two native origins are allowed by the deployment's cross-origin settings (deployment configuration, not part of this spec). Note: in production, allowing `https://localhost` with credentials lets any page served from that origin make signed-in calls |

## Outcomes
| # | Outcome | How we know it is achieved |
|---|---|---|
| 1 | Native login returns the token in the body, no cookie | Test: native login gives a body token and no cookie |
| 2 | Native refresh rotates | Test: header token gives a new token and access token; the old one is revoked, same family, same expiry rules as F1 and F13 |
| 3 | Reuse detection works | Test: a rotated token sent again gives 401 and revokes the family; the user's other devices still refresh |
| 4 | Invalid tokens refused alike | Test: missing, unknown, expired, idle (7 days), past 30 days, another user's: 401, nothing issued |
| 5 | Choose organization and logout work on native | Test: choose organization rotates the header token (old one rejected); logout revokes only that device |
| 6 | Web is untouched | Existing web tests pass; web responses still carry no body token and still set the cookie |
| 7 | Browser pages cannot opt in | Test: web-origin requests, with or without any native signal, never get a body token |
| 8 | Same limits | Test: rate limit gives 429; session actions without device-time headers give 400 |
| 9 | Token not logged | Test or review shows no log or error holds the token |

## Out of Scope
| Item |
|---|
| Allowing the native origins in the deployment settings (precondition above) |
| Grace period for parallel refreshes (separate backlog item; parallel refresh on native counts as reuse, as on web) |
| Secure storage of the token inside the app (client work) |
| Proving a request comes from a genuine app build (device or app attestation) |
| Any change to the web cookie flow, its names or lifetimes |

## Backlog Impact
| Backlog file | Result |
|---|---|
| None | No backlog item is solved or partly solved. `/backlog/improvements/2026-10-15-refresh-reuse-grace-window.md` is related but stays unchanged |

## Assumptions
- Login and choose organization also return the token in the body for native — without them a native app has no token to send, so the refresh flow would be unusable.
- Logout-all and change password need no token header — they use the access token, and only clear the cookie today.
- Native sessions have the same lifetimes (7 days idle, 30 days total) — no reason to differ.
- The native origins stay in configuration — they are deployment values, not business rules.
- Decision by the requester: native is recognised by an Origin check only (no extra header, secret or attestation).
- Known, accepted risk of that decision: any page served from `https://localhost` counts as native and can receive a body token, since production allows that origin with credentials.
