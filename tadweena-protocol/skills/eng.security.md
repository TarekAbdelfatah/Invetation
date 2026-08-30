---
name: eng.security
id: eng.security
layer: engineering
gate: before_finish
---

# eng.security

## HOW

Inspect every tick file on Auth, Identity, Crypto, Permission, or secret-handling paths. Also inspect MCP/API surfaces that accept tokens, keys, or identity.

Checklist (record residual risk; "looks fine" is not a finding):

1. **Authn vs authz** — proving who the caller is is not the same as what they may do. Check both.
2. **Trust boundary** — never trust client-supplied ids, role names, or `projectId` without a server check.
3. **Secrets** — no tokens, connection strings, or MCP keys in logs, findings, commits, or skill evidence. Do not print `mcp_key`.
4. **Injection** — SQL, TDS/JSON, path traversal (`FilePathValidator` style). Use parameterized access; reject `../` and absolute paths.
5. **Crypto** — do not invent hashing/encryption. Prefer platform APIs. Tokens have lifetime and audience.
6. **Least data** — MCP responses must not dump secrets or PII. Compact errors, not stack traces to the model in production.

If you cannot verify a control, say so in findings and either fix it or `waive` with `not_applicable` / `out_of_scope` — never `satisfied` with empty risk.

`quick_complete` cannot close a tick that requires this skill.

## WHEN

Required when TaskFileLink or planned files sit on Auth/Security/Crypto/Permission/Identity paths. High-risk: `quick_complete` cannot bypass this skill.

## EVIDENCE

`status=satisfied` requires `filesReviewed` intersecting those paths, ≥1 finding of ≥40 chars naming a concrete control (or residual risk), `resolved=true`, and `commitHash` when finishing with `gitHash`.
