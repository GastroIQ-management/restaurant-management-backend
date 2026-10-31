# Review: Idempotency key on writes

**Verdict:** Ready to merge
**Build:** pass (0 errors, 6 existing test warnings)   **Tests:** full suite runs in CI (Continuous Integration) — merge only when it is green

Second and final review, after the fix round (`6cd69d6`, `3e12672`, `d826496`). Security matrices check (`check-security-matrices.py`) passes. No package added, so no vulnerable-package run was needed. Test edits since the baseline commit `2e76ae0`: the OR72 assertion (user-approved, `e6e934a`), the planned `TestHostSettings` line, and the new T167 test added by the fix round. No existing test was edited, deleted, skipped or weakened.

All 4 Fix first items from the first review are resolved:
1. `InvitationService.Accept.cs:72` now uses `JoinableTransaction.BeginOrJoinAsync(_db)`. Identity matrix row T167 and scenario 41 are added, with the test `IdentityIdempotencyEndpointTests.AcceptInvitation_SameKeyTwice_JoinsOnceAndTheReplayAnswersTheSame`. A second accept that does not replay answers `LinkInvalid` (the link is already used). So the second 204 proves the replay. No other signed-in Identity write that keeps the key opens its own transaction. The remaining `BeginTransactionAsync` calls are on anonymous endpoints or on `change-password`, which ignores the key.
2. `IdempotencyOutcome`, `IdempotencyReservation` and `StoredResponse` moved to `Shared/Data/Idempotency/` with the new namespace. The folders now hold 9 and 7 files.
3. T165 is narrowed to "`logout-all` sent twice with one key".
4. The `plan.md` Checklist carries the commit hashes.

## Watch out
These were Watch outs in the first review, and the fix round did not take them up. Fix them now or drop them. They do not block the merge.

| # | File:line | Problem | Suggestion |
|---|---|---|---|
| 1 | `src/RestaurantManagement.Shared/Services/Idempotency/IdempotencyKeyStore.cs:42-54`, `IdempotencyKeyFilter.cs:150` | Suppose a write runs longer than `InProgressTimeoutSeconds` (120 s) and a retry takes its key over. `CompleteAsync` then updates 0 rows, or overwrites the retry's row, and the write still commits. That leaves a write with no key of its own. `ReleaseAsync` can also delete the retry's row | Make `CompleteAsync` return the `ExecuteUpdateAsync` count. Also match `State == InProgress && RequestHash == hash`. In the filter, throw when the count is not 1, so the write rolls back |
| 2 | `docs/plans/shared/2026-10-30-idempotency-key-on-writes/batches/B4.md:31`; `src/RestaurantManagement.Modules.Organization/Services/OrganizationService.cs:61-69, 265-268` | The Delivered note says "under 300 lines", but the file has 307. The trims also removed comments in `GetOrganizationsAsync`, which this task does not touch | Correct the note to "307 lines (316 before)". Restore the two comments in `GetOrganizationsAsync` |
| 3 | `src/RestaurantManagement.Shared/Services/Idempotency/IdempotencyKeyFilter.cs:123` | Magic string `"Location"` | Use `HeaderNames.Location` |

## Check before merging
- **One transaction around every keyed write** (`IdempotencyKeyFilter.cs:141-153`). Any signed-in write whose service opens its own transaction on its module's database context now fails with 500 when a key is sent. Every future service must use `JoinableTransaction.BeginOrJoinAsync`. The invitation accept was one such case and is now fixed. Consider stating this rule in each module's `CLAUDE.md`.
- **The replay is answered before the module's access checks** (approved at Gate 1). For up to 8 days, a user whose access was removed can still get back their own earlier stored answer. It is never another user's answer.
- **Create organization's Owner undo** (`OrganizationService.cs`). The revoke runs when the filter's transaction ends, after the service has returned. It relies on the request-scoped `_accessSetup` and `_tenantScope`, and OR72 proves it on PostgreSQL. The backlog file `backlog/offline/2026-10-26-idempotency-key-on-writes.md` is deleted but not committed: commit the deletion yourself.

## Decisions for you
None

## Pull request description
**Idempotency key on writes**

Every signed-in write (POST, PUT, PATCH, DELETE) now accepts an optional `Idempotency-Key` header holding a UUID (Universally Unique Identifier). If the same request is sent again with the same key, the write does not run twice and the first answer comes back. The frontend offline engine relies on this when it replays its outbox.

- **Shared:** a key table mapping (one call per module), a store, settings (`Idempotency` section, 8-day retention, 120 s in-progress timeout), an hourly purge job and a resource filter. The filter checks the key: not a UUID gives 400 `idempotency_key.invalid`; reused for another request gives 422 `idempotency_key.reused`; still running gives 409 `request.in_progress`. It replays a finished answer, and saves a new success answer in the same transaction as the write. Also adds `JoinableTransaction`, so a service joins that transaction and can register an undo step.
- **Web:** registers the filter, settings check and purge job. The OpenAPI document shows the optional header on signed-in writes. A check fails when a signed-in write has no key store and no ignore marker.
- **Menu / Organization / Identity:** each adds its own key table (`menu_`, `organization_`, `identity_idempotency_keys`) through an `AddIdempotencyKeys` migration, so a key always commits in its module's database. Menu create, organization create and invitation accept join the request's transaction. Create organization revokes the Owner when the write does not commit. `select-organization`, `logout-all` and `change-password` ignore the key, so no token or cookie answer is ever stored.

Spec Outcomes 1–10 delivered. Outcome 3 (simultaneous duplicates) is proven at store level on PostgreSQL. Cross-tenant and 403 with a key are proven through Menu (Gate 2 decision).

**Risk: high.** New Shared contracts, three new tables (no data, not launched), and a replay answered before the module's access checks (same user and same organization only). The backlog item `offline/2026-10-26-idempotency-key-on-writes` is solved and removed.

🤖 Generated with [Claude Code](https://claude.com/claude-code)
