---
name: tadweena-tasks
description: >
  Use this skill whenever you are about to call begin_task, finish_task, or implement a
  Tadweena task checkbox. Triggers on: starting implementation of a task, marking a task done,
  committing code, finish_task, begin_task, or any phrase like "let's implement X" / "I finished X"
  / "mark this task as done". ALWAYS read this before begin_task or finish_task — skipping it
  causes workflow integrity violations that the server will reject.
---

# Tadweena Task Execution

Full protocol: `../../../tadweena-protocol/tasks.md`

Read that file now. It covers:
- Mandatory begin_task → git → finish_task sequence
- No-commit completions and valid `reason` values
- blockId / docId source rules
- What the server rejects and why

Remember: `blockId` passed to `begin_task` is the executable `checkListItem`
block inside the Task document, not the document ID. Prefer using the explicit
`blockId` from `create_document`'s `blockMap` for determinism.
