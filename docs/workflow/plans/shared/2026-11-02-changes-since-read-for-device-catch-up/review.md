# Review: Changes-Since Read for Device Catch-Up

**Verdict:** Ready to merge
**Build:** pass (0 errors; the 6 warnings are all in older test files)   **Tests:** the full suite runs in CI (Continuous Integration). Merge only when CI is green

Second review, after the fix round (`1d4402c`, `887bde2`, `13a4dac`). Security matrix check (`check-security-matrices.py`): passes. Test changes since the baseline commit `1ee8d4f` are limited to the user-approved commits `1168abc` and `bf5409c` and the two new user-approved files `ChangeReaderPagingPostgresTests.cs` and `MenuUnlinkedDeleteTrackingTests.cs`. No test was weakened.

## Earlier Fix first items
- **Fix first 1 (reader skipped an unread tombstone): resolved.** `ChangeReader.cs:60-64` takes the last row read from each source that hit `pageSize + 1` and keeps the earliest of them as the limit. Before the merge, it drops every entry from both sources that sorts after that limit. Every row at or before the limit has been read from both sources, so no later entry for the same ID can hide an unread one. Each page still moves forward: the source that set the limit has `pageSize + 1` rows strictly after the position and at or before the limit, so at least one entry is always handed out. Rows that share a stamp past the cut are read in full on the next page, by the existing "same stamp" read. `IsAfter` uses the same PostgreSQL uuid byte order as the merge. SH152 repeats the exact example from the first review (page size 1) on PostgreSQL. Under the old code, Z@12 would be skipped. Under the new code, it comes back on page 3.
- **`hasMore` change: correct and safer.** `hasMore = page.HasMore || limit is not null`. The cut is now counted only on rows after the last stamp (`next.Count`), so rows at or before the position no longer count as "more". This also resolves Watch out 3 from the first review. One harmless case remains: a source with exactly `pageSize + 1` rows whose IDs collapse in the merge says "more" and costs one extra, empty call. No change is lost.
- **Fix first 2 (MD5, unlinked menu tombstone): resolved.** Row MD5 has been added to `docs/security/menu-matrix.md:141`. `MenuUnlinkedDeleteTrackingTests` deletes a menu with no links and checks that it leaves exactly one tombstone, with no location and with the menu's organization. It reads the rows the same way as `MenuChangeTrackingTests`.

The fix round left the earlier Watch outs (1, 2, 4, 5, 6, 7) unfixed, so they are dropped. They are not repeated here.

## Check before merging
- **`ChangeReader` paging (`ChangeReader.cs:56-95`).** This is still the only code behind "never miss a change", and the limit logic is subtle. SH152 covers the skip that was found. M85 and the paging scenarios cover the rest. A silent loss would leave a device showing a deleted record until its next full reload.
- **The trigger SQL in the two migrations.** Every delete path writes tombstones only through these triggers, including database cascades and the `BEFORE DELETE` trigger on `menus`. MD1 to MD5 and W39 cover them. A mistake here would not show in any service code.
- **The test setup changes in `bf5409c`.** `Pooling=false` makes CI slower. The changes-feed classes run outside the parallel set. M85 (overlapping writes) is the test most likely to be flaky.

## Decisions for you
None

## Pull request description
**Changes-since read for device catch-up**

Devices that were offline (up to 7 days) can now ask for "what changed since my last sync" instead of reloading everything. Each feed returns the changed records, the IDs of deleted records (tombstones) and an opaque cursor, in pages of 1 to 500 (default 100).

**Shared**
- Cursor codec bound to the feed, organization and location. A wrong or forged cursor gets 400 `changes.cursor_invalid`. A cursor older than 14 days gets 410 `changes.cursor_expired`.
- `ChangesQuery` / `ChangesPageResponse<T>`. New error code `location.required` (400). Messages in en, pt and fr.
- `IChangeTracked`, `ApplyChangeTracking`, the `ChangeTrackingSql` trigger texts, `ChangeReader`. Each row is stamped with the PostgreSQL transaction ID. The next cursor starts at the oldest running transaction, so a write that commits late is never missed (it may come back twice). A page never goes past a row it has not read, so a later record can never hide an unread tombstone (SH152).

**Menu**
- The `menus`, `menu_categories` and `location_menus` tables are stamped by triggers. Deletes and cascades write to `menu_tombstones`. Deleting a menu with no links writes one tombstone with no location (MD5).
- `GET api/v1/menus/changes`, `GET api/v1/menus/location-links/changes`, `GET api/v1/menu-categories/changes`. Each needs `X-Location-Id` and Menus Read at that location. Without them, the answer is 403 with no records or tombstones.

**Organization**
- `locations` is stamped by a trigger. `GET api/v1/locations/changes` has the same access as the locations list. A closed location comes back as a change, never as a tombstone.

**Web / docs**
- The guard `ChangeTrackingCoverageTests` fails when a module adds a tenant entity without change tracking, a changes feed or its triggers, unless the entity is excluded with a written reason.
- Architecture skill section 7: the offline-capable modules checklist (commit `de228e6`), and the change tracking row filled in. ef-migration skill: the trigger exception.
- Test infrastructure (user approved): test databases connect without pooling, and the changes-feed classes run in a non-parallel collection.

**Spec outcomes delivered:** #1 to #11 (`scenarios.md`, 45 scenarios: 43, plus 2 added in the review fix round).

**Risk: High.** New Shared contract, hand-written database triggers in two migrations (approved at Gate 1), and four new endpoints that read tenant data. The backlog item `backlog/offline/2026-10-26-changes-since-read-for-device-catch-up.md` is removed (fully solved).

🤖 Generated with [Claude Code](https://claude.com/claude-code)
