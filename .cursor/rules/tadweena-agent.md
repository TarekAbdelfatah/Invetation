---
description: Tadweena MCP workflow for Cursor
alwaysApply: true
---

# Tadweena Agent Rules (Cursor)

⛔ PROJECT BOUNDARY — HARD RULE
This file is instructions for the AI agent. It is NOT the project
identity file. The project identity file is `tadweena.md` in the
CURRENT WORKING DIRECTORY ONLY. NEVER confuse the two.

## Workflow

1. Read `tadweena.md` in the CURRENT WORKING DIRECTORY ONLY.
   ⛔ If missing, STOP and ask the user. NEVER walk up to parent folders.
   NEVER read sibling/parent projects. NEVER search the file system tree.

2. Use its `project_id` for all Tadweena MCP tool calls.

3. Generate one fresh `sessionId` (UUID v4) for this chat. Update only
   the `cursor` provider entry under `sessions`. Preserve other providers.

4. Prefer Tadweena graph tools BEFORE file search:
   `project_pulse`, `search_knowledge`, `query_code_graph`, `inspect_node`.

5. Call `begin_task(blockId, docId, modelName, sessionId)` BEFORE any
   implementation.

6. Before `create_document` → read `tadweena-protocol/create.md` (skill `tadweena-create`).
   Before `edit_blocks` → read `tadweena-protocol/edit.md` (skill `tadweena-edit`).

7. Complete tasks with `finish_task`, NEVER with `edit_blocks`.

## ⛔ Anti-Pattern Guards

- NEVER call `read_block` immediately after `create_document`. Use `blockMap` IDs
  from the response directly. Only read to recover stale/previous-session IDs.
- NEVER call `edit_blocks` to fix TDS that could have been correct in the
  original `create_document` call.
- NEVER use bare filenames in `target` or `fileLinks` fields; always use repo-relative paths.

## Hard Stops

- Never fabricate `projectId`, `docId`, `blockId`, `sessionId`, or `gitHash`.
- Never fabricate task status or commit hashes.
- Never walk up to parent folders for ANY reason.
- Never read files from outside the current project folder.
- Never use `..` (parent directory references) in file paths.
- Never complete tasks via `edit_blocks` — only `finish_task`.

## No-Commit Completion

`finish_task.gitHash` is optional. Provide it ONLY for a real commit.
Otherwise provide `reason`:
`data_migration | manual_done | external_work | pre_existing | micro_task`
