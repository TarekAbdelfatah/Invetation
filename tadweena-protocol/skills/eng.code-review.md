---
name: eng.code-review
id: eng.code-review
layer: engineering
gate: before_finish
---

# eng.code-review

## HOW

Review the tick's actual diff and listed files, not the title.

Check, in order:

1. **Correctness** — does the change do what `why`/`target` said, and only that?
2. **Regressions** — neighboring callers, skipGate/blocker paths, finish vs quick_complete.
3. **Contract drift** — MCP schema, TDS fields, migration vs snapshot, protocol vs bundled copies (they must match).
4. **Error paths** — collected validation vs swallowed exceptions; `[SKILL_GATE]` still readable.
5. **Tests** — a test exists for the new branch, or a documented waive.

House style from `eng.clean-code` / `lang.csharp` is in scope: 15-line methods, composed validation, no secret in output.

Write findings as concrete observations (≥40 chars). Empty `LGTM` / `n/a` / `looks good` is rejected by the server.

## WHEN

Required for public API, wide file scope, or hotspot overlap. Evidence `filesReviewed` must intersect the tick files.

## EVIDENCE

```json
{
  "skillId": "eng.code-review",
  "status": "satisfied",
  "filesReviewed": ["TadweenaAiBackend.Services/Skills/SkillEvidenceValidator.cs"],
  "findings": ["skipGate still requires before_finish evidence; optional clean-code is not persisted as fake rows."],
  "resolved": true,
  "commitHash": "<same as finish_task gitHash>"
}
```

Unresolved issues: `status=waived` plus `waiveReason` in `out_of_scope|already_covered|user_directed|not_applicable`. Do not mark `satisfied` with `resolved=false`.
