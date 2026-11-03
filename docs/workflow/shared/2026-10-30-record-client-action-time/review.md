# Review: Record Client Action Time

**Verdict:** Ready to merge
**Build:** pass (0 errors, 6 warnings, all in tests and older than this branch)   **Tests:** full suite runs in CI — merge only when it is green

Second and final review. Both findings from the first review are answered:
- Fix first 1 (`ee8dada`): `ClientTimeValidator.cs:33-40` now checks `lead` (the corrected time minus the received time,
  a `TimeSpan`) against the bounds before it builds `corrected`. The largest possible `lead` (about 20,000 years) fits in
  a `TimeSpan` (about 29,000 years), so nothing can overflow. An offset that pushes a parsed date out of range fails
  `DateTimeOffset.TryParse` and gives 400 `client_time.invalid`. New test
  `ClientTimeValidatorTests.Validate_BothHeadersReadableButFarApart_Throws422InsteadOfOverflowing` covers both extreme
  pairs (expects `client_time.in_future` and `client_time.too_old`, both 422). Matrix row SH136 and scenario 41 were
  added in the same commit, and the B1 Delivered note was updated.
- Watch out 1 (`6b3effb`): `plan.md:35` is unticked, with "green locally; CI runs on the pull request".

Checked again: `check-security-matrices.py` passes (Shared 267 named tests). No package change and no secrets. Since
the baseline `971d7fa`, `tests/` changed in only two places: the approved `AuditFieldsMigrationTests` helper (2 lines)
and the new SH136 test method that the first review asked for. No existing test was edited, removed or weakened.
Commits follow Conventional Commits, one per task, with scenario numbers in the bodies; nothing was amended. Every new
file is in an allowed folder.

The local Npgsql 53300 failures ("too many clients") do not come from this branch. Every new Web test disposes its
factory (`await using`, or `Dispose` in `ClientTimeInterceptorWiringTests`). The test Postgres container already sets
`max_connections=500`.

## Check before merging
- `ClientTimeFilter` refuses every signed-in write that does not send both headers, session actions included
  (`select-organization`, `logout-all`, `change-password`). A client that does not send them gets 400 on every write.
  It runs after `IdempotencyKeyFilter`, so a replay is answered first and a refusal frees the key (SH104, SH125–SH131).
- `ClientTimeSaveChangesInterceptor` runs on every save of all four contexts. During an Organization save it resolves
  `ILocationBusinessDay`, which queries the same `OrganizationDbContext`. No entity derives from `LocationEntity` yet,
  so this path is proven only by unit tests and `LocationBusinessDayTests`. The first feature with a location record
  will run it for real for the first time.
- The spec commit `7a297d7` (cherry-picked) deletes `backlog/offline/2026-10-26-record-client-action-time.md`. Confirm
  that the item is fully solved. Watch the CI run for the 53300 connection failures seen locally.

## Decisions for you
None

## Pull request description
**Record when the user really acted on the device**

Every signed-in write now carries two headers in RFC 3339 format with an offset. `X-Client-Action-Time` is the device
time of the action. `X-Client-Sent-At` is the device clock at send time. The server computes the device clock error
and corrects the action time. It refuses a corrected time more than 5 minutes ahead or more than 7 days old. It stores
the times on every saved record. An offline sale made at 21:00 and synced at 02:00 keeps its 21:00 time.

What changed:
- **Shared:** a filter checks the headers. Missing or unreadable gives 400 `client_time.required` or
  `client_time.invalid`. Out of bounds gives 422 `client_time.in_future` or `client_time.too_old`. Extreme but
  readable dates also give 422, never a 500. Titles are in English, Portuguese and French. A request-scoped holder
  keeps the accepted times. A save interceptor stamps five nullable columns on every `BaseEntity`, and a business day
  on new `LocationEntity` rows. Adds the new contract `ILocationBusinessDay` and an audit log migration.
- **Organization:** `LocationBusinessDay` implements the contract. It uses the location's time zone plus the
  organization cutoff (04:00 by default), inside the token organization only. Adds a migration.
- **Identity, Menu:** migrations (nullable columns, no backfill).
- **Web:** wires the filter and the interceptor. The OpenAPI document declares both headers as required on signed-in
  writes.

Spec Outcomes delivered: 1–8 (stored times, clock correction, business day in the location time zone, future and
too-old refusals, missing or invalid refusals, replay keeps the first times, one write per module).

Risk (from `plan.md`): High. A new Shared contract, new columns on every table of every module, and two new required
headers on every signed-in write. Anonymous writes (login, registration, password reset) and reads are unchanged.
The times come from the client, so a user can backdate within 7 days. The stored clock error and received time keep
this visible for review.

Backlog: removes `backlog/offline/2026-10-26-record-client-action-time.md` (fully solved).

🤖 Generated with [Claude Code](https://claude.com/claude-code)
