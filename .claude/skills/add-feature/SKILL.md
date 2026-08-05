---
name: add-feature
description: Scaffold a new CQRS command or query (record + handler + validator + minimal-API endpoint), including single-item, list, and paginated/filtered search queries, for an existing entity in a module of this modular monolith, matching this template's vertical-slice conventions exactly.
---

# add-feature

Scaffold one CQRS use case (a "feature") for an entity that already exists (via `/add-entity` or
pre-existing). Read this repo's root `CLAUDE.md` first if one exists — it takes precedence over
this skill wherever the two differ. Ask the user which **module**, which **entity/aggregate**,
the **use case name** (verb + noun, e.g. `PublishEvent`, `GetOrder`, `GetOrders`,
`SearchOrders`), whether it's a **command** (mutates state) or **query** (reads state), and its
input/output shape.

Before scaffolding, find and read 2-3 existing use cases in this repo to match style exactly: a
command with a return value, ideally a command with no return value, and a single-item plus a
list (or paginated search) query — along with their endpoints. Below, the worked example
scaffolds five use cases for a fictional `TodoItem` entity in a fictional `Tasks` module (a
stand-in that won't collide with any real entity in this repo): `CreateTodoItem` (command,
returns `Guid`), `CompleteTodoItem` (command, no return value, route-param only), `GetTodoItem`
(single query), `GetTodoItems` (unfiltered list query), `SearchTodoItems` (paginated/filtered
query). Swap these for whatever the user actually asked for; `<ProjectName>` stands for this
repo's actual root namespace/solution name.

## A. Command feature — `Application/TodoItems/CreateTodoItem/`

**`CreateTodoItemCommand.cs`**
```csharp
using <ProjectName>.Common.Application.Messaging;

namespace <ProjectName>.Modules.Tasks.Application.TodoItems.CreateTodoItem;

public sealed record CreateTodoItemCommand(string Title, string Description) : ICommand<Guid>;
```

**`CreateTodoItemCommandHandler.cs`**
```csharp
using <ProjectName>.Common.Application.Messaging;
using <ProjectName>.Common.Domain;
using <ProjectName>.Modules.Tasks.Application.Abstractions.Data;
using <ProjectName>.Modules.Tasks.Domain.TodoItems;

namespace <ProjectName>.Modules.Tasks.Application.TodoItems.CreateTodoItem;

internal sealed class CreateTodoItemCommandHandler(ITodoItemRepository todoItemRepository, IUnitOfWork unitOfWork)
    : ICommandHandler<CreateTodoItemCommand, Guid>
{
    public async Task<Result<Guid>> Handle(CreateTodoItemCommand request, CancellationToken cancellationToken)
    {
        TodoItem todoItem = TodoItem.Create(request.Title, request.Description);

        todoItemRepository.Insert(todoItem);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return todoItem.Id;
    }
}
```
For a brand-new aggregate (as above), call `TodoItem.Create(...)` then
`todoItemRepository.Insert(todoItem)` before `SaveChangesAsync`. For a mutation on an existing
aggregate (see `CompleteTodoItem` below), `GetAsync` it first, call its behavior method, then
`SaveChangesAsync` — EF change tracking persists the mutation, no explicit `Update` call needed.

**`CreateTodoItemCommandValidator.cs`** (commands only — never write one for a query)
```csharp
using FluentValidation;

namespace <ProjectName>.Modules.Tasks.Application.TodoItems.CreateTodoItem;

internal sealed class CreateTodoItemCommandValidator : AbstractValidator<CreateTodoItemCommand>
{
    public CreateTodoItemCommandValidator()
    {
        RuleFor(c => c.Title).NotEmpty().MaximumLength(200);
        RuleFor(c => c.Description).NotEmpty().MaximumLength(2000);
    }
}
```

## A2. Command with no return value, route-param only — `Application/TodoItems/CompleteTodoItem/`

```csharp
using <ProjectName>.Common.Application.Messaging;

namespace <ProjectName>.Modules.Tasks.Application.TodoItems.CompleteTodoItem;

public sealed record CompleteTodoItemCommand(Guid TodoItemId) : ICommand;
```
```csharp
using <ProjectName>.Common.Application.Clock;
using <ProjectName>.Common.Application.Messaging;
using <ProjectName>.Common.Domain;
using <ProjectName>.Modules.Tasks.Application.Abstractions.Data;
using <ProjectName>.Modules.Tasks.Domain.TodoItems;

namespace <ProjectName>.Modules.Tasks.Application.TodoItems.CompleteTodoItem;

internal sealed class CompleteTodoItemCommandHandler(
    ITodoItemRepository todoItemRepository,
    IDateTimeProvider dateTimeProvider,
    IUnitOfWork unitOfWork)
    : ICommandHandler<CompleteTodoItemCommand>
{
    public async Task<Result> Handle(CompleteTodoItemCommand request, CancellationToken cancellationToken)
    {
        TodoItem? todoItem = await todoItemRepository.GetAsync(request.TodoItemId, cancellationToken);

        if (todoItem is null)
        {
            return Result.Failure(TodoItemErrors.NotFound(request.TodoItemId));
        }

        Result result = todoItem.Complete(dateTimeProvider.UtcNow);

        if (result.IsFailure)
        {
            return result;
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
```
Inject `IDateTimeProvider` (from `Common.Application.Clock`) instead of calling `DateTime.UtcNow`
directly whenever "now" is a value the handler makes a decision with or passes into the entity —
it's what makes that decision testable.

## B. Query feature — single item and unfiltered list

Only create a `Response` record when this is the "canonical" query for the shape (typically the
singular `Get<Entity>` query) — a list/search query for the same aggregate reuses it by
namespace-qualified reference rather than redefining a DTO.

**`TodoItemResponse.cs`** (in the singular query's folder, `Application/TodoItems/GetTodoItem/`)
```csharp
namespace <ProjectName>.Modules.Tasks.Application.TodoItems.GetTodoItem;

public sealed record TodoItemResponse(Guid Id, string Title, string Description, bool IsCompleted);
```

**`GetTodoItemQuery.cs`**
```csharp
using <ProjectName>.Common.Application.Messaging;

namespace <ProjectName>.Modules.Tasks.Application.TodoItems.GetTodoItem;

public sealed record GetTodoItemQuery(Guid TodoItemId) : IQuery<TodoItemResponse>;
```

**`GetTodoItemQueryHandler.cs`** — reads via **Dapper**, not EF, against `IDbConnectionFactory`
(this is the read side of CQRS):
```csharp
using System.Data.Common;
using Dapper;
using <ProjectName>.Common.Application.Data;
using <ProjectName>.Common.Application.Messaging;
using <ProjectName>.Common.Domain;
using <ProjectName>.Modules.Tasks.Application.Abstractions.Data;
using <ProjectName>.Modules.Tasks.Domain.TodoItems;

namespace <ProjectName>.Modules.Tasks.Application.TodoItems.GetTodoItem;

internal sealed class GetTodoItemQueryHandler(IDbConnectionFactory dbConnectionFactory)
    : IQueryHandler<GetTodoItemQuery, TodoItemResponse>
{
    public async Task<Result<TodoItemResponse>> Handle(GetTodoItemQuery request, CancellationToken cancellationToken)
    {
        await using DbConnection connection = await dbConnectionFactory.OpenConnectionAsync();

        const string sql =
            $"""
             SELECT
                 id AS {nameof(TodoItemResponse.Id)},
                 title AS {nameof(TodoItemResponse.Title)},
                 description AS {nameof(TodoItemResponse.Description)},
                 is_completed AS {nameof(TodoItemResponse.IsCompleted)}
             FROM tasks.todo_items
             WHERE id = @TodoItemId
             """;

        TodoItemResponse? result = await connection.QuerySingleOrDefaultAsync<TodoItemResponse>(sql, request);

        if (result is null)
        {
            return Result.Failure<TodoItemResponse>(TodoItemErrors.NotFound(request.TodoItemId));
        }

        return result;
    }
}
```
Use `nameof(TodoItemResponse.X)` for every column alias so the SQL stays compiler-checked against
the record. Pass `request` directly as the Dapper parameters object when its property names match
the `@Param` placeholders.

List variant, parameterless record syntax (note: no parentheses), same folder-reuse rule:
```csharp
namespace <ProjectName>.Modules.Tasks.Application.TodoItems.GetTodoItems;

public sealed record GetTodoItemsQuery : IQuery<IReadOnlyCollection<GetTodoItem.TodoItemResponse>>;
```
```csharp
internal sealed class GetTodoItemsQueryHandler(IDbConnectionFactory dbConnectionFactory)
    : IQueryHandler<GetTodoItemsQuery, IReadOnlyCollection<TodoItemResponse>>
{
    public async Task<Result<IReadOnlyCollection<TodoItemResponse>>> Handle(GetTodoItemsQuery request, CancellationToken cancellationToken)
    {
        await using DbConnection connection = await dbConnectionFactory.OpenConnectionAsync();

        const string sql =
            $"""
             SELECT
                 id AS {nameof(TodoItemResponse.Id)},
                 title AS {nameof(TodoItemResponse.Title)},
                 description AS {nameof(TodoItemResponse.Description)},
                 is_completed AS {nameof(TodoItemResponse.IsCompleted)}
             FROM tasks.todo_items
             """;

        List<TodoItemResponse> todoItems = (await connection.QueryAsync<TodoItemResponse>(sql, request)).AsList();

        return todoItems;
    }
}
```
No validator file for either query — queries have none in this template (the validation pipeline
behavior only runs for `IBaseCommand`).

## C. Paginated/filtered search query — `Application/TodoItems/SearchTodoItems/`

Use this shape whenever the use case takes filters and/or paging, instead of the plain list query
above. It runs **two** SQL statements against the same filter predicate — one page of rows, one
total count — through a shared private parameters record so both stay in sync.

**`SearchTodoItemsQuery.cs`**
```csharp
using <ProjectName>.Common.Application.Messaging;

namespace <ProjectName>.Modules.Tasks.Application.TodoItems.SearchTodoItems;

public sealed record SearchTodoItemsQuery(
    bool? IsCompleted,
    DateTime? CreatedAfterUtc,
    int Page,
    int PageSize) : IQuery<SearchTodoItemsResponse>;
```

**`SearchTodoItemsResponse.cs`**
```csharp
namespace <ProjectName>.Modules.Tasks.Application.TodoItems.SearchTodoItems;

public sealed record SearchTodoItemsResponse(
    int Page,
    int PageSize,
    int TotalCount,
    IReadOnlyCollection<TodoItemResponse> Items);
```

**`SearchTodoItemsQueryHandler.cs`**
```csharp
using System.Data.Common;
using Dapper;
using <ProjectName>.Common.Application.Data;
using <ProjectName>.Common.Application.Messaging;
using <ProjectName>.Common.Domain;
using <ProjectName>.Modules.Tasks.Application.Abstractions.Data;
using <ProjectName>.Modules.Tasks.Application.TodoItems.GetTodoItem;

namespace <ProjectName>.Modules.Tasks.Application.TodoItems.SearchTodoItems;

internal sealed class SearchTodoItemsQueryHandler(IDbConnectionFactory dbConnectionFactory)
    : IQueryHandler<SearchTodoItemsQuery, SearchTodoItemsResponse>
{
    public async Task<Result<SearchTodoItemsResponse>> Handle(SearchTodoItemsQuery request, CancellationToken cancellationToken)
    {
        await using DbConnection connection = await dbConnectionFactory.OpenConnectionAsync();

        var parameters = new SearchTodoItemsParameters(
            request.IsCompleted,
            request.CreatedAfterUtc,
            request.PageSize,
            (request.Page - 1) * request.PageSize);

        IReadOnlyCollection<TodoItemResponse> items = await GetItemsAsync(connection, parameters);
        int totalCount = await CountItemsAsync(connection, parameters);

        return new SearchTodoItemsResponse(request.Page, request.PageSize, totalCount, items);
    }

    private static async Task<IReadOnlyCollection<TodoItemResponse>> GetItemsAsync(
        DbConnection connection, SearchTodoItemsParameters parameters)
    {
        const string sql =
            $"""
             SELECT
                 id AS {nameof(TodoItemResponse.Id)},
                 title AS {nameof(TodoItemResponse.Title)},
                 description AS {nameof(TodoItemResponse.Description)},
                 is_completed AS {nameof(TodoItemResponse.IsCompleted)}
             FROM tasks.todo_items
             WHERE
                (@IsCompleted IS NULL OR is_completed = @IsCompleted) AND
                (@CreatedAfterUtc::timestamp IS NULL OR created_at_utc >= @CreatedAfterUtc::timestamp)
             ORDER BY created_at_utc
             OFFSET @Skip
             LIMIT @Take
             """;

        List<TodoItemResponse> items = (await connection.QueryAsync<TodoItemResponse>(sql, parameters)).AsList();
        return items;
    }

    private static async Task<int> CountItemsAsync(DbConnection connection, SearchTodoItemsParameters parameters)
    {
        const string sql =
            """
            SELECT COUNT(*)
            FROM tasks.todo_items
            WHERE
               (@IsCompleted IS NULL OR is_completed = @IsCompleted) AND
               (@CreatedAfterUtc::timestamp IS NULL OR created_at_utc >= @CreatedAfterUtc::timestamp)
            """;

        return await connection.ExecuteScalarAsync<int>(sql, parameters);
    }

    private sealed record SearchTodoItemsParameters(bool? IsCompleted, DateTime? CreatedAfterUtc, int Take, int Skip);
}
```
The `private sealed record ...Parameters` computes `Skip` once and is reused as the Dapper
parameter object for both statements — don't recompute `(Page - 1) * PageSize` twice or let the
two `WHERE` clauses drift apart. Nullable-filter columns use the `@Param IS NULL OR column = @Param`
pattern so an absent filter is a true no-op, not an accidental exclusion.

## D. Endpoint — `Presentation/TodoItems/<UseCase>.cs`

Class/file name = use case name minus `Command`/`Query` suffix.

Command with a return value (`Presentation/TodoItems/CreateTodoItem.cs`):
```csharp
using <ProjectName>.Common.Domain;
using <ProjectName>.Common.Presentation.Endpoints;
using <ProjectName>.Common.Presentation.Results;
using <ProjectName>.Modules.Tasks.Application.TodoItems.CreateTodoItem;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace <ProjectName>.Modules.Tasks.Presentation.TodoItems;

internal sealed class CreateTodoItem : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("todo-items", async (Request request, ISender sender) =>
        {
            Result<Guid> result = await sender.Send(new CreateTodoItemCommand(request.Title, request.Description));

            return result.Match(Results.Ok, ApiResults.Problem);
        })
        .WithTags(Tags.TodoItems);
    }

    internal sealed class Request
    {
        public string Title { get; init; }
        public string Description { get; init; }
    }
}
```
Command with **no** return value (`Result`, not `Result<T>`) — use either
`.Match(() => Results.Ok(), ApiResults.Problem)` or `.Match(Results.NoContent, ApiResults.Problem)`.
**Repos following this template can end up genuinely inconsistent** between these two for
the same shape of endpoint — there's no single house rule to enforce. Match whichever sibling
endpoints already exist in the module/aggregate you're extending; if there's no sibling to match,
default to `Results.NoContent()`.

Route-param-only command (`Presentation/TodoItems/CompleteTodoItem.cs`) — bind `Guid id` as a
plain lambda parameter matched to `{id}`, no `[FromRoute]` needed:
```csharp
app.MapPut("todo-items/{id}/complete", async (Guid id, ISender sender) =>
{
    Result result = await sender.Send(new CompleteTodoItemCommand(id));

    return result.Match(Results.NoContent, ApiResults.Problem);
})
.WithTags(Tags.TodoItems);
```
Use `MapPut` for updates/state-transitions, `MapPost` for creation, `MapDelete` only if truly
deleting.

Query (`Presentation/TodoItems/GetTodoItem.cs`):
```csharp
app.MapGet("todo-items/{id}", async (Guid id, ISender sender) =>
{
    Result<TodoItemResponse> result = await sender.Send(new GetTodoItemQuery(id));
    return result.Match(Results.Ok, ApiResults.Problem);
})
.WithTags(Tags.TodoItems);
```
List query (`Presentation/TodoItems/GetTodoItems.cs`, no params):
```csharp
app.MapGet("todo-items", async (ISender sender) =>
{
    Result<IReadOnlyCollection<TodoItemResponse>> result = await sender.Send(new GetTodoItemsQuery());
    return result.Match(Results.Ok, ApiResults.Problem);
})
.WithTags(Tags.TodoItems);
```
Paginated/filtered search query (`Presentation/TodoItems/SearchTodoItems.cs`) — filters and
paging bind as plain query-string lambda parameters, with C# default values doubling as the
endpoint's own defaults:
```csharp
app.MapGet("todo-items/search", async (
    ISender sender,
    bool? isCompleted,
    DateTime? createdAfterUtc,
    int page = 1,
    int pageSize = 20) =>
{
    Result<SearchTodoItemsResponse> result =
        await sender.Send(new SearchTodoItemsQuery(isCompleted, createdAfterUtc, page, pageSize));

    return result.Match(Results.Ok, ApiResults.Problem);
})
.WithTags(Tags.TodoItems);
```

If `Tags.TodoItems` doesn't exist yet in this module's `Presentation/Tags.cs`, add a
`const string TodoItems = "TodoItems";` entry.

## E. No manual registration needed

Do **not** register the handler, validator, or endpoint anywhere by hand — MediatR assembly
scanning picks up the handler/validator, and `AddEndpoints(Presentation.AssemblyReference.Assembly)`
(called once from the module's `TasksModule.cs`) picks up the `IEndpoint` implementation via
reflection. If the build doesn't pick up the new feature, the bug is elsewhere (wrong
namespace/assembly), not a missing registration line.

## F. After scaffolding

Build the solution (`dotnet build`) to catch warnings-as-errors issues. Tell the user
`/add-tests` can now generate a handler unit test and (if endpoints exist) an integration test
for this feature, and `/ca-review` can check it against the architectural rules.
