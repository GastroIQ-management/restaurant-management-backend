# Identity data has no automatic organization filter

**Status:** proposed
**Severity:** medium
**Origin:** docs/plans/shared/2026-10-27-client-generated-ids-on-create/plan.md · B4, T7
**Date:** 2026-10-28

## Problem
The root `CLAUDE.md` says tenant data is filtered by organization automatically (EF Core global
query filters). Menu and Organization do this; Identity does not. Every Identity query on
organization-owned rows (roles, role levels, member access, invitations and their grids) must add
`OrganizationId == organizationId` by hand. Today every query does, so no leak is known. But a
future query that forgets the condition would show one organization's data to another, for
example a "list pending invitations" query showing Cantina's manager the emails Trattoria invited,
and nothing would catch it.

Adding the filter is not a one-line change: registration, verification, login, refresh and
accepting an invitation from the email link run with no organization in the token, so each needs
a reviewed `IgnoreQueryFilters` place listed in the root `CLAUDE.md`. Auth change: high risk.

## Evidence
- `src/RestaurantManagement.Modules.Identity/Data/IdentityDbContext.cs` — never calls
  `ApplyTenantQueryFilters`
- `src/RestaurantManagement.Modules.Organization/Data/OrganizationDbContext.cs:64` and
  `src/RestaurantManagement.Modules.Menu/Data/MenuDbContext.cs:26` — both do
- `src/RestaurantManagement.Modules.Identity/Services/Invitations/InvitationService.cs:88-90` and
  `:259-260` — organization condition written by hand
- `tests/RestaurantManagement.Web.Tests/Identity/InvitationWebSupport.cs:129-131` — the test helper
  assumed the filter existed and returned every organization's invitations (fixed by hand in T7)

## Acceptance criteria
- [ ] Organization-owned Identity entities are filtered by the token's organization automatically
- [ ] Flows that run without an organization in the token (register, verify, login, refresh,
      accept invitation) still work, each bypass listed in the root `CLAUDE.md`
- [ ] A test proves an Identity query without an organization condition returns no other
      organization's rows

## Related
None
