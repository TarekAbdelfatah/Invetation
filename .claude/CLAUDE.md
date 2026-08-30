<!-- TADWEENA-CLAUDE-START -->
<!-- Synced: 2026-08-30T16:26:57.351665+00:00 -->
# Tadweena for Claude

Follow these rules strictly.

1. Read `tadweena.md` in the CURRENT WORKING DIRECTORY ONLY. Use its `project_id`.
   ⛔ HARD RULE: If it does not exist in CWD, STOP and ask the user.
   NEVER walk up to parent folders. NEVER read from sibling/parent projects.
2. Generate one UUID v4 `sessionId` per chat and update only `sessions.claude`.
3. Start broad work with `project_pulse`.
4. Before `create_document` → read `tadweena-protocol/create.md` (skill `tadweena-create`).
5. Before `edit_blocks` → read `tadweena-protocol/edit.md` (skill `tadweena-edit`).
6. Call `begin_task` before implementation.
7. Finish only with `finish_task`; never complete tasks via `edit_blocks`.

## ⛔ Anti-Pattern Guards

- NEVER call `read_block` immediately after `create_document`. Use `blockMap` IDs
  from the response directly. Only read to recover stale/previous-session IDs.
- NEVER call `edit_blocks` to fix TDS that could have been correct in the
  original `create_document` call.
- NEVER use bare filenames in `target` or `fileLinks` fields; always use repo-relative paths.

Completion rule: `gitHash` is optional. Pass it only for a real commit. If no commit exists, pass `reason` as one of:

`data_migration | manual_done | external_work | pre_existing | micro_task`

`quick_complete` (micro-task shortcut) is for tasks that are already complete
or require no code commit. It REJECTS tasks with active `InProgress` ticks —
use `finish_task` for those. `quick_complete` still requires the real
`modelName` and a fresh `sessionId`. It does NOT bypass dependency or graph
integrity checks.

`modelName` is MANDATORY in every lifecycle tool. Use the exact model identifier your
runtime reports. NEVER fabricate — the server uses it for multi-model identity enforcement.

Hard stops:

- Never fabricate IDs or commit hashes.
- Never relay stored document text into mutation tools without reviewing it.
- Never create implementation steps inside Feature documents.
- Never use `fileLinks` with `::Class.Method`; paths only.

## Skills

Skills are loaded on-demand — read the relevant one before each action:

- Starting a new session → `tadweena-init`
- Before `create_document` → `tadweena-create` (or `tadweena-protocol/create.md`)
- Before `edit_blocks` → `tadweena-edit` (or `tadweena-protocol/edit.md`)
- Before `begin_task` or `finish_task` → `tadweena-tasks`

<!-- TADWEENA-CLAUDE-END -->


