---
name: eng.architecture
id: eng.architecture
layer: engineering
gate: before_finish
---

# eng.architecture

## HOW

Map the tick onto existing layers. Prefer extending a current seam over a new project, folder, or service.

This repo's default flow:

```
Api (host) → Mcp / HTTP
           → Services (skills, ticks, documents)
           → Core (entities) / DTO
           → Infrastructure (EF, migrations)
```

Rules:

- MCP tools stay thin: parse, call a service, format. Scoring and validation belong in `TadweenaAiBackend.Services`, not in tool methods.
- Do not add a second catalog, a `list_skills` tool, or parse SKILL.md on the server. Catalog = WHEN; markdown = HOW for the agent.
- Daemon (`tadweena-daemon`) syncs the code graph and installs protocol files. It is not the MCP server. Do not put finish-gate logic in Python.
- Smallest change that preserves layering. Note what must **not** move.
- If AST/graph is skipped (`ast_skipped` on the contract), treat empty graph as **unknown risk**, not zero risk. Use SQL file paths. Do not promote architecture from leftover `CodeGraphNodes` after `tadweena stop`.

## WHEN

Promoted to required only when AST signals are live and the tick is large or a hotspot. If the daemon is stopped, this skill is not promoted; empty graph is not zero risk.

## EVIDENCE

Findings must name the boundary you kept or the coupling you refused (≥40 chars). `filesReviewed` must intersect the tick files.
