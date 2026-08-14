# Query Slice Templates

Files go in `Application/<Entities>/<Verb><Entity>/`. Reads **never** go through the repository or
`IUnitOfWork` — they bypass the domain model entirely and query the database directly with Dapper for
performance and to avoid loading full aggregates just to project a few columns.

## B1. `<Entity>Response.cs` (single-item shape)

Put it in the singular `Get<Entity>/` folder; other read features that return the same shape reference
it from there rather than redefining it.

```csharp
namespace <RootNamespace>.Modules.Todos.Application.TodoItems.GetTodoItem;

public sealed record TodoItemResponse(Guid Id, string Title, string? Description, bool IsCompleted, DateTime? DueDateUtc);
```

## B2. `Get<Entity>Query.cs`

```csharp
using <RootNamespace>.Common.Application.Messaging;

namespace <RootNamespace>.Modules.Todos.Application.TodoItems.GetTodoItem;

public sealed record GetTodoItemQuery(Guid TodoItemId) : IQuery<TodoItemResponse>;
```

## B3. `Get<Entity>QueryHandler.cs`

```csharp
using System.Data.Common;
using Dapper;
using <RootNamespace>.Common.Application.Data;
using <RootNamespace>.Common.Application.Messaging;
using <RootNamespace>.Common.Domain;
using <RootNamespace>.Modules.Todos.Domain.TodoItems;

namespace <RootNamespace>.Modules.Todos.Application.TodoItems.GetTodoItem;

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
                 is_completed AS {nameof(TodoItemResponse.IsCompleted)},
                 due_date_utc AS {nameof(TodoItemResponse.DueDateUtc)}
             FROM todos.todo_items
             WHERE id = @TodoItemId
             """;

        TodoItemResponse? todoItem = await connection.QuerySingleOrDefaultAsync<TodoItemResponse>(sql, request);

        if (todoItem is null)
        {
            return Result.Failure<TodoItemResponse>(TodoItemErrors.NotFound(request.TodoItemId));
        }

        return todoItem;
    }
}
```

List-returning variant (`GetTodoItems`, no filters):

```csharp
namespace <RootNamespace>.Modules.Todos.Application.TodoItems.GetTodoItems;

public sealed record GetTodoItemsQuery : IQuery<IReadOnlyCollection<TodoItemResponse>>;

internal sealed class GetTodoItemsQueryHandler(IDbConnectionFactory dbConnectionFactory)
    : IQueryHandler<GetTodoItemsQuery, IReadOnlyCollection<TodoItemResponse>>
{
    public async Task<Result<IReadOnlyCollection<TodoItemResponse>>> Handle(
        GetTodoItemsQuery request,
        CancellationToken cancellationToken)
    {
        await using DbConnection connection = await dbConnectionFactory.OpenConnectionAsync();

        const string sql =
            $"""
             SELECT
                 id AS {nameof(TodoItemResponse.Id)},
                 title AS {nameof(TodoItemResponse.Title)},
                 description AS {nameof(TodoItemResponse.Description)},
                 is_completed AS {nameof(TodoItemResponse.IsCompleted)},
                 due_date_utc AS {nameof(TodoItemResponse.DueDateUtc)}
             FROM todos.todo_items
             """;

        List<TodoItemResponse> todoItems = (await connection.QueryAsync<TodoItemResponse>(sql, request)).AsList();

        return todoItems;
    }
}
```

Rules:
- `IQuery<TResponse>` records carry only the filter/paging parameters — never a full entity.
- The handler opens its own connection via `await using DbConnection connection = await
  dbConnectionFactory.OpenConnectionAsync();`, one per `Handle` call.
- SQL is a `const string` `"""..."""` raw string literal, interpolated **only** for `nameof(...)` column
  aliases (so renaming a response property is a compile error if the SQL falls out of sync) — never
  interpolate actual parameter values into the SQL text; pass `request` (or a private `sealed record
  ...Parameters(...)` built from it) straight to Dapper as the parameters object.
- Table/column names are `snake_case`; response properties are `PascalCase`; the `AS {nameof(...)}`
  aliasing bridges the two.
- Single-item queries that find nothing return `Result.Failure<T>(<Entity>Errors.NotFound(id))`.
  Collection queries return an empty collection on no matches — never fail for "no results".
- A query with pagination/filters bundles them into a private `sealed record ...Parameters(...)` inside
  the handler (see `SearchEvents`-style filtering: status flags, nullable filters combined with SQL like
  `(@CategoryId IS NULL OR category_id = @CategoryId)`, and `OFFSET @Skip LIMIT @Take` computed as
  `(request.Page - 1) * request.PageSize`) rather than scattering ad-hoc parameter objects.
