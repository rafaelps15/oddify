---
name: add-feature
description: Scaffold a new CQRS command or query (record + handler + validator + minimal-API endpoint) for an existing entity in a module of this modular monolith, matching this template's vertical-slice conventions exactly.
---

# add-feature

Scaffold one CQRS use case (a "feature") for an entity that already exists (via
`/add-entity` or pre-existing). Read this repo's `CLAUDE.md` (or equivalent conventions doc)
first, if one exists. Ask the user which **module**, which **entity/aggregate**, the **use
case name** (verb + noun, e.g. `PublishEvent`, `GetOrder`, `GetOrders`), whether it's a
**command** (mutates state) or **query** (reads state), and its input/output shape.

Before scaffolding, find and read 2-3 existing use cases in this repo to match style exactly:
a command with a return value, ideally a command with no return value, and a single-item plus
a list query — along with their endpoints. Below, the worked example scaffolds four use cases
for a fictional `TodoItem` entity in a fictional `Tasks` module (a stand-in that doesn't
collide with any real entity in this repo): `CreateTodoItem` (command, returns `Guid`),
`CompleteTodoItem` (command, no return value, route-param only), `GetTodoItem` (single query),
`GetTodoItems` (list query). Swap these for whatever the user actually asked for;
`<ProjectName>` stands for this repo's actual root namespace/solution name.

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
        TodoItem todoItem = TodoItem.Create(request.Title, request.Description, DateTime.UtcNow);

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
using <ProjectName>.Common.Application.Messaging;
using <ProjectName>.Common.Domain;
using <ProjectName>.Modules.Tasks.Application.Abstractions.Data;
using <ProjectName>.Modules.Tasks.Domain.TodoItems;

namespace <ProjectName>.Modules.Tasks.Application.TodoItems.CompleteTodoItem;

internal sealed class CompleteTodoItemCommandHandler(ITodoItemRepository todoItemRepository, IUnitOfWork unitOfWork)
    : ICommandHandler<CompleteTodoItemCommand>
{
    public async Task<Result> Handle(CompleteTodoItemCommand request, CancellationToken cancellationToken)
    {
        TodoItem? todoItem = await todoItemRepository.GetAsync(request.TodoItemId, cancellationToken);

        if (todoItem is null)
        {
            return Result.Failure(TodoItemErrors.NotFound(request.TodoItemId));
        }

        Result result = todoItem.Complete(DateTime.UtcNow);

        if (result.IsFailure)
        {
            return result;
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
```

## A3. Command that needs data from another module — never inline it

If the command handler needs to read state owned by a different module (via that module's
`PublicApi`) or an external system, **do not** call it directly from the command handler, and
**do not** wrap it in a MediatR query either — a query handler's contract forces a `Result<T>`
return, which just reintroduces the unwrap-in-the-handler shape this rule exists to avoid. See
`CLAUDE.md`'s "Command handler shape" for the rule (no `*Response`-suffixed type, and no
`Result<T>` from a cross-module read, may appear in a command handler) and its worked example.

Instead, scaffold a plain injected service whose method signature reads exactly like a
repository lookup — `Task<T?>`, nullable, no `Result<T>` at the call site:

**`Application/Abstractions/Assignees/IAssigneeSummaryService.cs`** (public — injected across
an assembly boundary):
```csharp
namespace <ProjectName>.Modules.Tasks.Application.Abstractions.Assignees;

public interface IAssigneeSummaryService
{
    Task<AssigneeSummary?> ObterAsync(Guid userId, CancellationToken cancellationToken);
}

public sealed record AssigneeSummary(Guid UserId, string DisplayName);
```

**`Infrastructure/Assignees/AssigneeSummaryService.cs`** (`internal sealed class` — owns the
other module's `PublicApi` call and its `Result.IsFailure` handling; any failure collapses to
`null` here, it never reaches the command handler):
```csharp
internal sealed class AssigneeSummaryService(IUsersApi usersApi) : IAssigneeSummaryService
{
    public async Task<AssigneeSummary?> ObterAsync(Guid userId, CancellationToken cancellationToken)
    {
        Result<UserResponse> user = await usersApi.ObterUsuarioAsync(userId, cancellationToken);
        if (user.IsFailure)
        {
            return null;
        }

        return new AssigneeSummary(user.Value.Id, user.Value.DisplayName);
    }
}
```
Register it by hand in `TasksModule.AddInfrastructure` — `services.AddScoped<IAssigneeSummaryService, AssigneeSummaryService>();`
(it's a plain service, not a handler/validator/endpoint, so assembly scanning doesn't pick it up).

**`CreateTodoItemCommandHandler.cs`** then reads like any other repository-backed lookup:
```csharp
internal sealed class CreateTodoItemCommandHandler(
    IAssigneeSummaryService assigneeSummaryService, ITodoItemRepository todoItemRepository, IUnitOfWork unitOfWork)
    : ICommandHandler<CreateTodoItemCommand, Guid>
{
    public async Task<Result<Guid>> Handle(CreateTodoItemCommand request, CancellationToken cancellationToken)
    {
        AssigneeSummary? assignee = await assigneeSummaryService.ObterAsync(request.AssigneeUserId, cancellationToken);
        if (assignee is null)
        {
            return Result.Failure<Guid>(TodoItemErrors.AssigneeUnavailable(request.AssigneeUserId));
        }

        var todoItem = TodoItem.Create(request.Title, assignee.DisplayName, DateTime.UtcNow);

        todoItemRepository.Insert(todoItem);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return todoItem.Id;
    }
}
```
For a real, fully worked instance of this exact shape in this repo, read
`AnalisarPartidaCommandHandler` +
`IAnaliseDePartidaDadosService`/`AnaliseDePartidaDadosService`.

## B. Query feature — `Application/TodoItems/GetTodoItem/` and `GetTodoItems/`

Only create a `Response` record when this is the "canonical" query for the shape (typically
the singular `Get<Entity>` query) — list/search queries in the same aggregate should reuse it
by referencing that folder's namespace rather than redefining a DTO.

**`TodoItemResponse.cs`** (in the singular query's folder)
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
List variant, parameterless record syntax (note: no parentheses):
```csharp
namespace <ProjectName>.Modules.Tasks.Application.TodoItems.GetTodoItems;

public sealed record GetTodoItemsQuery : IQuery<IReadOnlyCollection<GetTodoItem.TodoItemResponse>>;
```

**`GetTodoItemQueryHandler.cs`** — reads via **Dapper**, not EF, against
`IDbConnectionFactory` (this is the read side of CQRS):
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
Use `nameof(TodoItemResponse.X)` for every column alias so the SQL stays in sync with the
record. Pass `request` directly as the Dapper parameters object when its property names match
the `@Param` placeholders. No validator file — queries have none in this template (the
validation pipeline behavior only runs for `IBaseCommand`).

## C. Endpoint — `Presentation/TodoItems/<UseCase>.cs`

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
Command with **no** return value (`Result`, not `Result<T>`) — use `.Match(() => Results.Ok(), ApiResults.Problem)`.
Route-param-only command (`Presentation/TodoItems/CompleteTodoItem.cs`) — bind `Guid id` as a
plain lambda parameter matched to `{id}`, no `[FromRoute]` needed:
```csharp
app.MapPut("todo-items/{id}/complete", async (Guid id, ISender sender) =>
{
    Result result = await sender.Send(new CompleteTodoItemCommand(id));

    return result.Match(() => Results.Ok(), ApiResults.Problem);
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

If `Tags.TodoItems` doesn't exist yet in this module's `Presentation/Tags.cs`, add a
`const string TodoItems = "TodoItems";` entry.

## D. No manual registration needed

Do **not** register the handler, validator, or endpoint anywhere by hand — MediatR assembly
scanning picks up the handler/validator, and `AddEndpoints(Presentation.AssemblyReference.Assembly)`
(called once from the module's `TasksModule.cs`) picks up the `IEndpoint` implementation via
reflection. If the build doesn't pick up the new feature, the bug is elsewhere (wrong
namespace/assembly), not a missing registration line.

## E. After scaffolding

Build the solution (`dotnet build`) to catch warnings-as-errors issues. Tell the user
`/add-tests` can now generate a handler unit test and (if endpoints exist) an integration
test for this feature, and `/ca-review` can check it against the architectural rules.
