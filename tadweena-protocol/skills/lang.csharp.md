---
name: lang.csharp
id: lang.csharp
layer: language
gate: before_finish
---

# lang.csharp

## HOW

Follow Microsoft C# conventions used by .NET docs / runtime style, then this repo's house rules. Match the file you are editing if it already differs (Microsoft: existing file style wins).

Sources: [C# coding conventions](https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/coding-conventions), [identifier names](https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/identifier-names), [ASP.NET Core error handling](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/error-handling).

### Microsoft conventions (must)

- PascalCase for types, public members, and **all methods including local functions**.
- camelCase for parameters and locals. Private/internal instance fields: `_camelCase`.
- Language keywords over BCL type names (`string`, `int`, not `String`, `Int32`).
- `async`/`await` for I/O. Do not block on `.Result` / `.Wait()`.
- Catch only exceptions you can handle. Do not `catch (Exception)` without a filter or a true edge translator. Prefer specific types.
- `var` only when the type is obvious from the right-hand side.
- Nullable annotations match the project's `#nullable` context. Do not add `!` to silence a real null.

### House rules (this repo)

- Method body ≤ **15 lines**. Extract until the caller reads as steps.
- Names stay meaningful even if long. No `Do`, `Handle`, `Process` without a subject.
- Prefer `partial` classes and existing folders over new projects.

### Nested validation (C#)

Public entry calls one composer. Each fact is a named method. Collect all expected failures; do not throw per field.

```csharp
private ValidationResult ValidateRequest(CreateUserRequest request)
{
    var result = new ValidationResult();
    ValidateIdentity(request, result);
    ValidateEmail(request.Email, result);
    ValidatePassword(request.Password, result);
    return result;
}

private void ValidateEmail(string? email, ValidationResult result)
{
    ValidateEmailPresent(email, result);
    ValidateEmailFormat(email, result);
    ValidateEmailLength(email, result);
}
```

The original method then does `ValidateRequest` then work. A reader who wants format rules opens `ValidateEmailFormat`, not a 80-line handler.

This matches `TadweenaAiBackend.Mcp/Tds/TdsValidator.cs`: one pass, every problem, no first-error short-circuit.

### Exceptions vs validation

| Kind | Where | Pattern |
|---|---|---|
| Input / TDS / MCP argument errors | Tool or service method | Collect all into one result. Return once (saves tokens; agent retries against a full list). |
| HTTP API unexpected errors | `ExceptionHandlerMiddleware` + `AddProblemDetails` | Catch once at the edge. Return ProblemDetails. Do not catch in every service. |
| Validation HTTP 400 | `ValidationProblem` / `HttpValidationProblemDetails` | All field errors in `errors`, not one exception per field. |

Do not invent a new exception type for "email too long". That is a collected validation error.

### Tests and layout

- Tests live in `TadweenaAiBackend.Test` mirroring the type under test.
- Prefer `dotnet test --filter` for the tick over a full solution run.
- DI: register in the existing composition root; do not new-up services that the container already owns.

## WHEN

Required on ticks whose files include `.cs` unless the 3-required cap demotes it.

## EVIDENCE

`filesReviewed` must intersect the tick `.cs` files. Findings must name a C# convention or house rule you applied or verified (naming, extract, collect-all validation, no swallow-catch) in ≥40 characters. `commitHash` must match `finish_task` `gitHash` when a commit exists.
