---
name: tadweena-create
description: "Read once per chat session before the first create_document in Tadweena. Triggers on creating a Feature, Task, Bug, Rule, Architecture, Decision, or General document; choosing parentDocumentId; writing TDS body blocks."
---

# Tadweena create_document — One-Shot Guide

Read this file once per chat session before your first `create_document` in that session. The MCP JSON schema defines field shapes at call time; this file is the canonical guide for hierarchy, pitfalls, and copy-paste examples. For later creates in the same session, apply the One-Shot Checklist from memory — re-read only after a validation rejection or when creating an unfamiliar document type. There is no `get_block_schema` tool.

## One-Shot Checklist

Before calling `create_document`, verify every item:

1. `projectId` — from `tadweena.md` → `project_id`. Never guess.
2. `sessionId` — fresh UUID for this chat. Record in `tadweena.md` before the first tool call.
3. `document.type` — pick the correct type (Feature, Task, Bug, General, etc.).
4. `document.parentDocumentId` — see table below. Never guess a parent GUID.
5. `document.summary` — 1–3 lines, max 80 words: WHAT / WHY / WHO.
6. `document.tags` — 3–7 tags. Prefer existing project tags (`get_project_tags`).
7. `document.body` — ordered blocks. First root block must be a `paragraph` ≥40 chars (Feature/Task/Bug).
8. **Flat arrays preferred** — `tags` and `body` should be single-level JSON arrays. Some MCP clients double-wrap; the server auto-flattens and saves (warning `JSON_REPAIR_SHAPE`, no retry needed).
8. Lists — `items`, `header`, `fileLinks`, `skillIds` are **arrays of plain strings**, never objects.
9. Task blocks — `why` ≥40 chars, non-empty `target`, `code` when title/target names files or methods. Optional `skillIds` = task INTENT (not HOW). Missing `skillIds` = infer-only at `begin_task`.
10. Paths — repo-relative in `target` and `fileLinks`. Never bare filenames like `Service.cs`.
11. Do NOT call `search_knowledge` first — duplicate detection is server-side.
12. After success — use returned `blockMap` IDs directly. Never call `read_block` to verify.
13. Do not put Skill IDs in `document.tags`. `tadweena-create` / `tadweena-edit` / `tadweena-tasks` are protocol skills, not `skillIds`.

## Tool Signature

```text
create_document(
  modelName:string,
  sessionId:string,      // GUID (xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx)
  projectId:string,      // GUID from tadweena.md project_id
  document:object        // { schema, title, summary, type, tags, parentDocumentId, body }
)
```

The `document` parameter is a single nested object. Do NOT flatten `title`, `summary`,
`tags`, `type`, or `parentDocumentId` to top-level parameters.

### Document envelope

```json
{
  "schema": "tadweena-document/1.0",
  "title": "Implement VectorSearchService query flow",
  "summary": "Adds the semantic search service that validates the request, embeds the query, and returns ranked results.",
  "type": "Task",
  "tags": ["search", "embedding", "service", "tests"],
  "parentDocumentId": "<feature-doc-id>",
  "body": [ /* ordered blocks */ ]
}
```

Valid types: `Feature` | `Bug` | `Task` | `UserStory` | `Rule` | `Architecture` | `Decision` | `Idea` | `General`

## Document Hierarchy

```text
Project  → vision, goals, architecture. No task/code blocks. (created via setup_project)
Feature  → business analysis narrative. ≥6 root blocks. No task/code blocks.
Task     → one or more executable `task` blocks.
Bug      → error report under a Task document.
```

`parentDocumentId` is mandatory for child documents:

- Feature: `parentDocumentId = projectId`
- Task: `parentDocumentId = featureDocId`
- Bug: `parentDocumentId = taskDocId`

Duplicate detection is enforced inside `create_document` via server-side title + parent +
type matching. You do NOT need to call `search_knowledge` before creating. Use it only for
explicit duplicate research or recovering IDs from previous sessions.

## parentDocumentId — which ID to pass

| Creating | `type` | `parentDocumentId` | Where the parent ID comes from |
|---|---|---|---|
| Business feature spec | `Feature` | **`projectId`** | `tadweena.md` → `project_id` (same GUID as `projectId` param) |
| Implementation plan | `Task` | **Feature doc id** | `create_document` response → `id:<guid>` from Feature create |
| Bug report | `Bug` | **Task doc id** | `create_document` response → `id:<guid>` from Task create |
| Standalone reference | `General`, `Rule`, `Architecture`, `Decision`, `Idea`, `UserStory` | `null` | No parent |
| Project vision doc | `Project` | `null` | Created only via `setup_project` bootstrap |

Common mistakes:

- `Task` with `parentDocumentId = projectId` → rejected (needs Feature parent)
- `Feature` with `parentDocumentId = null` → rejected (needs Project parent)
- Guessing a parent GUID → always use an id returned by a prior `create_document` in this session

## LIST FIELD SHAPES (most common failures)

- `items`, `header`, `fileLinks`, `skillIds` → JSON **array of plain strings**
  - OK: `"items":["a","b"]`
  - WRONG: `"items":"a"` (single string)
  - WRONG: `"items":[{"text":"a"}]` (array of objects)
- `rows` → array of arrays of strings; each row length must equal `header.length`
- In `create_document`, send `type:"bulletList"` with `items[]` — the server flattens to storage items. Never send `bulletListItem` in create payloads.

## Tag Reuse Policy

**STRONG REUSE BIAS.** Always prefer an existing project tag over creating a new one.
New tag creation should be rare — every new tag dilutes the graph. If only 2–3 existing
tags fit, use 2–3. If unsure which tags exist, call `get_project_tags` first.

## Summary Contract

1–3 lines covering **WHAT** / **WHY** / **WHO**. Max 80 words. The body holds the rest.

## Block Types (12)

Discriminated by the `type` field. The MCP schema enforces required fields per type.

| type | required fields | optional fields |
|---|---|---|
| `paragraph` | `text` | — |
| `heading` | `level` (1-6), `text` | — |
| `bulletList` | `items[]` (string[]) | — |
| `numberedList` | `items[]` (string[]) | `start` (int, default 1) |
| `quote` | `text` | — |
| `divider` | — | — |
| `code` | `language`, `source` | — |
| `toggle` | `title`, `body[]` (children) | — |
| `task` | `title`, `checked`, `why` (≥40 chars), `target` | `code`, `risk`, `fileLinks`, `skillIds`, `body` |
| `image` | `url` (https) | `caption` |
| `attachment` | `url` (https) | `name` |
| `table` | `header[]` (string[]), `rows[][]` (string[][]) | — |

Quick shapes:

```text
paragraph    {"type":"paragraph","text":"..."}
heading      {"type":"heading","level":3,"text":"..."}
bulletList   {"type":"bulletList","items":["a","b"]}
numberedList {"type":"numberedList","start":1,"items":["a"]}
quote        {"type":"quote","text":"..."}
divider      {"type":"divider"}
code         {"type":"code","language":"csharp","source":"..."}
toggle       {"type":"toggle","title":"...","body":[...]}
image        {"type":"image","url":"https://...","caption":"optional"}
attachment   {"type":"attachment","url":"https://...","name":"optional"}
table        {"type":"table","header":["Col1","Col2"],"rows":[["a","b"]]}
task         {"type":"task","title":"...","checked":false,"why":"≥40 chars","target":"path/file.cs","code":{...},"fileLinks":["path"],"skillIds":["eng.testing","lang.csharp"]}
```

## Feature Documents

Feature documents are business analysis only.

Required content: WHAT, WHY, WHO, HOW it fits, WHEN it runs, RULES/constraints.

Forbidden in Feature documents: `task` blocks, `code` blocks, implementation snippets.

Minimum: 6 root blocks. First root block must be a `paragraph` with at least 40 characters.

## Task Documents

Task documents contain executable work. First block must be a `paragraph` ≥40 chars.

A single Task document may contain many executable `task` blocks. Each must be complete.

Every executable task block:

```json
{
  "type": "task",
  "title": "Plain task title without manual numbering",
  "checked": false,
  "why": "At least 40 characters explaining why the task matters and what fails without it.",
  "target": "Repo-relative path or path::Class.Method naming the exact target.",
  "code": {
    "language": "csharp",
    "source": "// Required when the task title or target names files, classes, methods, or implementation."
  },
  "fileLinks": ["Services/Search/VectorSearchService.cs"],
  "skillIds": ["eng.testing", "lang.csharp"]
}
```

Target examples:

```text
BlazorTadweenaAi.Services/Search/VectorSearchService.cs
BlazorTadweenaAi.Services/Search/VectorSearchService.cs::VectorSearchService
BlazorTadweenaAi.Services/Search/VectorSearchService.cs::VectorSearchService.SearchAsync
```

`fileLinks` are file paths only. Never put `::` in `fileLinks`.

### Task skillIds (intent, optional)

`skillIds` declare which **engineering/language** disciplines apply to this task. They are not the HOW body. Missing `skillIds` keeps infer-only behavior at `begin_task`. Do not reuse `document.tags`. Do not use protocol skill names (`tadweena-create`, `tadweena-edit`, `tadweena-tasks`) here.

Shipped IDs (same list as `tadweena-protocol/skills/*.md` and the daemon bundler):

```text
eng.security     — Authentication, authorization, secrets, crypto and security-sensitive changes.
eng.code-review  — Correctness, maintainability, architecture violations and project conventions.
eng.testing      — Validate changed behavior and appropriate test coverage.
eng.architecture — Boundaries, dependencies and architectural impact.
eng.clean-code   — Hygiene and safe edit patterns before writing code.
lang.csharp      — C# language conventions and safe patterns.
```

`begin_task` merges explicit `skillIds` with file/path/AST inference into a locked `SKILL_CONTRACT`. Load `tadweena-protocol/skills/{id}.md` only when that contract lists the id. There is no `list_skills` tool.

## Bug Documents

Bug documents belong under the task document that owns the failing work.

Required shape:

```json
{
  "body": [
    {
      "type": "paragraph",
      "text": "Exact error, observed behavior, stack trace, or failing command. Include enough context to reproduce the failure."
    },
    {
      "type": "code",
      "language": "csharp",
      "source": "// The failing code or the smallest useful snippet."
    },
    {
      "type": "paragraph",
      "text": "Root cause analysis, attempted fixes, and the current hypothesis."
    }
  ]
}
```

## create_document response (IDS / blockMap)

Success returns an **IDS** array mapping block type → GUID in your input order.

- One `bulletList` with 3 items → three `{ "bulletListItem": "<guid>" }` entries
- One `task` block → `{ "task": "<guid>" }` — same GUID used by `begin_task` (stored as checkListItem)
- Use these GUIDs for `edit_blocks` — never call `read_block` just to recover IDs from the same call

## Server Rejections

| Code | Cause | Fix |
|---|---|---|
| `TASK_WHY_SHORT` | `why` < 40 chars | Expand to ≥40 characters |
| `TASK_TARGET_EMPTY` | `target` empty | Add repo-relative path or class.method |
| `TASK_MANUAL_NUMBER` | title starts with `1.` / `2)` / `Step 1:` | Remove manual numbering |
| `TASK_CODE_REQUIRED` | title references code without `code` block | Add `code` field |
| `TASK_SKILL_UNKNOWN` | `skillIds` contains an id not in the shipped catalog | Use shipped IDs from the skillIds table; protocol skills are not skillIds |
| `TASK_TITLE_SHORT` | title < 3 chars | Use a descriptive title |
| `FEATURE_HAS_TASKS` | Feature doc contains `task` blocks | Move tasks to a Task document |
| `TOO_MANY_BLOCKS` | body > 130 blocks | Split into sibling documents |
| `BARE_FILENAME` | `fileLinks` has bare filename (warning) | Use repo-relative paths |

## Research Protocol (optional)

Before creating Task or Feature documents:

1. `search_knowledge` is RECOMMENDED for explicit duplicate research, but NOT mandatory.
2. Check official docs for current APIs when work depends on external packages.
3. Check GitHub Issues when work touches a library with known regressions.

For Bug documents, external research is optional unless the cause is unclear.

## Clean Code Policy (C# tasks)

1. Business logic methods start with `Validation()` on line 1 when validation applies.
2. One responsibility per method. Avoid names like `DoXAndY`.
3. Prefer semantic names. Aim for methods <15 lines, classes <150 lines.
4. Code tasks must name exact files/classes/methods in `target`, not "the page" or "the logic".

## Canonical Example: All 12 Block Types

Copy when you need a single document exercising every block type:

```text
create_document(
  modelName="<your-model-id>",
  sessionId="<your-fresh-session-uuid>",
  projectId="<your-project-id-from-tadweena.md>",
  document={
    "schema": "tadweena-document/1.0",
    "title": "Block Types Reference",
    "summary": "Canonical document covering all 12 TDS block types with one example each. Use as a copy-paste reference when building new documents.",
    "type": "General",
    "tags": ["documentation", "samples", "test"],
    "parentDocumentId": null,
    "body": [
      { "type": "heading", "level": 1, "text": "Block Types Reference" },
      { "type": "paragraph", "text": "This document demonstrates every supported block type in the Tadweena Document Schema. Copy any block when building a new document." },
      { "type": "heading", "level": 2, "text": "Text Blocks" },
      { "type": "bulletList", "items": ["First bullet item", "Second bullet item", "Third bullet item"] },
      { "type": "numberedList", "start": 1, "items": ["Alpha step", "Beta step", "Gamma step"] },
      { "type": "quote", "text": "Simplicity is the ultimate sophistication." },
      { "type": "divider" },
      { "type": "heading", "level": 2, "text": "Code" },
      { "type": "code", "language": "csharp", "source": "public class Greeter { public string Hello(string name) => $\"Hello, {name}!\"; }" },
      { "type": "heading", "level": 2, "text": "Task Block" },
      { "type": "task",
        "title": "Demonstrate task block structure",
        "checked": false,
        "why": "Validation requires the task block to carry a why field of at least forty characters explaining motivation and a non-empty target reference.",
        "target": "Demo/Greeter.cs::Greeter.Hello",
        "code": { "language": "csharp", "source": "public string Hello(string name) => $\"Hello, {name}!\";" },
        "fileLinks": ["Demo/Greeter.cs"] },
      { "type": "heading", "level": 2, "text": "Toggle with Nested Children" },
      { "type": "toggle",
        "title": "Click to expand toggle content",
        "body": [
          { "type": "paragraph", "text": "This paragraph lives inside the toggle body." },
          { "type": "bulletList", "items": ["Nested bullet A", "Nested bullet B"] },
          { "type": "code", "language": "json", "source": "{ \"nested\": true }" }
        ] },
      { "type": "heading", "level": 2, "text": "Media" },
      { "type": "image", "url": "https://example.com/image.png", "caption": "Reference image." },
      { "type": "attachment", "url": "https://example.com/files/sample.pdf", "name": "sample.pdf" },
      { "type": "heading", "level": 2, "text": "Table" },
      { "type": "table",
        "header": ["Block Type", "Required Field", "Optional Fields"],
        "rows": [
          ["paragraph", "text", "—"],
          ["heading", "level, text", "—"],
          ["task", "title, checked, why, target", "code, risk, fileLinks, skillIds, body"]
        ] },
      { "type": "paragraph", "text": "End of block types reference." },
      { "type": "divider" }
    ]
  }
)
```

## Full Example: Feature create_document

```text
create_document(
  modelName="<your-model-id>",
  sessionId="<your-fresh-session-uuid>",
  projectId="<your-project-id-from-tadweena.md>",
  document={
    "schema": "tadweena-document/1.0",
    "title": "Add vector search to document discovery",
    "summary": "Adds pgvector-backed vector search so users can find semantically similar documents. Captures user value, business rules, and acceptance boundaries.",
    "type": "Feature",
    "tags": ["search", "embedding", "pgvector", "ui"],
    "parentDocumentId": "<your-project-id-from-tadweena.md>",
    "body": [
      { "type": "paragraph", "text": "This feature enables users to search project documents by meaning rather than exact keywords, using stored embeddings and ranked vector similarity." },
      { "type": "heading", "level": 3, "text": "User Problem" },
      { "type": "paragraph", "text": "Current keyword search misses conceptual matches when users do not know the exact words used in the original document." },
      { "type": "heading", "level": 3, "text": "User Experience" },
      { "type": "paragraph", "text": "The existing Search page gains a semantic mode that returns project-filtered results ordered by similarity." },
      { "type": "heading", "level": 3, "text": "Business Rules" },
      { "type": "bulletList", "items": [
          "Empty queries return no results and do not call external providers.",
          "Results must always be filtered by the user's current project.",
          "Embedding generation is retried up to three times on provider failure."
        ] },
      { "type": "heading", "level": 3, "text": "Acceptance Boundary" },
      { "type": "paragraph", "text": "The feature is complete when semantic results appear beside existing search behavior without exposing cross-project content." }
    ]
  }
)
```

## Full Example: Task create_document

```text
create_document(
  modelName="<your-model-id>",
  sessionId="<your-fresh-session-uuid>",
  projectId="<your-project-id-from-tadweena.md>",
  document={
    "schema": "tadweena-document/1.0",
    "title": "Implement VectorSearchService query flow",
    "summary": "Creates the implementation plan for vector search query execution. Includes service method, validation, unit tests, and DI wiring.",
    "type": "Task",
    "tags": ["search", "embedding", "service", "tests"],
    "parentDocumentId": "<your-feature-doc-id>",
    "body": [
      { "type": "paragraph", "text": "This task document implements the vector search query flow end to end so the semantic search UI can request ranked document results." },
      { "type": "task",
        "title": "Add SearchAsync to VectorSearchService",
        "checked": false,
        "why": "The semantic search UI and API endpoint need one service method that validates the request, embeds the query, and returns ranked document results.",
        "target": "BlazorTadweenaAi.Services/Search/VectorSearchService.cs::VectorSearchService.SearchAsync",
        "code": {
          "language": "csharp",
          "source": "public async Task<IReadOnlyList<SearchResult>> SearchAsync(int projectId, string query, int limit, CancellationToken ct) { Validation().ProjectId(projectId).Query(query).Limit(limit); var embedding = await _embeddingService.GetEmbeddingAsync(query, ct); return await _vectorStore.SearchAsync(projectId, embedding, limit, ct); }"
        },
        "fileLinks": ["BlazorTadweenaAi.Services/Search/VectorSearchService.cs"]
      }
    ]
  }
)
```

## Full Example: Bug create_document

```text
create_document(
  modelName="<your-model-id>",
  sessionId="<your-fresh-session-uuid>",
  projectId="<your-project-id-from-tadweena.md>",
  document={
    "schema": "tadweena-document/1.0",
    "title": "NullReferenceException in SearchAsync when query is empty",
    "summary": "Documents an empty-query failure in VectorSearchService.SearchAsync. Includes observed error, failing call, and root cause hypothesis.",
    "type": "Bug",
    "tags": ["bug", "search", "validation"],
    "parentDocumentId": "<your-task-doc-id>",
    "body": [
      { "type": "paragraph", "text": "VectorSearchService.SearchAsync throws NullReferenceException when called with an empty query string instead of rejecting the input locally." },
      { "type": "code", "language": "csharp", "source": "await vectorSearchService.SearchAsync(42, \"\", 10, CancellationToken.None);" },
      { "type": "paragraph", "text": "The likely root cause is that SearchAsync calls the embedding service before validating the query." }
    ]
  }
)
```
