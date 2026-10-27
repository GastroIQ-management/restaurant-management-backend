---
description: Write the plan for an approved spec or a backlog item (step 2): plan.md plus batch files
argument-hint: <module>/<spec-name> | backlog/<folder>/<item>.md
---

Input: $ARGUMENTS

If no input was given, ask me for it and stop.

- **Spec** (`<module>/<spec-name>`): use the planner subagent to write the plan for
  `docs/specs/$ARGUMENTS.md`.
- **Backlog item** (a path starting with `backlog/`): check the file exists and has Problem and
  Acceptance criteria; if not, stop and tell me what is missing. Otherwise use the planner
  subagent to write the plan for that backlog item, with no spec. The planner picks the module
  and writes the plan folder `docs/plans/<module>/<YYYY-MM-DD>-<item-name>/` (today's date, the
  backlog file name without its date and `.md`), so `/test` and `/implement` work on it as usual.

Show me only the planner's final message: its backlog finding line, and the decisions I must
make with their options, or `Plan ready.` when there are none. Add nothing else. Then stop and wait.

If I give changes, send them to the planner. The planner may create only its plan folder
(plan.md and batches/), never backlog files.