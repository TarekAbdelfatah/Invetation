---
name: eng.testing
id: eng.testing
layer: engineering
gate: before_finish
---

# eng.testing

## HOW

Identify what the tick can break. Add or update tests at the **same layer** as the change (unit next to a pure scorer; MCP/HTTP for a tool contract).

Rules:

- Name the tests after the behavior (`SkipGate_DoesNotBypassRequiredBeforeFinishSkills`), not after the ticket id.
- Exercise the new branch and one regression path. Empty "tests exist" is not coverage.
- Report **what you ran** (`dotnet test --filter FullyQualifiedName~SkillSkipGateBypassTests`) or waive with a real `waiveReason`.
- Do not claim you ran the suite if you did not. Do not skip failing tests to finish.
- Fixtures and shared DB: follow `TadweenaAiBackend.Test` existing patterns; do not hit production `TadweenaAI` from tests.

If the change is protocol/docs only, waive with `not_applicable` and say why in `waiveReason` — do not invent passing tests.

## WHEN

Required for migrations/DbContext, public API, or wide scope. Evidence must name real tick files.

## EVIDENCE

`filesReviewed` must include the test file you added/updated **or** the production file whose behavior you proved, and must intersect the tick files. Findings must state the filter/command run or the waive. ≥40 characters. No `LGTM`.
