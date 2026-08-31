---
project_id: a8ffd050-3155-4522-b7cc-57e22dec4266
mcp_key: mcp_xYWcPDwNDQ3bXQNw0DsJp0jxgHuRjHJdjKZg3fK5jY
sessions:
  other:
    model: cursor-grok-4.6
    session_id: 7c4e8a91-2f3b-4d6e-9a10-b8c5d1e4f702
    started_at: 2026-08-30T22:08:00.0000000Z
  opencode:
    model: opencode/big-pickle
    session_id: 8f2a3c1e-9b4d-4e6a-b7f5-2d8c1a9e4f3b
    started_at: 2026-08-31T16:39:00Z
  opencode2:
    model: opencode/mimo-v2.5-free
    session_id: b7e3f1a2-4c8d-4e9b-a1f6-3d2e8c5b9a74
    started_at: 2026-08-31T22:15:00Z
  current:
    model: opencode/big-pickle
    session_id: 3d6e2f1f-1dbf-4160-9792-8f3a79ca0fdc
    started_at: 2026-08-31T23:01:00Z
---

# Graph note (requires)

On 2026-08-31 every Ibtikar `requires` edge was deleted (`BlockLinks` + AGE `DEPENDS_ON`) because dual MCP/engine readings and hub-and-spoke wiring deadlocked execution for hours.

Do **not** call `wire_graph(requires)` on this project unless the user explicitly asks. Sequencing lives in Task documents, not the graph. `part_of` / `HAS_CHILD` / `SHARES_CODE` were left in place.

