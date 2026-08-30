---
name: eng.clean-code
id: eng.clean-code
layer: engineering
gate: before_write
---

# eng.clean-code

## HOW

Read the files you are about to change. Match neighboring naming, error handling, and logging. Do not expand scope. Prefer the smallest named unit that already exists over a new abstraction.

### Size and names

- A method should do one thing and stay at or under **15 lines** of body (signature and closing brace do not count). If it grows past that, extract a named helper.
- Names must describe intent. Long names are better than short vague ones (`ValidateEmailFormat` over `Check`, `BuildSkillContractFromTickFiles` over `DoIt`).
- One responsibility per method. If the name needs "And", split it.

### Nested validation (compose, do not inline)

The public method should read as a short story. Drill-down happens only if the reader opens a helper.

```
HandleCreateUser
  → ValidateRequest
      → ValidateIdentity
      → ValidateEmail
          → ValidateEmailFormat
          → ValidateEmailLength
      → ValidatePassword
  → PersistUser
```

Rules:

- Each check is its own method with a verb + subject name.
- Leaf methods validate one fact and return a structured result or append to a collector. They do not throw for expected input problems.
- Parent methods only call children. They do not mix format checks with persistence or HTTP mapping.
- C# examples live in `lang.csharp`. This rule applies to every language on the tick.

### Errors: expected vs unexpected

- **Expected input/domain problems** (bad email, missing field, unknown skill id): collect every problem in one pass, then return once. Do not throw per field. Do not catch-and-swallow.
- **Unexpected failures** (null ref, IO, invariant broken): do not scatter `try/catch` in business methods. Let a single edge handler map them (HTTP middleware, MCP tool envelope). Catch only what you can recover or translate.
- Never log-and-continue as if success. Never wrap the same error at three layers.

### Before you write

1. Open the target file and one neighbor of the same kind.
2. Name the new methods before coding them.
3. If a method would exceed 15 lines, extract first.

## WHEN

Optional on most code ticks (`before_write`). Not a `finish_task` gate in V1. Load when `SKILL_CONTRACT` lists this id.

## EVIDENCE

If you report this skill, `filesReviewed` must be the files you actually opened, and findings must name a concrete hygiene change (extract, rename, or compose) — not "looks clean".
