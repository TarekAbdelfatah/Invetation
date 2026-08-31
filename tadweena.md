---
project_id: a8ffd050-3155-4522-b7cc-57e22dec4266
mcp_key: mcp_xYWcPDwNDQ3bXQNw0DsJp0jxgHuRjHJdjKZg3fK5jY
sessions:
  other:
    model: cursor-grok-4.6
    session_id: 7c4e8a91-2f3b-4d6e-9a10-b8c5d1e4f702
    started_at: 2026-08-30T22:08:00.0000000Z
  opencode:
    model: minimax-coding-plan/MiniMax-M3
    session_id: a069d669-9c9e-4e02-9ce7-1dc676a23c66
    started_at: 2026-08-31T00:31:18Z
---

# Graph note (requires direction)

On this project the workflow engine treats `requires` as:

- **source** = prerequisite (must finish first)
- **target** = waiter (cannot start until source is done)

Wrong: `SendToCommittee requires CSRF` — CSRF looks blocked by later POSTs (deadlock).
Right: `CSRF requires SendToCommittee` in wire_graph terms for *this* engine, i.e. `sourceId=CSRF`, `targetId=SendToCommittee`.

Do not follow the MCP sentence “source depends on target being done first” when wiring Ibtikar. All existing Ibtikar `requires` / AGE `DEPENDS_ON` edges were flipped to the engine-correct direction on 2026-08-31. See Tadweena Rule: Requires edge direction: prerequisite is source.

