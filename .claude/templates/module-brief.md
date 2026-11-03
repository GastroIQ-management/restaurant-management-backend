# Module brief: <Module>

**Phase:** <n> (`docs/roadmap/phase-<n>.md`)   **Date:** YYYY-MM-DD   **Requested by:** <name>

## Purpose
What problem this module solves, for whom, and why the business needs it now. 3–5 lines.

## Glossary
Terms this module introduces. Each one is also added to the domain skill's glossary.

| Term | Meaning |
|---|---|
| <term> | <plain-words meaning> |

## What it holds
The business things this module owns. Nothing here is owned by another module.

| Thing | What it is | Belongs to | Life cycle |
|---|---|---|---|
| <thing> | <one line> | Organization / Location | <states and how it moves between them, e.g. Free → Occupied → Free> |

## Relations with other modules
One row per relation, both directions. Business words only: what is needed and why, never how.

| Module | Direction | What passes | Why |
|---|---|---|---|
| <module> | Uses | <what this module needs from it> | <business reason> |
| <module> | Used by | <what that module needs from this one> | <business reason> |

## Who may do what
Access is set per location and access area (None, Read or Manage). The Owner may do everything.

| Action | Access area | Level | Roles with it by default | Audit log |
|---|---|---|---|---|
| <action> | <area, e.g. Orders> | Read / Manage | <Admin, Manager, Cashier, Waiter, Kitchen> | Yes / No |

## Business rules
| # | Rule |
|---|---|
| 1 | <rule: what the module must do or refuse> |

Every brief answers each line below, even with "Does not apply":
- **Money:** amounts in the organization's currency (BRL, USD or CAD); rounding, discounts, refunds
- **Time:** which moments use the location's time zone and business day
- **Offline:** what a device can do with no connection (up to 7 days), and what waits for the server
- **Ending things:** what is closed, archived or deactivated instead of deleted, and what history is kept
- **Audit log:** which actions are logged (already marked in "Who may do what")

## Out of scope
| Item | Where it belongs |
|---|---|
| <item> | <other module, later phase, or not planned> |

## Feature map
Each slice is one behaviour: one future spec, reviewable in about 5 minutes. Build order and
status live only in the roadmap; this table holds neither.

| Roadmap ID | Slice | Needs first |
|---|---|---|
| P<n>-<nn> | <one behaviour, plain words> | <roadmap IDs, or —> |

## Open questions
- **[Blocking]** <question> — options: <A> / <B>. Recommended: <A>, because <reason>
- **[confirm]** <business fact still open; treated as open, not as a rule>

## Assumptions
- <assumption> — <reason>
