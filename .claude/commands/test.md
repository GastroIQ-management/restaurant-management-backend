---
description: Write the scenarios, stubs and failing tests for an approved plan (step 3)
argument-hint: <module>/<spec-name>
---

Use the tester subagent to write the tests for the plan in docs/workflow/$ARGUMENTS/

If no argument was given, ask me for the plan and stop.

If `plan.md` has `**Kind:** Setup`, stop and reply with one line: setup plans skip tests; run
`/implement` next.

When the tester finishes, use the test-reviewer subagent to check its work. Always send the
full report back to the **same** tester (continue it with SendMessage; never start a new one,
which would reload everything), whatever the verdict. On "Fix first", the tester fixes the
findings once. In both cases, the tester copies the reviewer's "Read closely" section into
`scenarios.md` and returns its final message.

Show me only the tester's final message: its backlog finding line, and the decisions I must
make with their options, or `Tests ready for Gate 2.` when there are none. Add nothing else. Then stop and wait (Gate 2).

If I give changes, send them to the tester. Do not implement features.

When I approve the tests (Gate 2), commit them. This is the baseline commit: tests are
read-only from here.
1. I must be on the feature's branch `<type>/<spec-name>`; if not, stop and tell me.
2. `dotnet build RestaurantManagement.sln` and `python3 .claude/scripts/check-security-matrices.py`
   must pass. If either fails, stop and show me the error.
3. Stage only what the tester wrote, by path: `scenarios.md`, the batch files, stubs, tests and
   security matrices. If anything else is uncommitted, leave it and tell me.
4. Commit: `test(<scope>): add failing tests for <feature>` with `Scenarios: 1–<N>` in the body,
   where `<N>` is the last scenario number in `scenarios.md`. Never guess it.
5. Reply with one line: the commit hash and summary. Then stop.