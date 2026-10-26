# Record when an offline action happened, corrected for the device clock

**Status:** proposed
**Severity:** high
**Origin:** User decisions of 2026-10-26 on offline-first (backend conversation) · action time
**Date:** 2026-10-26

## Problem
The server stamps its own time on every write. A write made offline at 21:00 and synced at 02:00
gets the sync time, and possibly the wrong business day. Device clocks are also often wrong. This
blocks the frontend offline engine (offline from day one, decided 2026-10-26).

## Evidence
- `src/RestaurantManagement.Shared/Data/Entities/BaseEntity.cs:7` — only server-set timestamps;
  no field for when the action happened on the device
- `src/RestaurantManagement.Modules.Organization/Entities/Location.cs:18` — each location has a
  `TimeZone`, needed for the business date
- `ClientOccurredAt` is client-supplied: a tampered client can backdate it inside the allowed
  window. The bounds below limit that; flags and the audit log make it visible

## Acceptance criteria
- [ ] Every write carries `ClientOccurredAt` (device time with UTC offset)
- [ ] The server stores both `ClientOccurredAt` and `ReceivedAt`
- [ ] At each sync the device sends its current clock; the server computes the device's clock
      offset and corrects `ClientOccurredAt` with it
- [ ] A corrected time more than 5 minutes in the future, or older than the 7-day session limit,
      is rejected or flagged (which of the two is still to decide)
- [ ] The business date comes from the corrected time, in the location's time zone
- [ ] Assumption: the computed offset is stored too, so each correction can be audited

## Related
- `backlog/features/2026-10-15-expose-location-business-day.md` (business date from a moment)
- `backlog/offline/2026-10-26-idempotency-key-on-writes.md`
