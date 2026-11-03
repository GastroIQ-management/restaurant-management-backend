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

When I approve the plan (Gate 1), commit it:
1. Spec plan: I must be on the spec's branch `<type>/<spec-name>`; if not, stop and tell me.
   Backlog plan: if I am not on `<type>/<plan-folder-name>`, run `git fetch origin`, then switch
   to it if it exists, otherwise `git switch -c <type>/<plan-folder-name> origin/development`.
2. Set `**Status:**` in `plan.md` to `Approved`.
3. Stage only the plan folder (`plan.md` and `batches/`), by path, and commit:
   `docs(<scope>): add the plan for <feature>`.
4. Reply with one line: the commit hash and summary. Then stop.