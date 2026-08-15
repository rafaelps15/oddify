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

## B4. Parent + children in one query (Dapper multi-mapping)

When a response is an entity plus a same-schema child collection (e.g. `TodoItem` + its checklist
items), fetch both in **one** query with a `JOIN` and Dapper's multi-mapping — never a separate `Row`
type materialized first and then converted via a `.ToResponse(...)` extension method. Materialize
directly into the final Response types; the child collection lives as a mutable property **outside**
the parent's positional constructor, populated by the multi-mapping callback itself.

```csharp
namespace <RootNamespace>.Modules.Todos.Application.TodoItems.GetTodoItem;

public sealed record TodoItemResponse(Guid Id, string Title, bool IsCompleted)
{
    // Fora do construtor posicional — populada pelo callback do multi-mapping abaixo, nunca por um
    // Row intermediário nem por um .ToResponse(...) depois de montar a lista.
    public List<TodoChecklistItemResponse> ChecklistItems { get; } = [];
}

public sealed record TodoChecklistItemResponse(Guid ChecklistItemId, string Description, bool IsDone);
```

`ChecklistItemId`, não `Id` — evita colisão de nome de coluna com `TodoItemResponse.Id` no `splitOn` do
multi-mapping (as duas colunas teriam o mesmo alias `"Id"` senão, e o Dapper não saberia onde cortar as
colunas do pai pras do filho).

```csharp
internal sealed class GetTodoItemQueryHandler(IDbConnectionFactory dbConnectionFactory)
    : IQueryHandler<GetTodoItemQuery, TodoItemResponse>
{
    public async Task<Result<TodoItemResponse>> Handle(GetTodoItemQuery request, CancellationToken cancellationToken)
    {
        await using DbConnection connection = await dbConnectionFactory.OpenConnectionAsync();

        const string sql =
            $"""
             SELECT
                 t.id AS {nameof(TodoItemResponse.Id)},
                 t.title AS {nameof(TodoItemResponse.Title)},
                 t.is_completed AS {nameof(TodoItemResponse.IsCompleted)},
                 c.id AS {nameof(TodoChecklistItemResponse.ChecklistItemId)},
                 c.description AS {nameof(TodoChecklistItemResponse.Description)},
                 c.is_done AS {nameof(TodoChecklistItemResponse.IsDone)}
             FROM todos.todo_items t
             LEFT JOIN todos.todo_checklist_items c ON c.todo_item_id = t.id
             WHERE t.id = @TodoItemId
             """;

        IEnumerable<TodoItemResponse> rows = await connection.QueryAsync<TodoItemResponse, TodoChecklistItemResponse?, TodoItemResponse>(
            sql,
            (todoItem, checklistItem) =>
            {
                if (checklistItem is not null)
                {
                    todoItem.ChecklistItems.Add(checklistItem);
                }

                return todoItem;
            },
            request,
            splitOn: nameof(TodoChecklistItemResponse.ChecklistItemId));

        TodoItemResponse? todoItem = rows.FirstOrDefault();

        if (todoItem is null)
        {
            return Result.Failure<TodoItemResponse>(TodoItemErrors.NotFound(request.TodoItemId));
        }

        return todoItem;
    }
}
```

For a **list** of parents each with children (`GetTodoItemsQuery`), the same technique generalizes: a
`Dictionary<Guid, TodoItemResponse>` for O(1) "have I seen this parent yet" lookups, alongside a plain
`List<TodoItemResponse>` that preserves the SQL's row order (a `Dictionary`'s enumeration order isn't a
guaranteed contract):

```csharp
List<TodoItemResponse> todoItems = [];
var todoItemsById = new Dictionary<Guid, TodoItemResponse>();

await connection.QueryAsync<TodoItemResponse, TodoChecklistItemResponse?, TodoItemResponse>(
    sql,
    (todoItem, checklistItem) =>
    {
        if (!todoItemsById.TryGetValue(todoItem.Id, out TodoItemResponse? existing))
        {
            existing = todoItem;
            todoItemsById.Add(existing.Id, existing);
            todoItems.Add(existing);
        }

        if (checklistItem is not null)
        {
            existing.ChecklistItems.Add(checklistItem);
        }

        return existing;
    },
    request,
    splitOn: nameof(TodoChecklistItemResponse.ChecklistItemId));

return todoItems;
```

## B5. Enriching a child with data from another module (`PublicApi`)

When part of the response can only come from another module — never a cross-schema SQL `JOIN` (see
CLAUDE.md §11) — fetch it in **one batch call** after the main query, never per-row inside a loop
(that's an N+1 in disguise). If the other module's `PublicApi` doesn't have a batch method yet
(`ObterXsAsync(IReadOnlyCollection<Guid> ids, ...)`), add one there — don't work around its absence
with a loop calling the singular method.

The fields that only come from the other module live outside the child record's positional
constructor, for the same reason as B4 (the main SQL query never fills them, so Dapper's multi-mapping
can't bind them positionally). A method on the child record itself — never inline code in the handler —
owns setting them:

```csharp
public sealed record TodoChecklistItemResponse(Guid ChecklistItemId, string Description, bool IsDone, Guid AssignedToId)
{
    public string? AssignedToName { get; set; }

    public void Enriquecer(UserSummaryResponse? assignedTo) => AssignedToName = assignedTo?.Name;
}
```

```csharp
IReadOnlyCollection<UserSummaryResponse> assignees = await usersApi.ObterUsuariosAsync(
    todoItem.ChecklistItems.Select(c => c.AssignedToId).Distinct().ToList(), cancellationToken);

var assigneesById = assignees.ToDictionary(a => a.Id);

todoItem.ChecklistItems.ForEach(c => c.Enriquecer(assigneesById.GetValueOrDefault(c.AssignedToId)));
```

`List<T>.ForEach` here is the BCL method, not the `foreach` statement — see the no-`foreach` rule below.

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
- **Never wrap a success return in `Result.Success(...)` explicitly.** Return the bare value
  (`return todoItem;` / `return todoItems;`) and let `Result<T>`'s implicit conversion do it — every
  example above does this; if you find `Result.Success(...)` in a query handler, that's a finding.
- **No `foreach` in a query handler.** Reshape rows with LINQ (`Select`/`Where`/`GroupBy`/`Aggregate`) or
  the multi-mapping callback in B4/B5 instead. If the reshaping is really a dedup-per-group or
  first-per-group operation (e.g. "best candidate per match"), check whether Postgres can just do it —
  `DISTINCT ON (<group_col>)` in a subquery, reordered/limited by the outer query, is usually cleaner
  and faster than fetching everything and doing `GroupBy(...).Select(g => g.First())` in C#.
- **A query handler never decides anything, it only fetches and returns.** If turning raw rows into the
  response needs an actual formula or business rule (a score, a threshold check, a derived
  classification) — not just column renaming — that logic belongs in a shared static calculator
  (`Application/Calculo/<Name>Calculator.cs`, same convention as any other reusable domain
  calculation), never inline in `Handle(...)`. The handler's job stays: fetch → call the calculator →
  return.
- **Ownership/tenant scoping goes directly into the main query's `WHERE`** (`WHERE id = @Id AND
  owner_id = @OwnerId`) — never a separate `SELECT EXISTS (...)` pre-check query run before the real
  one just to produce a different `NotFound` for "not yours" vs. "doesn't exist." One query, one
  round-trip; both cases fall out of the same `WHERE`.
