---
description: Write the scenarios, stubs and failing tests for an approved plan (step 3)
argument-hint: <module>/<spec-name>
---

Use the tester subagent to write the tests for the plan in docs/plans/$ARGUMENTS/

If no argument was given, ask me for the plan and stop.

When the tester finishes, use the test-reviewer subagent to check its work. Always send the
full report back to the **same** tester (continue it with SendMessage; never start a new one,
which would reload everything), whatever the verdict. On "Fix first", the tester fixes the
findings once. In both cases, the tester copies the reviewer's "Read closely" section into
`scenarios.md` and returns its final message.

Show me only the tester's final message: the decisions I must make with their options, or
`Tests ready for Gate 2.` when there are none. Add nothing else. Then stop and wait (Gate 2).

If I give changes, send them to the tester. Do not implement features.