---
name: tadweena-edit
description: "Read once per chat session before the first edit_blocks in Tadweena. Triggers on adding, updating, deleting, or moving document blocks; setting tags or summary; choosing blockId and position."
---

# Tadweena edit_blocks — One-Shot Guide

Read this file once per chat session before your first `edit_blocks` in that session. The MCP JSON schema defines field shapes at call time; this file is the canonical guide for operations, positions, and examples. For later edits in the same session, apply the One-Shot Checklist from memory — re-read only after a validation rejection or when using an unfamiliar operation.

## One-Shot Checklist

Before calling `edit_blocks`, verify every item:

1. `projectId` — from `tadweena.md` → `project_id`. Never guess.
2. `sessionId` — same UUID used for this chat session.
3. `edit.docId` — document GUID from a prior `create_document` or `search_knowledge` (previous session).
4. `blockId` / `afterBlockId` / `beforeBlockId` / `parentBlockId` — from `blockMap` or `MutationReceipt.affected[]` in the CURRENT session. Never guess.
5. Never call `read_block` immediately after `create_document` or `edit_blocks` just to get IDs — the response already has them.
6. Cap: at most **30 `add` ops** per call.
7. On validation failure — read this file and retry once. Do not iterate blindly.
8. Never use `edit_blocks` to mark a task done — use `finish_task`.
9. Lists — `items`, `header`, `fileLinks` are **arrays of plain strings**. Never send `bulletListItem`.
10. Task blocks in `add`/`update` — same rules as create: `why` ≥40 chars, non-empty `target`, `code` when required.

## Tool Signature

```text
edit_blocks(
  modelName:string,
  sessionId:string,      // GUID
  projectId:string,      // GUID from tadweena.md project_id
  edit:object            // { docId, edits: [EditOp, ...] }
)
```

The `edit` parameter is a single nested object. Do NOT flatten `docId` or `edits` to top-level parameters.

## DocumentEdit envelope

```json
{
  "docId": "<document-guid>",
  "edits": [
    { "op": "add", "position": "end", "block": { "type": "paragraph", "text": "..." } },
    { "op": "update", "blockId": "<id>", "block": { "type": "paragraph", "text": "..." } },
    { "op": "delete", "blockId": "<id>" },
    { "op": "move", "blockId": "<id>", "position": "start" },
    { "op": "set_tags", "tags": ["a", "b"] },
    { "op": "set_summary", "summary": "New summary." }
  ]
}
```

## Operations (6)

| op | required fields | purpose |
|---|---|---|
| `add` | `block`, `position` (default `end`) | Insert a new block |
| `update` | `blockId`, `block` | Replace block content |
| `delete` | `blockId` | Remove a block |
| `move` | `blockId`, `position` | Relocate a block |
| `set_tags` | `tags` (string[]) | Replace document tags |
| `set_summary` | `summary` (string) | Replace document summary |

Per-op fields:

- `blockId` — GUID. Required for `update` / `delete` / `move`.
- `block` — TDS JSON block. Required for `add` / `update`.
- `position` — `end` | `start` | `after` | `before` | `under_parent`. Default `end`.
- `afterBlockId` — GUID. Required when `position="after"`.
- `beforeBlockId` — GUID. Required when `position="before"`.
- `parentBlockId` — GUID. Required when `position="under_parent"`.
- `tags` — string[]. Required when `op="set_tags"`.
- `summary` — string. Required when `op="set_summary"`.

Cap: at most **30 `add` ops** per call (130 total blocks across all add ops).

## Positions (5)

```text
ADD:    edit.edits = [{ op: "add",    block, position }]
UPDATE: edit.edits = [{ op: "update", block, blockId }]
DELETE: edit.edits = [{ op: "delete", blockId }]
MOVE:   edit.edits = [{ op: "move",   blockId, position }]
```

- `end` — append at document root (default)
- `start` — prepend at document root
- `after` — insert after `afterBlockId`
- `before` — insert before `beforeBlockId`
- `under_parent` — nest as child of `parentBlockId` (toggle body, etc.)

## Where blockId comes from

Use `blockMap` returned by `create_document` or `MutationReceipt.affected[]` from a
previous `edit_blocks` call in the **same session**.

⛔ NEVER call `read_block` immediately after `create_document` or `edit_blocks` just to
retrieve block IDs or confirm created content. The response already contains every
block ID you need.

Only call `search_knowledge` or `read_block` to recover IDs from documents created in a
**PREVIOUS session**, or when another agent modified the document after creation.

Do not use `wire_graph` for `touches_file` or `part_of`; those are auto-wired from
`fileLinks` and `parentDocumentId`. Use `wire_graph` only for model-inferred
relations such as `requires`, `blocks`, `solution_for`, and `decision`.

## LIST FIELD SHAPES (most common failures)

- `items`, `header`, `fileLinks`, `skillIds` → JSON **array of plain strings**
  - OK: `"items":["a","b"]`
  - WRONG: `"items":"a"` (single string)
  - WRONG: `"items":[{"text":"a"}]` (array of objects)
- In `edit_blocks`, send `type:"bulletList"` with `items[]` — never send `bulletListItem`.

## edit_blocks response (MutationReceipt)

Success returns a **MutationReceipt** with:

- `affected[]` — every block touched, with `blockId`, `type`, `canBeginTask`
- `blockMap` — full map when document is small enough
- Use `affected[].blockId` for subsequent edits — never re-read the document

## Full Example: edit_blocks add

```text
edit_blocks(
  modelName="<your-model-id>",
  sessionId="<your-fresh-session-uuid>",
  projectId="<your-project-id-from-tadweena.md>",
  edit={
    "docId": "<your-task-doc-id>",
    "edits": [
      {
        "op": "add",
        "position": "end",
        "block": {
          "type": "task",
          "title": "Add input validation to VectorSearchService.SearchAsync",
          "checked": false,
          "why": "Empty queries currently propagate toward the embedding provider, so explicit validation keeps failures close to the public service boundary.",
          "target": "BlazorTadweenaAi.Services/Search/VectorSearchService.cs::VectorSearchService.SearchAsync",
          "code": {
            "language": "csharp",
            "source": "Validation().ProjectId(projectId).Query(query).Limit(limit);"
          }
        }
      }
    ]
  }
)
```

## Full Example: edit_blocks update

```text
edit_blocks(
  modelName="<your-model-id>",
  sessionId="<your-fresh-session-uuid>",
  projectId="<your-project-id-from-tadweena.md>",
  edit={
    "docId": "<your-task-doc-id>",
    "edits": [
      {
        "op": "update",
        "blockId": "<your-block-id-from-blockmap>",
        "block": {
          "type": "task",
          "title": "Add input validation to VectorSearchService.SearchAsync",
          "checked": false,
          "why": "Updated: empty and whitespace queries must be rejected before calling the embedding provider to preserve a clear public API contract.",
          "target": "BlazorTadweenaAi.Services/Search/VectorSearchService.cs::VectorSearchService.SearchAsync",
          "code": {
            "language": "csharp",
            "source": "public async Task<IReadOnlyList<SearchResult>> SearchAsync(int projectId, string query, int limit, CancellationToken ct) { Validation().ProjectId(projectId).Query(query).Limit(limit); var embedding = await _embeddingService.GetEmbeddingAsync(query, ct); return await _vectorStore.SearchAsync(projectId, embedding, limit, ct); }"
          }
        }
      }
    ]
  }
)
```

## Full Example: edit_blocks with nested toggle child

```text
edit_blocks(
  modelName="<your-model-id>",
  sessionId="<your-fresh-session-uuid>",
  projectId="<your-project-id-from-tadweena.md>",
  edit={
    "docId": "<your-doc-id>",
    "edits": [
      {
        "op": "add",
        "position": "end",
        "block": {
          "type": "toggle",
          "title": "Implementation notes",
          "body": [
            { "type": "paragraph", "text": "Decision log entry." },
            { "type": "bulletList", "items": ["Approach A", "Approach B"] },
            { "type": "code", "language": "csharp", "source": "// snippet" }
          ]
        }
      }
    ]
  }
)
```

## Full Example: edit_blocks position after

```text
edit_blocks(
  modelName="<your-model-id>",
  sessionId="<your-fresh-session-uuid>",
  projectId="<your-project-id-from-tadweena.md>",
  edit={
    "docId": "<your-doc-id>",
    "edits": [
      {
        "op": "add",
        "position": "after",
        "afterBlockId": "<existing-block-id-from-blockmap>",
        "block": {
          "type": "paragraph",
          "text": "Inserted immediately after the referenced block."
        }
      }
    ]
  }
)
```

## Full Example: edit_blocks set_tags and set_summary

```text
edit_blocks(
  modelName="<your-model-id>",
  sessionId="<your-fresh-session-uuid>",
  projectId="<your-project-id-from-tadweena.md>",
  edit={
    "docId": "<your-doc-id>",
    "edits": [
      { "op": "set_tags", "tags": ["search", "embedding", "service"] },
      { "op": "set_summary", "summary": "Updated summary covering what changed and why." }
    ]
  }
)
```

## Server Rejections (common)

| Code | Cause | Fix |
|---|---|---|
| `TASK_WHY_SHORT` | `why` < 40 chars | Expand to ≥40 characters |
| `TASK_TARGET_EMPTY` | `target` empty | Add repo-relative path or class.method |
| `TASK_MANUAL_NUMBER` | title starts with `1.` / `2)` / `Step 1:` | Remove manual numbering |
| `TASK_CODE_REQUIRED` | title references code without `code` block | Add `code` field |
| `BARE_FILENAME` | `fileLinks` has bare filename (warning) | Use repo-relative paths |
