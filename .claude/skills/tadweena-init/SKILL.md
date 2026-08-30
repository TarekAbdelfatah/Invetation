---
name: tadweena-init
description: >
  Use this skill at the START of every new chat window before making any Tadweena MCP tool call.
  Triggers on: any mention of tadweena, starting work on a project, "let's work on X", opening a
  workspace, setup_project, or when tadweena.md is referenced. MANDATORY first step — never call
  any Tadweena MCP tool without completing this initialization first.
---

# Tadweena Project Initialization

Full protocol: `../../../tadweena-protocol/init.md`

Read that file now. It covers:
- Generating a fresh `sessionId`
- Reading / migrating / creating `tadweena.md`
- The Structured UI Protocol for `requires_interaction` responses
- New project bootstrap steps
