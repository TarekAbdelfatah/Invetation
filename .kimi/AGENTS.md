<!-- TADWEENA-KIMI-AGENTS-START -->
<!-- Synced: 2026-08-30T16:26:57.511016+00:00 -->
# Tadweena for Kimi

Keep reasoning private and tool inputs precise.

- Read `tadweena.md` in the CURRENT WORKING DIRECTORY ONLY for `project_id`.
  ⛔ HARD RULE: NEVER walk up to parent folders. NEVER read sibling/parent projects.
- Generate one `sessionId` per chat.
- Use `project_pulse` before broad work.
- Before `create_document` → read `tadweena-protocol/create.md`.
- Before `edit_blocks` → read `tadweena-protocol/edit.md`.
- Use `begin_task` before implementation.
- Finish with `finish_task`.
- `gitHash` is optional; use it only for a real commit. Otherwise pass `reason`.
- For tasks with no commit and no implementation step, prefer `quick_complete`
  over the full lifecycle. Valid reasons: `data_migration`, `manual_done`,
  `external_work`, `pre_existing`, `micro_task`. `quick_complete` rejects
  tasks with active `InProgress` ticks.

Never fabricate IDs or commit hashes.

## ⛔ Anti-Pattern Guards

- NEVER call `read_block` immediately after `create_document`. Use `blockMap` IDs
  from the response directly. Only read to recover stale/previous-session IDs.
- NEVER call `edit_blocks` to fix TDS that could have been correct in the
  original `create_document` call.
- NEVER use bare filenames in `target` or `fileLinks` fields; always use repo-relative paths.

<!-- TADWEENA-KIMI-AGENTS-END -->


