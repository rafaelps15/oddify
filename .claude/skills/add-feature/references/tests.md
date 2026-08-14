# Test Templates

Stack actually used by this repo's `Oddify.UnitTests` / `Oddify.IntegrationTests` projects: **xUnit**
+ **FluentAssertions** + **NSubstitute** for handler unit tests, and a `WebApplicationFactory`-based
fixture (`OddifyWebAppFactory`) over a real Postgres for integration tests. Confirm this against the
target repo's actual test project references before writing anything — if a repo built from this
template genuinely has no mocking library referenced, follow the hand-written-fakes approach from the
`add-tests` skill instead.

## Command handler unit tests

`tests/<Solution>.UnitTests/Modules/<Module>/<Verb><Entity>/<Verb><Entity>CommandHandlerTests.cs`.
Substitute every constructor dependency with NSubstitute (`Substitute.For<T>()`), one `[Fact]` per
`Result.Failure` branch plus one happy path asserting the resulting entity state and that
`IUnitOfWork.SaveChangesAsync` was called exactly once.

```csharp
using FluentAssertions;
using NSubstitute;
using <RootNamespace>.Common.Application.Clock;
using <RootNamespace>.Common.Domain;
using <RootNamespace>.Modules.Todos.Application.Abstractions.Data;
using <RootNamespace>.Modules.Todos.Application.TodoItems.CompleteTodoItem;
using <RootNamespace>.Modules.Todos.Domain.TodoItems;

namespace <RootNamespace>.UnitTests.Modules.Todos.CompleteTodoItem;

public sealed class CompleteTodoItemCommandHandlerTests
{
    private readonly ITodoItemRepository _todoItemRepository = Substitute.For<ITodoItemRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private CompleteTodoItemCommandHandler CriarHandler() => new(_todoItemRepository, _unitOfWork);

    [Fact]
    public async Task Handle_should_complete_the_todo_item()
    {
        TodoItem todoItem = TodoItem.Create("Buy milk", description: null, dueDateUtc: null).Value;
        _todoItemRepository.GetAsync(todoItem.Id, Arg.Any<CancellationToken>()).Returns(todoItem);

        Result resultado = await CriarHandler().Handle(new CompleteTodoItemCommand(todoItem.Id), CancellationToken.None);

        resultado.IsSuccess.Should().BeTrue();
        todoItem.IsCompleted.Should().BeTrue();
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_should_fail_when_todo_item_not_found()
    {
        var todoItemId = Guid.NewGuid();
        _todoItemRepository.GetAsync(todoItemId, Arg.Any<CancellationToken>()).Returns((TodoItem?)null);

        Result resultado = await CriarHandler().Handle(new CompleteTodoItemCommand(todoItemId), CancellationToken.None);

        resultado.IsFailure.Should().BeTrue();
        resultado.Error.Should().Be(TodoItemErrors.NotFound(todoItemId));
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
```

Notes:
- Name the test class `<Verb><Entity>CommandHandlerTests`, `sealed`, no base class.
- Test method names: `Handle_should_<outcome>[_when_<condition>]`, lower snake-ish after `Handle_should_`
  (matches the sibling handler tests already in the repo — don't switch to PascalCase mid-file).
- Every not-found/validation-failure test asserts `SaveChangesAsync` was **not** received.
- Inject `IDateTimeProvider`/`IUserContext` the same way when the handler needs them, stubbing only the
  members the test actually reads (`.Returns(...)`).
- Assert repository interactions that matter to the outcome with `Received(1)`/`DidNotReceive()` and
  `Arg.Is<T>(x => ...)` for the shape of what was inserted/updated — don't assert every property, only
  the ones the test is actually about.

## Query handler unit tests (business-logic queries only)

A query handler that composes results from other modules' `PublicApi` calls (no direct database access)
gets the same NSubstitute treatment as a command handler — substitute the `PublicApi` interfaces, cover
each failure branch plus the happy path:

```csharp
using FluentAssertions;
using NSubstitute;
using <RootNamespace>.Common.Domain;
using <RootNamespace>.Modules.Todos.Application.TodoItems.GetTodoItemSummary;
using <RootNamespace>.Modules.Projects.PublicApi;

namespace <RootNamespace>.UnitTests.Modules.Todos.GetTodoItemSummary;

public sealed class GetTodoItemSummaryQueryHandlerTests
{
    private readonly IProjectsApi _projectsApi = Substitute.For<IProjectsApi>();

    private GetTodoItemSummaryQueryHandler CriarHandler() => new(_projectsApi);

    [Fact]
    public async Task Handle_should_fail_when_project_is_not_found()
    {
        var projectId = Guid.NewGuid();
        _projectsApi.ObterProjetoAsync(projectId, Arg.Any<CancellationToken>()).Returns((ProjectResponse?)null);

        Result<TodoItemSummaryResponse> resultado =
            await CriarHandler().Handle(new GetTodoItemSummaryQuery(projectId), CancellationToken.None);

        resultado.IsFailure.Should().BeTrue();
    }
}
```

**Do not** unit test a query handler that reads directly via `IDbConnectionFactory`/Dapper with a fake
connection — a fake `IDbConnectionFactory` would just be re-implementing a database. Those are covered
only by the integration test below, through the real endpoint against a real Postgres instance.

## Integration tests

`tests/<Solution>.IntegrationTests/Modules/<Module>/<Entities>Tests.cs`. Inherit the repo's
`WebApplicationFactory`-backed fixture (e.g. `OddifyWebAppFactory`) through the shared xUnit collection,
reset the database in `InitializeAsync`, and drive the feature through real HTTP — never call the
handler directly here.

```csharp
using System.Net;
using System.Net.Http.Json;
using FluentAssertions;

namespace <RootNamespace>.IntegrationTests.Modules.Todos;

[Collection(IntegrationTestCollection.Name)]
public sealed class TodoItemsTests(OddifyWebAppFactory factory) : IAsyncLifetime
{
    private readonly HttpClient _client = factory.CreateClient();

    public Task InitializeAsync() => factory.ResetDatabaseAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task CompleteTodoItem_should_mark_the_todo_item_as_completed()
    {
        HttpResponseMessage createResponse = await _client.PostAsJsonAsync("todo-items", new
        {
            Title = "Buy milk",
        });
        Guid todoItemId = await createResponse.Content.ReadFromJsonAsync<Guid>();

        HttpResponseMessage completeResponse = await _client.PutAsync($"todo-items/{todoItemId}/complete", null);
        completeResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        HttpResponseMessage getResponse = await _client.GetAsync($"todo-items/{todoItemId}");
        TodoItemResponse? todoItem = await getResponse.Content.ReadFromJsonAsync<TodoItemResponse>();
        todoItem!.IsCompleted.Should().BeTrue();
    }

    [Fact]
    public async Task CompleteTodoItem_should_return_not_found_for_unknown_id()
    {
        HttpResponseMessage response = await _client.PutAsync($"todo-items/{Guid.NewGuid()}/complete", null);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
```

Minimum coverage per endpoint: one happy-path test asserting observable state via a follow-up `GET`,
and one failure-translation test (unknown id → 404, or whichever `ErrorType` the handler can return) —
matching the status-code table in `Common.Presentation`'s `ApiResults.Problem`. A `GET`-only query
feature (list/search) needs at least a happy-path test asserting the returned shape/count.

## Run

```
dotnet test
```

Integration tests need a reachable Postgres (see `OddifyWebAppFactory`/Testcontainers setup). For the
full test taxonomy — Domain entity tests, hand-written-fake handler tests if this repo genuinely has no
mocking library, and architecture tests — use the `add-tests` skill.
