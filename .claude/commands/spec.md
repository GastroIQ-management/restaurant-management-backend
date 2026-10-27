---
description: Write only the spec for a requirement (step 1)
argument-hint: <requirement in plain words>
---

Use the spec-writer subagent to write a spec for this requirement:

$ARGUMENTS

If no argument was given, ask me for the requirement and stop.

Show me only the spec writer's final message:

Spec ready: <path>
Backlog finding: <at most one: path — problem>, or "None" (then ask me in the chat; nothing is created until I say yes)
Decisions   the decisions I must make with their options, only when there are any

Add nothing else. Then stop and wait.

If I give answers or changes, send them to the spec writer. It updates the spec only with
those answers. Do not plan or write code.