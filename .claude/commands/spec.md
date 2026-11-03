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

When I approve the spec, commit it:
1. Branch `<type>/<spec-name>` (`feat` for a feature, `fix` for a fix; `<spec-name>` is the spec
   file name without `.md`). If I am not on it: `git fetch origin`, then switch to it if it
   exists, otherwise `git switch -c <type>/<spec-name> origin/development`. Never branch from the
   current branch or from a local `development` or `main`.
2. Stage only the spec file, by path, and commit: `docs(<scope>): add the spec for <feature>`.
   Backlog files the spec writer updated or deleted stay uncommitted; I commit them myself.
3. Reply with one line: the commit hash and summary. Then stop.