---
description: Write only the spec for a requirement (step 1)
argument-hint: <requirement in plain words>
---

Use the spec-writer subagent to write a spec for this requirement:

$ARGUMENTS

If no argument was given, ask me for the requirement and stop.

Show me only the spec writer's final message: the decisions I must make with their options, or
`Spec ready: <path>` when there are none. Add nothing else. Then stop and wait.

If I give answers or changes, send them to the spec writer. It updates the spec only with
those answers. Do not plan or write code.