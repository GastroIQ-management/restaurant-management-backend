---
description: Write the brief for a new module (step 0): what it holds, its relations, access and feature map
argument-hint: <module> [what it covers, in plain words]
---

Use the module-designer subagent to write the brief for this module:

$ARGUMENTS

If no argument was given, ask me for the module and stop.

Show me only the module designer's final message:

Brief ready: <path>
Roadmap rows added: <IDs>, or "None"
Backlog finding: <at most one: path — problem>, or "None" (then ask me in the chat; nothing is created until I say yes)
Decisions   the decisions I must make with their options, only when there are any

Add nothing else. Then stop and wait.

If I give answers or changes, send them to the module designer. It updates the brief, and the
files it keeps in step with it, only with those answers. Do not write specs, plans or code.

When I approve the brief (Gate 0), commit it:
1. Branch `docs/<YYYY-MM-DD>-<module>-module-brief` (today's date, the module name in lowercase).
   If I am not on it: `git fetch origin`, then switch to it if it exists, otherwise
   `git switch -c docs/<YYYY-MM-DD>-<module>-module-brief origin/development`. Never branch from
   the current branch or from a local `development` or `main`.
2. If the module's scope is not in the scope list of the root `CLAUDE.md` ("Commits"), add it.
3. Stage by path only: the brief, `docs/workflow/modules/context-map.md`, the roadmap files and
   `.claude/skills/domain/SKILL.md` the module designer changed, and `CLAUDE.md` when step 2
   changed it. Commit: `docs(<module>): add the module brief`.
4. Reply with one line: the commit hash and summary. Then stop.

The module's specs then start with `/spec`, one slice of the brief's feature map each.
