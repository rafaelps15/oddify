# Endpoint Templates

File: `Presentation/<Entities>/<Verb><Entity>.cs`. One file per use case, shared shape for commands and
queries.

## Command with a response, POST

```csharp
using <RootNamespace>.Common.Domain;
using <RootNamespace>.Common.Presentation.Endpoints;
using <RootNamespace>.Common.Presentation.Results;
using <RootNamespace>.Modules.Todos.Application.TodoItems.CreateTodoItem;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace <RootNamespace>.Modules.Todos.Presentation.TodoItems;

internal sealed class CreateTodoItem : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("todo-items", async (Request request, ISender sender) =>
        {
            Result<Guid> result = await sender.Send(
                new CreateTodoItemCommand(request.Title, request.Description, request.DueDateUtc));

            return result.Match(Results.Ok, ApiResults.Problem);
        })
        .WithTags(Tags.TodoItems);
    }

    internal sealed class Request
    {
        public string Title { get; init; }

        public string? Description { get; init; }

        public DateTime? DueDateUtc { get; init; }
    }
}
```

## Command with no response, PUT (state transition, no body)

```csharp
internal sealed class CompleteTodoItem : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("todo-items/{id}/complete", async (Guid id, ISender sender) =>
        {
            Result result = await sender.Send(new CompleteTodoItemCommand(id));

            return result.Match(() => Results.Ok(), ApiResults.Problem);
        })
        .WithTags(Tags.TodoItems);
    }
}
```

## Query, GET by id

```csharp
internal sealed class GetTodoItem : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("todo-items/{id}", async (Guid id, ISender sender) =>
        {
            Result<TodoItemResponse> result = await sender.Send(new GetTodoItemQuery(id));

            return result.Match(Results.Ok, ApiResults.Problem);
        })
        .WithTags(Tags.TodoItems);
    }
}
```

## Rules

- `internal sealed class : IEndpoint`, one endpoint per HTTP operation, `MapEndpoint` is the only member.
- Route segments are lower-kebab/plural nouns matching the entity (`todo-items`), sub-actions as a
  trailing verb segment (`todo-items/{id}/complete`) — never a verb-first route.
- The lambda body is always the same three lines: build the Command/Query from route/body/query
  parameters → `await sender.Send(...)` → `return result.Match(...)`.
  - `Result<T>` → `result.Match(Results.Ok, ApiResults.Problem)` (method group, not a lambda).
  - `Result` (no value) → `result.Match(() => Results.Ok(), ApiResults.Problem)` or
    `result.Match(Results.NoContent, ApiResults.Problem)` — match whichever shape the sibling endpoints
    in the module already use (see `CLAUDE.md` §6 — this codebase is not consistent between the two).
- Every endpoint ends with `.WithTags(Tags.<Entities>)`. If `Tags.<Entities>` doesn't exist yet on the
  module's `Tags` static class, add it (`internal const string TodoItems = "TodoItems";`).
- POST/PUT bodies are bound through a `Request` nested class **inside** the endpoint class, `internal
  sealed`, every property `{ get; init; }`. Never bind the Command/Query type directly as the request
  body — the wire shape and the MediatR message are kept separate even when they look identical today.
- Route-param-only inputs (`Guid id`) and query-string inputs on a `GET` bind as plain minimal-API
  lambda parameters — no `[FromRoute]`; C# default values on a query-string parameter double as the
  endpoint's defaults (`int page = 0, int pageSize = 15`).
- No manual endpoint registration — `IEndpoint` implementations are discovered by assembly scanning.
