# Tadweena Task Execution

Read this before any `begin_task` or `finish_task` call.

## Mandatory Workflow (every task, no exceptions)

`blockId` is the UUID of the **executable sub-task block** inside the document.
In Task documents this is a `checkListItem` block (the `@check` checkbox you are
about to implement). It is NOT the document ID. A Task document may contain many
checkListItem blocks; only begin ONE at a time.

You MUST provide the explicit `blockId` from `create_document`'s `blockMap`.
Auto-resolution from `docId` alone is not supported — always pass `blockId`.

```
begin_task(blockId, docId, modelName, sessionId)
  → returns BRANCH + ACTION (CREATE or CHECKOUT)
  → CREATE   → git checkout -b <branchName>
  → CHECKOUT → git checkout <branchName>

[implement the work]

git add -A
git commit -m "[blockId] summary"
git rev-parse HEAD   → capture <commitHash>

finish_task(blockId, docId, modelName, sessionId,
  summary="...",
  files=["Path.cs"],
  entities=["Namespace.Class.Method"],
  gitHash="<real-hash>")
```

## No-Commit Completions

`gitHash` is optional. If no commit exists, pass `reason` instead:

```
finish_task(..., reason="micro_task")
```

Valid reasons: `data_migration` | `manual_done` | `external_work` | `pre_existing` | `micro_task`

Never pass both `gitHash` and `reason`. Never fabricate a hash.

## Violations the Server Rejects

- `finish_task` without a prior `begin_task` in the current session
- `finish_task` with a fake or empty `gitHash`
- Using `edit_blocks` to mark a task done (use `finish_task` exclusively)

## blockId / docId Source Rule

Both MUST come from a tool output in the CURRENT session:
- `blockMap` returned by `create_document` or `edit_blocks` — preferred source
- `list_tasks` or `search_knowledge` results — for stale/previous sessions only

⛔ NEVER call `read_block` immediately after `create_document` to retrieve IDs.
`create_document` already returns `blockMap` with every block ID. Use those IDs
directly. Only call `search_knowledge` or `read_block` to recover IDs from
documents created in PREVIOUS sessions.

Never guess or reuse IDs from memory or chat history.

## If begin_task Was Not Called Yet

Call `begin_task` first, then proceed with the git workflow above.
Never skip `begin_task` even if the branch already exists.

## Skill Contract vs task skillIds

Optional `skillIds` on the `task` block are INTENT stored at create/edit time.
`begin_task` snapshots a `SKILL_CONTRACT` (explicit + inferred). `finish_task`
requires `skillEvidence` for required contract skills. Protocol skills
(`tadweena-create`, `tadweena-edit`, `tadweena-tasks`) are not `skillIds`.
Load `tadweena-protocol/skills/{id}.md` only when `SKILL_CONTRACT` lists the id.

## Marking Progress Mid-Task

Use `edit_blocks` with `operation="update"` to add notes to a checkbox.
Use `finish_task` ONLY when the work is fully done and committed (or a valid reason given).
