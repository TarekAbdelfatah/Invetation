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
  opencode2:
    model: opencode/mimo-v2.5-free
    session_id: b7e3f1a2-4c8d-4e9b-a1f6-3d2e8c5b9a74
    started_at: 2026-08-31T22:15:00Z
  current:
    model: minimax-coding-plan/MiniMax-M3
    session_id: b7e3f1a2-4c8d-4e9b-a1f6-3d2e8c5b9a74
    started_at: 2026-08-31T22:30:00Z
---

# Graph note (requires direction)

`wire_graph requires` follows the MCP tool text: **source depends on target** (waiter → prerequisite). Do not mass-flip edges.

- ViewModel `499fcc2b` has no outbound `requires` — it can `finish_task` first.
- Create `13012599` requires ViewModel only — finish after ViewModel.
- Form follow-ups (counters, Other, tech, PDF UI, Submit, SaveDraft, Autofill, audience, ProcedureGateway) require Create — they wait; Create does not wait on them.

Never wire both directions. Never treat inbound consumers as a reason to invert the whole graph. Keep `BlockLinks` and AGE `DEPENDS_ON` in the same direction.

