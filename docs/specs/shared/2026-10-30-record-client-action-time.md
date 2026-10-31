# Spec: Record Client Action Time

**Type:** Feature   **Date:** 2026-10-30   **Requested by:** igormori

## Business Goals
- Keep the true moment of each action. A sale made offline at 21:00 and synced at 02:00 must not look like a 02:00 sale, or land on the wrong business day.
- Unblock the frontend offline engine (offline from day one). Device clocks are often wrong, so the time must be corrected, not trusted blindly.

## Objective
Every write records when the user acted on the device, corrected for the device's clock error, next to when the server received it. The business day comes from the corrected time.

## Current State
The server stamps only its own time on every write. An action made offline gets the sync time, possibly on the wrong business day. Nothing records when it really happened or how wrong the device clock was.

## Future State
| Question | Answer |
|---|---|
| **What** will change? | Every write carries the device time of the action (with its UTC offset: the gap from universal time). The server also stores when it received the write, the device clock error it computed, and the corrected action time. |
| **How** should it behave? | See Rules below. |
| **Who** is impacted? | Every signed-in user of any client, mainly the frontend offline engine. No access rule changes. |
| **Where** is the impact? | Shared (the mechanism) and every module with write endpoints: Identity, Organization, Menu, and future modules. |

### Rules
| Situation | Behaviour |
|---|---|
| Device time of the action | Sent with every write. Missing or unreadable: refused with 400 (Bad Request) and nothing runs |
| Device clock at send time | Sent with each write or sync batch. The server compares it with its own clock to get the clock error, and shifts the action time by that error. Missing or unreadable: refused with 400 |
| Stored per record | Device action time, corrected action time, clock error, and server received time |
| Corrected time more than 5 minutes in the future | Refused with 422 (Unprocessable Content: understood but invalid); nothing runs |
| Corrected time older than 7 days (the offline session limit) | Refused with 422; nothing runs |
| Business day | Taken from the corrected action time in the time zone of the record's location. Records not tied to a location store no business day |
| A write replayed with an idempotency key | The first stored times stay; a replay never changes them |
| Tampering | The device time is client-supplied, so a user can backdate within the 7-day window; the stored clock error and received time keep it visible for review |

## Outcomes
| # | Outcome | How we know it is achieved |
|---|---|---|
| 1 | Action time and received time are both stored | Test writes with an action time 5 hours old: record holds the corrected action time and a later received time |
| 2 | Clock error is corrected | Test sends a device clock 10 minutes fast: corrected time is 10 minutes earlier than the device time, and the error is stored |
| 3 | Business day uses the location time zone | Test with an action at 23:30 local that is already the next day in universal time: business day is the local date; repeat for a location in a different time zone |
| 4 | Future times are refused | Test with corrected time 6 minutes ahead: 422, nothing stored; 4 minutes ahead is accepted |
| 5 | Too-old times are refused | Test with corrected time 7 days and 1 minute old: 422, nothing stored; just under 7 days is accepted |
| 6 | Missing or invalid times are refused | Test without either time, and with garbage values: 400 each time, nothing stored |
| 7 | Replay keeps the original times | Test repeats a write with the same idempotency key later: stored times are unchanged |
| 8 | Works on every module's writes | Test covers one write in each of Identity, Organization and Menu |

## Out of Scope
| Item |
|---|
| Flagging suspicious times instead of refusing them, and any review screen for them |
| Making the existing "business day of a location" read available to clients |
| Client-generated IDs, changes-since-read catch-up (separate offline items) |
| Writes with no signed-in user (login, registration, password reset) |
| Audit log entries for these times; they are technical, not money, role or settings changes |

## Backlog Impact
| Backlog file | Result |
|---|---|
| `/backlog/offline/2026-10-26-record-client-action-time.md` | Removed (fully solved) |

## Assumptions
- Decision by the requester: out-of-bounds times are refused (422), not flagged.
- The action time is required on every write — the item says "every write carries" it; the frontend is built offline-first, so no old clients exist (not launched).
- Bounds apply to the corrected time, 5 minutes ahead and 7 days back — taken from the item.
- The clock error is stored on each record — the item asks for it so each correction can be audited.
- Anonymous writes are excluded — they come from no offline session.
