# Tadweena Initialization Protocol

Run this ONCE per chat window, before the first MCP tool call.

## Steps

### 1. Generate a fresh sessionId
```
sessionId = Guid.NewGuid().ToString()   // new UUID, never reuse from history
```

### 2. Verify the Tadweena daemon is running
Check if the daemon is active (`tadweena status` or look for the PID file).
If it is stopped, ask the user if they want to run `tadweena start` in a
separate terminal. The daemon syncs the code graph — without it,
`project_pulse`, `query_code_graph`, and `search_knowledge` may return stale
data. Note: the daemon is NOT the MCP server; MCP tools work either way.

### 3. Read `tadweena.md` in the CURRENT WORKING DIRECTORY ONLY.
   HARD RULE: If it does not exist in CWD, STOP and ask the user use setup_project mcp tool.
   NEVER walk up to parent folders. NEVER read from sibling/parent projects.

**If tadweena.md exists with YAML front-matter (from step 3):**
- Extract `project_id`
- Merge-update ONLY your provider entry under `sessions`:
  ```yaml
  sessions:
    anthropic:                          # or: openai / gemini / codex / etc.
      model: claude-sonnet-4-6          # exact model id
      session_id: <fresh-guid>
      started_at: <iso8601-utc-now>
  ```
- Preserve `project_id`, all other providers, and any human notes
- Use `project_id` for all subsequent tool calls → **done**

**If tadweena.md is in legacy format** (`ProjectID: <guid>`):
- Migrate to YAML front-matter using the same GUID as `project_id`
- Add/update only your provider entry under `sessions`

**If tadweena.md is missing OR has no project_id:**
- Call `setup_project(modelName, sessionId)`
- ⚠️ This tool is multi-stage — if it returns `flowState = requires_interaction`, STOP and present the UI to the user (see Structured UI below)
- After `PROJECT_SELECTED` or `PROJECT_CREATED`, create/update tadweena.md with the exact YAML the tool returns

### 4. tadweena.md canonical format
```yaml
---
project_id: <guid>
mcp_key: <raw-mcp-token>
sessions:
  anthropic:
    model: claude-sonnet-4-6
    session_id: <guid>
    started_at: 2025-01-01T00:00:00Z
---
```
Provider keys: `openai`, `anthropic`, `gemini`, `kimi`, `deepseek`, `meta`, `mistral`, `other`

## Structured UI Protocol

When any tool returns `structuredContent.flowState = requires_interaction`:

1. **STOP** — do NOT call any other tool
2. Translate the UI to natural language and present it to the user (never relay raw JSON)
3. Wait for the user's response — never guess or use placeholder values
4. Call the tool again with exactly what the user provided:
   - options UI → `selectedProjectId = "<id>"` OR `newProjectName = "<name>"`
   - confirmation UI → `altInput.paramName = "<exact name>"`
5. Never pass option numbers — always pass the actual id or name

## New Project Bootstrap

After `setup_project` returns for a brand-new project, the response carries
`Entities.bootstrapRequired=true` and a `Next` string that lists the three
bootstrap modes. The Project document is intentionally empty — do not create
a placeholder README, do not create a second Project document, do not
auto-fill a fake Vision/Goals/Architecture.

1. **Choose a bootstrap mode** based on the user's intent:
   - `manual_input` — ask the user for Vision, Goals, Architecture, then fill with `edit_blocks`.
   - `explore_codebase` — call `project_pulse` to discover existing code, then fill with `edit_blocks`.
   - `goal_based` — propose Vision/Goals/Architecture from the project name, then fill with `edit_blocks`.
 2. **Fill the same Project document** (docId = projectId) with `edit_blocks` — TDS structure:
    ```
    {"type":"heading","level":3,"text":"Vision"}
    {"type":"paragraph","text":"Why this project exists and what problem it solves."}
    {"type":"heading","level":3,"text":"Goals"}
    {"type":"bulletList","items":["First measurable goal."]}
    {"type":"heading","level":3,"text":"Architecture"}
    {"type":"paragraph","text":"Core technology and design decisions."}
    ```
    Rule: NEVER add `task` blocks to the project doc. NEVER use `create_document`
    with `type="Project"` — the project already exists. NEVER use `setup_project`
    again to "re-fill" the README.
 3. **Create Feature docs** — `create_document(type="Feature", parentDocumentId=projectId)`
 4. **Create Task docs** — under each Feature (see create.md for TDS rules)
5. **Report** — show created structure, ask which task to implement first
