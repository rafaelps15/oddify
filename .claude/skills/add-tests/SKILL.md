---
name: add-tests
description: Write xUnit + FluentAssertions tests for a .NET Clean Architecture / Modular Monolith solution — Domain entity unit tests, Application command/query handler unit tests using hand-written fakes (no mocking framework), and reflection-based architecture tests that enforce the layering/naming conventions. Use when the user asks to add or write tests for an entity, handler, command, query, or to add architecture tests.
argument-hint: <use case or entity to cover, e.g. "CompleteTodoItemCommand" or "the TodoItem entity">
---

# Add Tests

Writes tests that match a specific stack: **xUnit** + **FluentAssertions**, with **no mocking
framework** (no NSubstitute/Moq) — handler tests use small hand-written fake implementations of the
repository/unit-of-work interfaces instead of a mocking library. Confirm this against the target repo's
test project references before writing anything: if it actually does reference NSubstitute or Moq,
follow that instead of this skill's fakes — this skill documents the no-mocking-library convention
because that's what a from-scratch Clean-Architecture/Modular-Monolith .NET solution defaults to absent
evidence otherwise, not because mocking libraries are wrong in general.

> **Heads up for this repo specifically:** `Oddify.UnitTests` already references **NSubstitute** and
> every existing handler test (e.g. `DepositarNaBancaCommandHandlerTests`,
> `GetResultadosDasPernasQueryHandlerTests`) substitutes dependencies with it rather than using
> hand-written fakes. Match that — use `Substitute.For<T>()`, not the fake classes below — for handler
> tests written for this repo. See [../add-feature/references/tests.md](../add-feature/references/tests.md)
> for the NSubstitute-based template and a real integration-test example
> (`OddifyWebAppFactory`/`BancasTests`). Sections A (Domain entity tests) and C (architecture tests)
> below are unaffected — they don't use test doubles either way.

## Step 0 — Detect the project's real conventions

1. Find the test projects (commonly `tests/<Solution>.UnitTests`, `tests/<Solution>.ArchitectureTests`,
   maybe `tests/<Solution>.IntegrationTests`). Read their `.csproj` `PackageReference`s to confirm the
   test framework and assertion library, and whether a mocking library, `NetArchTest`/`ArchUnitNET`, or
   `Testcontainers`/`Microsoft.AspNetCore.Mvc.Testing` is present. Let what you find override the
   defaults in this skill.
2. Resolve `<RootNamespace>` and `<Module>` the same way as in `add-entity`/`add-feature`.
3. Read the entity and handler you're testing (or the ones nearest in shape to what you're testing) —
   test doubles must implement the exact repository interface signatures, not an assumed shape.

Mirror the production folder structure inside the test project: a Domain entity test lives at
`tests/<Solution>.UnitTests/<Module>/<Entities>/<Entity>Tests.cs`; a handler test lives at
`tests/<Solution>.UnitTests/<Module>/<Entities>/<Verb><Entity>/<Verb><Entity>CommandHandlerTests.cs`.

---

## A. Domain entity unit tests

Pure, no test doubles needed — the entity has no dependencies. One test class per entity, `sealed`,
named `<Entity>Tests`. One `[Fact]` per behavior/branch; use `[Theory]` + `[InlineData]` only when the
same assertion genuinely repeats across multiple inputs.

Naming: `<Method>_Should_<ExpectedOutcome>_When_<Condition>` (drop the `_When_...` clause when there's
no meaningful condition, e.g. the happy path of a factory method). Arrange/Act/Assert, blank line
between each section, no comments labeling them (the blank lines already do that job).

```csharp
using <RootNamespace>.Common.Domain;
using <RootNamespace>.Modules.Todos.Domain.TodoItems;
using FluentAssertions;
using Xunit;

namespace <RootNamespace>.UnitTests.Todos.TodoItems;

public sealed class TodoItemTests
{
    private static readonly string Title = "Buy milk";

    [Fact]
    public void Create_Should_ReturnSuccess_WhenDueDateIsInTheFuture()
    {
        DateTime dueDateUtc = DateTime.UtcNow.AddDays(1);

        Result<TodoItem> result = TodoItem.Create(Title, description: null, dueDateUtc);

        result.IsSuccess.Should().BeTrue();
        result.Value.Title.Should().Be(Title);
        result.Value.IsCompleted.Should().BeFalse();
    }

    [Fact]
    public void Create_Should_ReturnFailure_WhenDueDateIsInThePast()
    {
        DateTime dueDateUtc = DateTime.UtcNow.AddDays(-1);

        Result<TodoItem> result = TodoItem.Create(Title, description: null, dueDateUtc);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(TodoItemErrors.DueDateInPast);
    }

    [Fact]
    public void Create_Should_RaiseTodoItemCreatedDomainEvent()
    {
        Result<TodoItem> result = TodoItem.Create(Title, description: null, dueDateUtc: null);

        result.Value.DomainEvents.Should().ContainSingle(e => e is TodoItemCreatedDomainEvent);
    }

    [Fact]
    public void Complete_Should_ReturnFailure_WhenAlreadyCompleted()
    {
        TodoItem todoItem = TodoItem.Create(Title, description: null, dueDateUtc: null).Value;
        todoItem.Complete();

        Result result = todoItem.Complete();

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(TodoItemErrors.AlreadyCompleted);
    }

    [Fact]
    public void Rename_Should_NotRaiseDomainEvent_WhenTitleIsUnchanged()
    {
        TodoItem todoItem = TodoItem.Create(Title, description: null, dueDateUtc: null).Value;
        todoItem.ClearDomainEvents();

        todoItem.Rename(Title);

        todoItem.DomainEvents.Should().BeEmpty();
    }
}
```

Rules:
- Assert on `Result.IsSuccess`/`IsFailure` and `Result.Error` (compare against the exact
  `<Entity>Errors` member, not a string), never on exception types — the entity never throws for
  expected business-rule violations.
- Cover the domain-event contract explicitly: a state transition that should raise an event asserts
  `DomainEvents.Should().ContainSingle(e => e is <Event>)`; a no-op transition asserts
  `DomainEvents.Should().BeEmpty()` after clearing whatever the setup raised.
- Don't reach into private state — assert only through public properties and `Result`.
- No test double, no `IUnitOfWork`, no repository — if a test needs one of those, it belongs in the
  handler tests below, not here.

---

## B. Application handler unit tests

No mocking framework: write a minimal in-memory fake that implements the exact repository interface,
and a no-op fake for `IUnitOfWork`. Keep the fakes next to the tests that use them (a private nested
class, or a small shared `Fakes` folder if more than one handler test needs the same one) — don't pull
in a mocking library just to avoid a ten-line class.

```csharp
using <RootNamespace>.Common.Domain;
using <RootNamespace>.Modules.Todos.Application.Abstractions.Data;
using <RootNamespace>.Modules.Todos.Application.TodoItems.CompleteTodoItem;
using <RootNamespace>.Modules.Todos.Domain.TodoItems;
using FluentAssertions;
using Xunit;

namespace <RootNamespace>.UnitTests.Todos.TodoItems.CompleteTodoItem;

public sealed class CompleteTodoItemCommandHandlerTests
{
    private readonly FakeTodoItemRepository _todoItemRepository = new();
    private readonly FakeUnitOfWork _unitOfWork = new();
    private readonly CompleteTodoItemCommandHandler _handler;

    public CompleteTodoItemCommandHandlerTests()
    {
        _handler = new CompleteTodoItemCommandHandler(_todoItemRepository, _unitOfWork);
    }

    [Fact]
    public async Task Handle_Should_ReturnFailure_WhenTodoItemDoesNotExist()
    {
        var command = new CompleteTodoItemCommand(Guid.NewGuid());

        Result result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(TodoItemErrors.NotFound(command.TodoItemId));
    }

    [Fact]
    public async Task Handle_Should_CompleteTodoItem_AndPersist_WhenTodoItemExists()
    {
        TodoItem todoItem = TodoItem.Create("Buy milk", description: null, dueDateUtc: null).Value;
        _todoItemRepository.Add(todoItem);

        Result result = await _handler.Handle(new CompleteTodoItemCommand(todoItem.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        todoItem.IsCompleted.Should().BeTrue();
        _unitOfWork.SaveChangesCallCount.Should().Be(1);
    }

    private sealed class FakeTodoItemRepository : ITodoItemRepository
    {
        private readonly Dictionary<Guid, TodoItem> _todoItems = [];

        public Task<TodoItem?> GetAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(_todoItems.GetValueOrDefault(id));

        public void Insert(TodoItem todoItem) => _todoItems[todoItem.Id] = todoItem;

        public void Add(TodoItem todoItem) => _todoItems[todoItem.Id] = todoItem;
    }

    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        public int SaveChangesCallCount { get; private set; }

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            SaveChangesCallCount++;

            return Task.FromResult(0);
        }
    }
}
```

Rules:
- Handler under test is constructed by hand in the test class constructor (or a `[Fact]`-local
  arrange), wired to the fakes — never resolved from a DI container in a unit test.
- The fake repository is a `Dictionary<Guid, T>`-backed in-memory store implementing the interface
  exactly as declared in Domain — don't add members the interface doesn't have.
- Assert three things per handler test as relevant: the returned `Result` (success/failure + which
  `Error`), the resulting state on the entity/fake store, and — only when it matters to the test — that
  `SaveChangesAsync` was (or wasn't) called, via a call counter on the fake `IUnitOfWork`. A
  not-found/validation-failure test should assert `SaveChangesCallCount` stayed `0`.
- Query handlers that read via Dapper/`IDbConnectionFactory` are **not** unit tested this way — a fake
  `IDbConnectionFactory` would just be re-implementing a database. Cover query handlers with an
  integration test against a real database instead (see the note at the end), or skip unit-testing the
  handler itself and rely on architecture tests + the endpoint's contract.
- Test class per handler, named `<Verb><Entity>CommandHandlerTests` / `...QueryHandlerTests`, `sealed`.

---

## C. Architecture tests

Enforce the conventions themselves — that the codebase keeps shipping code in this shape, not just that
one feature happens to. Plain reflection + FluentAssertions (no `NetArchTest`/`ArchUnitNET` unless the
target repo's test project already references one — check Step 0 first). Load each module's assemblies
once per test class via `typeof(<SomeMarkerType>).Assembly`.

```csharp
using System.Reflection;
using <RootNamespace>.Common.Application.Messaging;
using FluentAssertions;
using Xunit;

namespace <RootNamespace>.ArchitectureTests.Todos;

public sealed class LayeringTests
{
    private static readonly Assembly DomainAssembly = typeof(Modules.Todos.Domain.TodoItems.TodoItem).Assembly;
    private static readonly Assembly ApplicationAssembly =
        typeof(Modules.Todos.Application.AssemblyReference).Assembly;

    [Fact]
    public void Domain_Should_NotReference_Application()
    {
        DomainAssembly.GetReferencedAssemblies()
            .Should().NotContain(a => a.Name == ApplicationAssembly.GetName().Name);
    }

    [Fact]
    public void CommandHandlers_Should_BeSealedAndInternal()
    {
        IEnumerable<Type> handlerTypes = ApplicationAssembly.GetTypes()
            .Where(t => t.IsClass && t.GetInterfaces()
                .Any(i => i.IsGenericType &&
                          (i.GetGenericTypeDefinition() == typeof(ICommandHandler<>) ||
                           i.GetGenericTypeDefinition() == typeof(ICommandHandler<,>))));

        handlerTypes.Should().OnlyContain(t => t is { IsSealed: true, IsPublic: false });
    }

    [Fact]
    public void Commands_Should_HaveNamesEndingWithCommand()
    {
        IEnumerable<Type> commandTypes = ApplicationAssembly.GetTypes()
            .Where(t => t.IsAssignableTo(typeof(IBaseCommand)) && t != typeof(IBaseCommand));

        commandTypes.Should().OnlyContain(t => t.Name.EndsWith("Command", StringComparison.Ordinal));
    }
}
```

Cover, one `[Fact]` each, adapted to what the target repo's own conventions actually are (verify each
rule against the real code before asserting it — don't assume every rule below applies verbatim):

- Domain assemblies reference nothing above them (no Application/Infrastructure/Presentation).
- Application assemblies don't reference Infrastructure or Presentation.
- One module's assemblies never reference another module's Domain/Application/Infrastructure directly
  (only its `PublicApi` project, if the solution has that cross-module-contract pattern).
- Every `ICommandHandler`/`IQueryHandler` implementation is `sealed` and non-public (`internal`).
- Every type implementing `ICommand`/`ICommand<T>`/`IQuery<T>` is a `sealed record` and its name ends
  with `Command`/`Query`.
- Every `AbstractValidator<T>` implementation is `sealed` and non-public, and its name ends with
  `Validator`.
- Every `IEndpoint` implementation is `sealed` and non-public.

---

## Integration tests (brief — verify before extending)

If the target repo has an `IntegrationTests` project, check what it actually references (a real
database via Docker/`docker-compose`, `Testcontainers`, `WebApplicationFactory`) before writing anything
here — don't invent infrastructure. In general, integration tests are the right place to cover what
unit tests deliberately skip: query handlers that hit Dapper/a real database, and full command
round-trips through EF Core. Same naming (`<Feature>Tests`, `sealed`), same FluentAssertions style;
follow whatever base class/fixture the existing integration tests already use for wiring the database
and app.

## Checklist before finishing

- [ ] Domain tests assert on `Result`/`Error`/`DomainEvents` only — no test double, no exception asserts
- [ ] Handler tests use hand-written fakes implementing the exact repository/`IUnitOfWork` interfaces
      (unless the repo actually has a mocking library — checked in Step 0)
- [ ] Every not-found/validation-failure handler test asserts `SaveChangesAsync` was **not** called
- [ ] Query handlers are not unit-tested with a fake `IDbConnectionFactory`
- [ ] Test class names: `<Entity>Tests`, `<Verb><Entity>CommandHandlerTests`/`...QueryHandlerTests`
- [ ] Test method names: `<Method>_Should_<Outcome>_When_<Condition>`
- [ ] Architecture-test rules were checked against the real codebase, not assumed wholesale
