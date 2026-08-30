# Tadweena Document Protocol — Router

This file is a lightweight index. Read the operation-specific guide once per chat session (before your first create or edit in that session):

| Operation | Guide | Skill (Claude Code / Cursor / OpenCode / Codex) |
|---|---|---|
| `create_document` | [tadweena-protocol/create.md](create.md) | skill `tadweena-create` |
| `edit_blocks` | [tadweena-protocol/edit.md](edit.md) | skill `tadweena-edit` |

The MCP tool JSON schema defines field shapes at call time. The protocol files above
have copy-paste examples, hierarchy rules, and pitfalls. There is no `get_block_schema` tool.

## create_document / edit_blocks response (IDS)

Success returns an **IDS** array mapping block type → GUID in your input order.

- One `bulletList` with 3 items → three `{ "bulletListItem": "<guid>" }` entries
- One `task` block → `{ "task": "<guid>" }` — same GUID used by `begin_task` (stored as checkListItem)
- Use these GUIDs for `edit_blocks` (`blockId`, `afterBlockId`, `beforeBlockId`, `parentBlockId`) — never call `read_block` just to recover IDs from the same call

`edit_blocks` also returns a **MutationReceipt** with `affected[]` containing every touched block ID.

## Mnemonic Legend (read_block output only)

When reading tool output (not when writing):

- FORMAT: `[id|type|status/content|order]`
- TYPES: `p`=paragraph, `h`=heading, `li`=bulletListItem, `ol`=numberedListItem, `tk`=task (stored as checkListItem), `q`=quote
- STATUS: `[x]`=checked, `[ ]`=unchecked

## Anti-Pattern: search_knowledge for IDs

**NEVER call `search_knowledge` to re-discover IDs that the most recent `create_document`
or `edit_blocks` already returned.** The `blockMap` and `MutationReceipt.affected[]`
slots contain every `docId` and `blockId` you need. Re-running `search_knowledge`
for an ID you already have is wasted tokens.

Use `search_knowledge` only for: explicit duplicate research before creating a doc you
suspect may exist, finding IDs in PREVIOUS sessions, or recovering IDs when another
agent modified the document.

## Anti-Pattern: read_block after mutation

⛔ NEVER call `read_block` immediately after `create_document` or `edit_blocks` to verify
success or recover block IDs. Both tools return every ID you need in the response.
