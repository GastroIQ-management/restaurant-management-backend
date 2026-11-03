# Spec: Idempotency Key on Writes

**Type:** Feature   **Date:** 2026-10-30   **Requested by:** igormori

## Business Goals
- Stop a retried request from running twice. A lost response, a timeout or a double tap must never create a duplicate record, and later never a duplicate order or double payment.
- Unblock the frontend offline engine (offline from day one): every write replayed from the device outbox (the queue of actions saved while offline) carries a key.

## Objective
Let any signed-in client attach a unique key (an "idempotency key": a random code that marks one intended action) to a write. The same action sent again with the same key is applied once, and the client gets the original result back.

## Current State
No write checks for repeats. If the server applies a write but the response never reaches the client, the client retries and the write runs again. Client-generated IDs protect only record creation; other writes (change a quantity, close a bill, record a payment) have no protection.

## Future State
| Question | Answer |
|---|---|
| **What** will change? | Every write request (create, replace, partial update, delete) accepts an optional `Idempotency-Key` header holding a UUID (Universally Unique Identifier: a random 128-bit code). One shared mechanism serves all modules. |
| **How** should it behave? | See Rules below. |
| **Who** is impacted? | Every signed-in user of any client, mainly the frontend offline engine. Owners and all roles alike: no access rule changes. |
| **Where** is the impact? | Shared (the mechanism) and every module with write endpoints: Identity, Organization, Menu, and future modules. |

### Rules
| Situation | Behaviour |
|---|---|
| No key sent | Request behaves exactly as today |
| New key | Write runs normally; if it succeeds, the key and the response are saved |
| Same key, same request again | The saved response (same status and body) is returned; the write does not run again |
| Same key, different request (different content or different target) | Refused with 422 (Unprocessable Content: request understood but invalid) and nothing runs |
| Same key while the first request is still running | Refused with 409 (Conflict) and nothing runs; the client may retry later |
| Key used by another user or organization | Never returns the other user's response; each user's keys are separate and a key is only ever matched inside its own organization |
| Key not a valid UUID | Refused with 400 (Bad Request) |
| First request failed (validation error, refusal, server error) | Nothing is saved for the key; sending it again runs the request normally |
| Crash between the write and saving the key | Either both are saved or neither: never a write with no key, never a key with no write |
| Key older than the retention time | Expired; reusing it counts as a new key. Expired keys are purged automatically |
| Retention time | Configurable; default 8 days (longer than the 7-day offline limit) |

## Outcomes
| # | Outcome | How we know it is achieved |
|---|---|---|
| 1 | A repeated write with the same key and same request runs once | Test sends a create twice with one key: one record exists, both responses are identical |
| 2 | Same key, different request is refused | Test reuses a key with a changed body and with a changed target: 422 both times, no change stored |
| 3 | Concurrent duplicate is refused | Test sends two simultaneous requests with one key: one succeeds, the other gets 409, one record exists |
| 4 | No leak between users or organizations | Test reuses user A's key as user B, and as a user of another organization: B never receives A's response and B's write runs on its own |
| 5 | Writes without a key are unchanged | Existing write tests pass with no key |
| 6 | Invalid key is rejected | Non-UUID key returns 400 and nothing runs |
| 7 | Failed first attempts can be retried | Test sends a request that fails validation, then a corrected one with the same key: second one runs |
| 8 | Write and key are saved together | Test forces a failure while saving the key: the write is not applied either |
| 9 | Keys expire and are purged | Test with a key older than the retention time: treated as new; the old row is removed |
| 10 | Works on every module's writes | Test covers one write in each of Identity, Organization and Menu |

## Out of Scope
| Item |
|---|
| Client-generated record IDs on create (separate offline item) |
| Optimistic concurrency versions (already delivered) and changes-since-read catch-up |
| Recording the time the user acted on the device |
| Requiring a key (making it mandatory) on any endpoint |
| Writes with no signed-in user (login, registration, password reset): no user to scope a key to |
| Audit log entries for keys; keys are technical and not a money, role or settings change |

## Backlog Impact
| Backlog file | Result |
|---|---|
| `/backlog/offline/2026-10-26-idempotency-key-on-writes.md` | Removed (fully solved) |

## Assumptions
- The key is optional — existing clients keep working and the requirement says writes "accept" it.
- Only successful responses are saved — a failed attempt changed nothing, so retrying it safely is more useful than replaying the error.
- Anonymous writes are excluded — keys are scoped by user and organization, which they lack.
- "Same request" means same method, same target and same content — otherwise a reused key could silently skip a different action.
- Retention default is 8 days — it must exceed the 7-day offline limit, and the item suggested 8.
