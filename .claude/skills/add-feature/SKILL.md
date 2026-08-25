---
name: add-feature
description: Scaffold a new CQRS use case (vertical slice) in a .NET Clean Architecture / Modular Monolith solution — Command or Query, its Handler, a FluentValidation Validator, and the minimal-API Endpoint that maps to it. Use when the user asks to add a new command, query, use case, application feature, or API endpoint in a project using MediatR, the Result pattern, Dapper for reads and EF Core for writes.
argument-hint: <feature description, e.g. "complete a todo item" or "get overdue todo items">
---

# Add Feature

Scaffolds one **vertical slice**: a single use case cutting through Application (Command/Query +
Handler + Validator) and Presentation (minimal-API endpoint), for a Domain entity that already exists.
If the entity doesn't exist yet, use the `add-entity` skill first — this skill assumes the entity,
its `Errors` class, and its repository interface are already in place.

This assumes the same architecture as `add-entity`: Modular Monolith, per-module `Application`/
`Presentation` projects, MediatR-based CQRS, Result pattern, FluentValidation, Dapper for reads, EF
Core for writes, minimal-API endpoints behind an `IEndpoint` marker interface.

## Step 0 — Detect the project's real conventions

Same as `add-entity` Step 0: resolve `<RootNamespace>` and `<Module>` from the existing codebase before
writing anything. Additionally, locate and read:

- The messaging abstractions (commonly `<RootNamespace>.Common.Application.Messaging`): `ICommand`,
  `ICommand<TResponse>`, `ICommandHandler<TCommand>`, `ICommandHandler<TCommand, TResponse>`,
  `IQuery<TResponse>`, `IQueryHandler<TQuery, TResponse>`. Confirm the exact generic shapes before
  implementing against them.
- The module's `IUnitOfWork` (commonly `.../Application/Abstractions/Data/IUnitOfWork.cs`) — used by
  every command handler that writes.
- The module's `IDbConnectionFactory` usage (commonly `<RootNamespace>.Common.Application.Data`) — used
  by every query handler that reads.
- `Result`/`Result<T>`/`Error` (same as `add-entity`).
- The Presentation layer's `IEndpoint` interface, the `ApiResults.Problem` / `ResultExtensions.Match`
  helpers, and the module's `Tags` static class.
- One existing feature end-to-end (pick any `Create<X>`/`Get<X>` pair) to confirm these templates still
  match the current state of the repo — architectures drift, don't blindly trust this file over the
  code that's actually there.

## Folder & naming convention

Every use case gets its own folder under `Application/<Entities>/<Verb><Entity>/`, named like the use
case itself (`CreateTodoItem`, `GetTodoItem`, `GetTodoItems`, `CompleteTodoItem`, `RenameTodoItem`).
One class per file, file name matches the class name exactly. The namespace is the folder path:
`<RootNamespace>.Modules.<Module>.Application.<Entities>.<Verb><Entity>`.

## Workflow

1. **Classify the use case.** A state change is a **command**; a read is a **query**. A feature that
   both reads and writes doesn't happen here: commands don't return read-model shapes beyond the bare
   id/void, and queries never touch the repository/`IUnitOfWork`.
2. **Check the Domain layer.** If the entity, its `<Entity>Errors` class, or a needed domain event
   doesn't exist, add it first with the `add-entity` skill.
3. **Command?** Create the four command-slice files (Command, Handler, Validator, plus the endpoint
   from step 4) using [references/command-slice.md](references/command-slice.md).
4. **Query?** Create the query-slice files (Response, Query, Handler) using
   [references/query-slice.md](references/query-slice.md).
5. **Create the endpoint** in `Presentation/<Entities>/<Verb><Entity>.cs` using
   [references/endpoint.md](references/endpoint.md) — shared shape for commands and queries.
6. **Write tests** — handler unit tests and an integration test, using
   [references/tests.md](references/tests.md). For the full test taxonomy (Domain entity tests,
   architecture tests), use the `add-tests` skill instead.
7. **Verify:** `dotnet build`, then `dotnet test`.

## Non-negotiable conventions

- **Folder = use case.** One folder per use case, containing all files for that slice.
- **Handlers are `internal sealed`** with primary constructors, implementing `ICommandHandler<TCommand>`,
  `ICommandHandler<TCommand, TResponse>`, or `IQueryHandler<TQuery, TResponse>`.
- **A Handler has exactly one method: `Handle`.** No private helper methods, ever (CLAUDE.md §17,
  verified against Kamil Grzybek's own repository). If the logic is short, it stays inline in `Handle`.
  If a fetch-and-build step needs its own I/O or is a real calculation, extract it to a `static`
  `Factory`/`Calculator` in `Application/Calculo/<Name>Calculator.cs` — dependencies as method
  parameters, no interface, no DI registration — never a private method on the Handler.
- **No manual DI registration.** Handlers, validators, and endpoints are discovered by assembly
  scanning — never register any of the three by hand.
- **Return `Result` / `Result<T>`, never throw** for expected failures. Errors come from static factory
  methods on `<Entity>Errors` in the Domain layer with codes like `"TodoItems.NotFound"`.
- **Validation lives in a `<Command>Validator`** (FluentValidation), structural rules only. Queries have
  no validators — the validation pipeline behavior only runs for commands.
- **Commands write via the repository + `IUnitOfWork`; queries read via `IDbConnectionFactory` +
  Dapper.** Never blur this split.
- **Endpoints** implement `IEndpoint`, end with `result.Match(...)`, and tag with the module's `Tags`
  class.

## Naming reference

| Artifact | Pattern | Example |
|---|---|---|
| Command | `<Verb><Entity>Command` | `CompleteTodoItemCommand` |
| Query | `Get<Entity>Query` | `GetOverdueTodoItemsQuery` |
| Handler | `<Command/Query>Handler` | `CompleteTodoItemCommandHandler` |
| Validator | `<Command>Validator` | `CreateTodoItemCommandValidator` |
| Response | `<Entity>Response` | `TodoItemResponse` |
| Endpoint | `<Verb><Entity>.cs` in `Presentation/<Entities>/` | `Presentation/TodoItems/CompleteTodoItem.cs` |

## Checklist before finishing

- [ ] Correctly classified as command (write, via repository + `IUnitOfWork`) or query (read, via
      `IDbConnectionFactory` + Dapper) — never mixed
- [ ] Command/Query is a `sealed record` implementing the right `ICommand[<T>]`/`IQuery<T>`
- [ ] Handler is `internal sealed`, primary-constructor DI, delegates all business rules to the entity
- [ ] Handler has no private helper methods — inline in `Handle`, or extracted to a static
      `Factory`/`Calculator` (CLAUDE.md §17), never in between
- [ ] Handler never calls `SaveChangesAsync` before every failure branch has already returned
- [ ] Command has a matching `internal sealed ...Validator : AbstractValidator<...>` for structural rules
- [ ] Query SQL aliases every column with `AS {nameof(Response.Property)}`, no interpolated values
- [ ] Endpoint is `internal sealed : IEndpoint`, ends with `result.Match(...)` and `.WithTags(...)`
- [ ] Request DTO (if any) is a nested `internal sealed class` with `init`-only properties
- [ ] No manual DI registration added for the handler, validator, or endpoint
