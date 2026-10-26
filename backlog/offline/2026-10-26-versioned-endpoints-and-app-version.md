# Version the endpoints so queued writes survive a backend deploy

**Status:** proposed
**Severity:** medium
**Origin:** User decisions of 2026-10-26 on offline-first (backend conversation) · compatibility
**Date:** 2026-10-26

## Problem
A device may hold queued writes built by an older app version for up to 7 days. If a deploy
changes a contract, those writes fail on replay. Endpoints have no version today. This matters
after launch, but the version prefix is cheap to add now, while there are few endpoints.

## Evidence
- No endpoint carries a version (no `/v1/` route or versioning setup in `src/`)
- No request carries the app version

## Acceptance criteria
- [ ] Endpoints are versioned; changes inside a version are additive only
- [ ] Every write carries the app version
- [ ] Each old version is kept for at least 30 days after the next one ships, longer than the
      7-day offline limit
- [ ] The OpenAPI document is published per version

## Related
- `backlog/offline/2026-10-26-problem-details-with-stable-error-codes.md`
