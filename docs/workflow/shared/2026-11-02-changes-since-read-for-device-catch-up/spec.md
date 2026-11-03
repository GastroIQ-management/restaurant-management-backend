# Spec: Changes-Since Read for Device Catch-Up

**Type:** Feature   **Date:** 2026-11-02   **Requested by:** igormori

## Business Goals
- A device that was offline (up to 7 days, the session limit) catches up quickly, even on a poor connection.
- Devices stop showing records that were deleted while they were away.
- Unblock the frontend offline engine (offline from day one).

## Objective
Offline-capable data can be read as "what changed since my last sync": changed records, deleted records and a bookmark (cursor) for the next call.

## Current State
Every list read returns everything. There is no way to ask for only what changed. Menus and menu categories are hard-deleted, so no trace is left and a device can never learn that a record is gone.

## Future State
| Question | Answer |
|---|---|
| **What** will change? | Each offline-capable resource gets a "changes since" read. It returns records created or changed after the cursor, the IDs of records deleted after it, and the next cursor. Deletes leave a trace (tombstone: a small note that a record was removed) so they can be reported. |
| **How** should it behave? | See Rules below. |
| **Who** is impacted? | Every signed-in member using a device with offline mode, mainly the frontend offline engine. No access rule changes. |
| **Where** is the impact? | Shared (the mechanism and the tombstones), Menu (menus, menu categories, menu-to-location links) and Organization (locations). Future modules join when they become offline-capable. |

### Rules
| Situation | Behaviour |
|---|---|
| First sync (no cursor) | Returns all current records the member may read, in pages, and a cursor |
| Later sync | Returns only records changed after the cursor. A record changed once is returned once |
| Paging | Results come in pages; the response says whether more remain and gives the cursor to continue |
| Cursor | Opaque to the client (it must not read or build it). Must never miss a change, even when two writes finish out of order; returning a record twice is acceptable |
| Deleted record | Returned as a tombstone (ID only) after the cursor |
| Cursor older than tombstone retention | Refused with 410 (Gone: the old data is no longer available); the client reloads in full |
| Tombstone retention | Longer than the 7-day offline limit (14 days) |
| Scope | Organization from the token and the location from the request's location header. Same access as the matching normal read: Read level at that location for the module |
| Access lost | A member who no longer has Read at the location is refused with 403 on that location's feeds, and the device drops that location's data. Tombstones are sent only for records leaving a location the member can still read (for example a detached menu) |
| Other organizations or locations | Never returned, in records or tombstones |
| Locations (closed, never deleted) | A closed location is a change, not a tombstone |

## Outcomes
| # | Outcome | How we know it is achieved |
|---|---|---|
| 1 | Only changes are returned | Create 3 records, sync, change 1: second sync returns exactly that 1 |
| 2 | No change is returned twice | Sync again with the new cursor and no writes: empty result |
| 3 | Deletes are reported | Delete a menu and a category after the cursor: both IDs come back as tombstones |
| 4 | Paging works | With more records than one page, following the cursors returns every record exactly once |
| 5 | Out-of-order commits lose nothing | Test with two overlapping writes finishing in reverse order: neither is missed |
| 6 | Expired cursor refused | Cursor older than 14 days: 410 |
| 7 | No cross-organization leak | A member of organization A never receives records or tombstones of organization B |
| 8 | Location access respected | No Read at location X: refused; records of other locations never appear |
| 9 | Lost access is reported | Member's access to a location is removed: the next sync is refused with 403 and the device drops that location's data |
| 10 | Tombstones kept long enough | A tombstone is still returned 8 days after the delete |
| 11 | Closed location is a change | Closing a location returns it as changed, not as a tombstone |

## Out of Scope
| Item |
|---|
| Deactivating instead of deleting menus and categories (separate decision) |
| Client-generated IDs, version checks on writes (separate offline items) |
| Offline-capable resources of modules that do not exist yet (Inventory, Orders) |
| Real-time push of changes to devices |
| Removing old tombstones beyond the retention rule (no cleanup schedule defined here) |

## Backlog Impact
| Backlog file | Result |
|---|---|
| `/backlog/offline/2026-10-26-changes-since-read-for-device-catch-up.md` | Removed (fully solved) |

## Assumptions
- Resources covered are those that exist today: menus, menu categories, menu-location links, locations. Users and members are not offline data.
- Tombstone retention is 14 days — backlog only requires more than the 7-day offline limit; 14 leaves a safe margin.
- Menu location links are read per location, so they follow the same location access as menus.
- Page size is a technical choice; not specified here.
