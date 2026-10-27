---
name: backlog
description: Rules for work found outside the current task — when to fix inline, when it blocks, when to drop it, when to return it as a backlog finding, how to avoid duplicates, and the required file format. Read it when you have a finding outside the task, or are about to create or update a backlog item.
---

# Backlog rules

Fix it or drop it; the backlog is the last resort. Every item is work someone must read, decide
and finish, so a task should add **zero** items most of the time. Decide in this order:

**Never create a backlog file without asking the user in the chat first.** An agent returns
the finding in its final message, one line: path, problem, evidence; it never writes it to a
file. The main session asks the user in the chat whether to create it, and writes it only after
the user says yes, following this skill. Updating or deleting an item the current work solves
(Section 5) needs no approval.

**Agents never decide what to build.** Agents record only problems in what exists or is being
built: bugs, technical debt, and improvements to security, performance, scalability, code clarity
or reusability. A new capability, endpoint, flow, option or business rule is never an agent's
backlog item, however useful it looks: only the user adds those.

## 1. Decide what to do with a finding

| Finding | Action |
|---|---|
| It breaks the current spec's Outcomes or the current task's DoD (Definition of Done) | **Blocking.** Fix it in this task, or stop and report it as [Blocking]. Never send it to the backlog. |
| **Small fix:** about 10 lines or fewer, in the area this task already changes (same folder or the code it calls), with no new package, no breaking public API or contract change and nothing high risk | **Fix inline** (implementer, small-feature, small-batch) with a test when behaviour changes, and mention it in the Delivered note or final message. Never in test files after Gate 2. |
| **A feature idea:** a new capability, endpoint, flow, option or business rule nobody asked for | **Drop it.** Never write it to `features/` or disguise it as an improvement. The spec writer may list it under the spec's "Out of Scope"; other agents drop it |
| **Low severity:** style, naming, docs, test hygiene, "the rule could cover more", any reviewer Watch out | **Drop it.** Never a backlog item |
| **Worth a ticket:** a bug users will hit, a security or data problem, or something that blocks a planned feature, and too big to fix inline | **Return it** in the final message only. The file is written only after the user says yes in the chat (above) |

## 2. Search before creating

1. Search the backlog for the same file, class, endpoint or problem:
   `grep -ril "<keyword>" backlog/` with two or three keywords.
2. **Same problem already listed:** add your evidence and origin to that file instead of creating
   a new one.
3. **Related but different:** create the item and link the related file under "Related".

## 3. File name and folder

`backlog/<folder>/<YYYY-MM-DD>-<short-kebab-title>.md`, always with the date prefix.

| Folder | Use for |
|---|---|
| `bugs/` | Behaviour that differs from what was intended |
| `improvements/` | Works as designed, should be more secure, faster, more scalable, clearer or more reusable |
| `tech-debt/` | Internal quality: naming, missing tests, fragile design, docs |
| `pre-production/` | Must be done before launch, outside the code: deployment, certificates, secrets, infrastructure |
| `offline/` | API contracts the frontend offline engine needs (offline from day one, decided 2026-10-26) |

`features/` holds only items the user writes. Agents never create one there.

## 4. File template (every field required)

```markdown
# <Title>

**Status:** proposed
**Severity:** critical | high | medium | low
**Origin:** <spec or plan path> · <phase or "Phase 0 — Task N">
**Date:** YYYY-MM-DD

## Problem
What is wrong or missing, in plain words.

## Evidence
- `<file>:<line>` — what it shows

## Acceptance criteria
- [ ] <measurable result that proves it is fixed>

## Related
<other backlog files, or "None">
```

| Severity | Meaning |
|---|---|
| critical | Security, data leak between organizations, data loss |
| high | Wrong behaviour users will hit |
| medium | Wrong behaviour in edge cases, or blocks a planned feature |
| low | Quality, naming, docs |

## 5. Status

- New items are always written as **`proposed`**. Only the user changes it to **`accepted`**, merges items, or
  deletes them.
- When work fully solves an item, commit the fix with `Refs: <backlog file path>` in the body,
  then delete the item file but **never commit the
  deletion**: the user checks what was solved and commits it. Never push.
- When a spec fully solves an item, the spec lists it under "Backlog Impact" and the item is
  deleted, uncommitted, once that work is done. Partially solved: update Problem and Acceptance criteria to
  what is left.

## 6. Limits

- At most **one** finding per task, across every agent in the workflow (spec, plan, tests,
  implementation, review). More than one means the task's scope was wrong: say so instead.
- Severity `low` is never filed. Only `medium`, `high` or `critical` (Section 4).
- Never propose a backlog item for something you are not sure is a problem. Check it first.
