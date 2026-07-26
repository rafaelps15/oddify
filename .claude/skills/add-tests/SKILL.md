---
name: add-tests
description: Scaffold unit tests, architecture-fitness tests, and integration tests for this modular monolith, bootstrapping test projects with xUnit + FluentAssertions (+ NetArchTest/NSubstitute/Testcontainers as needed) and wiring them into the solution.
---

# add-tests

**Check the repo's actual state before assuming anything.** This template's test projects
(`tests/<ProjectName>.UnitTests`, `tests/<ProjectName>.ArchitectureTests`,
`tests/<ProjectName>.IntegrationTests`) may already contain real tests, be empty scaffolds not
even wired into the solution, or not exist at all — verify with `git status`/`ls` and by
checking whether they're referenced in the `.sln` before deciding whether this is "bootstrap
from scratch" or "extend what's there." If real tests already exist, mirror their exact style
instead of the templates below. The established stack for this template is **xUnit +
FluentAssertions** (+ **NSubstitute** for mocking, **NetArchTest.Rules** for architecture
tests, **Testcontainers** + **Respawn** for integration tests) — don't introduce a different
framework (Moq, Shouldly, NUnit, etc.) unless the repo already uses it or the user asks. Below,
the examples cover a fictional `TodoItem` entity in a fictional `Tasks` module (a stand-in that
doesn't collide with any real entity in this repo) with a `CreateTodoItem` command — swap for
whatever the user is actually covering. `<ProjectName>` stands for this repo's actual root
namespace/solution name.

## 0. One-time bootstrap (skip any project that already exists and is wired in)

For each missing test project, `dotnet new xunit -o tests/<ProjectName>.<X>Tests -n <ProjectName>.<X>Tests`,
then set `TargetFramework` in the new `.csproj` to match this repo's `Directory.Build.props`
(don't leave whatever the template defaults to), then wire it into the solution:
```
dotnet sln <ProjectName>.sln add tests/<ProjectName>.UnitTests/<ProjectName>.UnitTests.csproj
dotnet sln <ProjectName>.sln add tests/<ProjectName>.ArchitectureTests/<ProjectName>.ArchitectureTests.csproj
dotnet sln <ProjectName>.sln add tests/<ProjectName>.IntegrationTests/<ProjectName>.IntegrationTests.csproj
```

Add `FluentAssertions` to every test project. Then, only for the project(s) you're actually
populating right now:

- **`<ProjectName>.UnitTests`** — add a `ProjectReference` to the module project(s) under test
  (e.g. `<ProjectName>.Modules.Tasks.Application`, `<ProjectName>.Modules.Tasks.Domain`) plus
  `<ProjectName>.Common.Domain`. Add `NSubstitute` for mocking repositories/`IUnitOfWork` if no
  mocking library exists yet — it pairs naturally with FluentAssertions and this template's
  primary-constructor DI style; don't introduce Moq unless the user asks for it.
- **`<ProjectName>.ArchitectureTests`** — add `NetArchTest.Rules` (if not present) and a
  `ProjectReference` to every assembly whose layering you want to assert
  (`<ProjectName>.Modules.Tasks.Domain/Application/Infrastructure/Presentation` for each
  module you're covering).
- **`<ProjectName>.IntegrationTests`** — add `Microsoft.AspNetCore.Mvc.Testing`,
  `Testcontainers.PostgreSql` (or whatever datastore this repo actually uses), and a
  `ProjectReference` to the API host project (needs `WebApplicationFactory<Program>` — check
  whether `Program.cs` needs a `public partial class Program {}` marker for the factory to see
  it, and add one if missing).

Code must build warning-free under this repo's `Directory.Build.props`/`.editorconfig`
settings (check for `TreatWarningsAsErrors`, `AnalysisMode`, mandatory file-scoped namespaces
and braces) — write scaffolded test code in that style, not throwaway style.

## 1. Unit test — command/query handler

Mirror the folder structure of the feature under test:
`tests/<ProjectName>.UnitTests/Modules/Tasks/TodoItems/CreateTodoItem/CreateTodoItemCommandHandlerTests.cs`.

```csharp
using <ProjectName>.Common.Domain;
using <ProjectName>.Modules.Tasks.Application.TodoItems.CreateTodoItem;
using <ProjectName>.Modules.Tasks.Application.Abstractions.Data;
using <ProjectName>.Modules.Tasks.Domain.TodoItems;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace <ProjectName>.UnitTests.Modules.Tasks.TodoItems.CreateTodoItem;

public sealed class CreateTodoItemCommandHandlerTests
{
    private readonly ITodoItemRepository _todoItemRepository = Substitute.For<ITodoItemRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    [Fact]
    public async Task Handle_should_create_todo_item_and_persist()
    {
        var command = new CreateTodoItemCommand("Buy milk", "2 liters, whole");

        var handler = new CreateTodoItemCommandHandler(_todoItemRepository, _unitOfWork);

        Result<Guid> result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        _todoItemRepository.Received(1).Insert(Arg.Is<TodoItem>(t => t.Title == command.Title));
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
```
For a handler that loads an existing aggregate first (e.g. `CompleteTodoItemCommandHandler`),
add one failure test per guard clause (not-found, business-rule violation) plus the happy path:
```csharp
[Fact]
public async Task Handle_should_return_failure_when_todo_item_not_found()
{
    var command = new CompleteTodoItemCommand(Guid.NewGuid());
    _todoItemRepository.GetAsync(command.TodoItemId, Arg.Any<CancellationToken>()).Returns((TodoItem?)null);

    var handler = new CompleteTodoItemCommandHandler(_todoItemRepository, _unitOfWork);

    Result result = await handler.Handle(command, CancellationToken.None);

    result.IsFailure.Should().BeTrue();
    result.Error.Should().Be(TodoItemErrors.NotFound(command.TodoItemId));
}
```
Use xUnit's `[Fact]`/`[Theory]`, method names as `MethodOrScenario_should_expectedBehavior_when_condition`
(check `.editorconfig` for a `CA1707` underscore-naming suppression — commonly allowed for
tests in this template). Build the arrange step through the entity's real `Create`/behavior
methods, never by reflection or bypassing invariants — that's the whole point of testing
against the domain model.

## 2. Unit test — domain entity behavior

`tests/<ProjectName>.UnitTests/Modules/Tasks/TodoItems/TodoItemTests.cs`:
```csharp
using <ProjectName>.Modules.Tasks.Domain.TodoItems;
using FluentAssertions;
using Xunit;

namespace <ProjectName>.UnitTests.Modules.Tasks.TodoItems;

public sealed class TodoItemTests
{
    [Fact]
    public void Create_should_raise_TodoItemCreatedDomainEvent()
    {
        TodoItem todoItem = TodoItem.Create("Buy milk", "2 liters, whole", DateTime.UtcNow);

        todoItem.DomainEvents.Should().ContainSingle(e => e is TodoItemCreatedDomainEvent);
    }

    [Fact]
    public void Complete_should_return_failure_when_already_completed()
    {
        TodoItem todoItem = TodoItem.Create("Buy milk", "2 liters, whole", DateTime.UtcNow);
        todoItem.Complete(DateTime.UtcNow);

        Result result = todoItem.Complete(DateTime.UtcNow);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(TodoItemErrors.AlreadyCompleted(todoItem.Id));
    }

    // For a no-op behavior method: assert DomainEvents stays empty when the value doesn't change.
}
```

## 3. Architecture-fitness test (NetArchTest.Rules)

`tests/<ProjectName>.ArchitectureTests/LayerDependencyTests.cs` — one rule set per module, or
parameterize across all module assemblies if you're covering more than one:
```csharp
using FluentAssertions;
using NetArchTest.Rules;
using Xunit;

namespace <ProjectName>.ArchitectureTests;

public sealed class LayerDependencyTests
{
    private const string DomainNamespace = "<ProjectName>.Modules.Tasks.Domain";
    private const string ApplicationNamespace = "<ProjectName>.Modules.Tasks.Application";
    private const string InfrastructureNamespace = "<ProjectName>.Modules.Tasks.Infrastructure";
    private const string PresentationNamespace = "<ProjectName>.Modules.Tasks.Presentation";

    [Fact]
    public void Domain_should_not_have_dependency_on_other_layers()
    {
        TestResult result = Types.InAssembly(typeof(<ProjectName>.Modules.Tasks.Domain.TodoItems.TodoItem).Assembly)
            .Should()
            .NotHaveDependencyOnAny(ApplicationNamespace, InfrastructureNamespace, PresentationNamespace)
            .GetResult();

        result.IsSuccessful.Should().BeTrue();
    }

    [Fact]
    public void Application_should_not_have_dependency_on_infrastructure_or_presentation()
    {
        TestResult result = Types.InAssembly(typeof(<ProjectName>.Modules.Tasks.Application.AssemblyReference).Assembly)
            .Should()
            .NotHaveDependencyOnAny(InfrastructureNamespace, PresentationNamespace)
            .GetResult();

        result.IsSuccessful.Should().BeTrue();
    }

    [Fact]
    public void CommandHandlers_should_be_sealed_and_internal()
    {
        TestResult result = Types.InAssembly(typeof(<ProjectName>.Modules.Tasks.Application.AssemblyReference).Assembly)
            .That()
            .ImplementInterface(typeof(<ProjectName>.Common.Application.Messaging.ICommandHandler<>))
            .Should()
            .BeSealed().And().NotBePublic()
            .GetResult();

        result.IsSuccessful.Should().BeTrue();
    }
}
```
(If the module's Domain project has no `AssemblyReference` class, use
`typeof(<ProjectName>.Modules.Tasks.Domain.TodoItems.TodoItem).Assembly` instead, as above.)
Add one cross-module rule too:
```csharp
[Fact]
public void Modules_should_not_reference_each_others_domain_or_application()
{
    // For each pair of modules, assert neither's Domain/Application namespace is referenced
    // by the other's Domain/Application/Infrastructure/Presentation — only an
    // <Module>.IntegrationEvents-style project is an allowed cross-module reference, if this
    // repo has that mechanism (check CLAUDE.md/existing consumers first).
}
```

## 4. Integration test — hitting an endpoint

`tests/<ProjectName>.IntegrationTests/Modules/Tasks/TodoItems/CreateTodoItemTests.cs`, backed
by a shared `WebApplicationFactory` fixture with real dependencies (DB, cache, etc.) via
Testcontainers (create `tests/<ProjectName>.IntegrationTests/<ProjectName>WebAppFactory.cs`
once, reuse via an xUnit `IClassFixture<<ProjectName>WebAppFactory>` / collection fixture
across all integration tests — don't spin up a container per test class):

```csharp
using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Xunit;

namespace <ProjectName>.IntegrationTests.Modules.Tasks.TodoItems;

public sealed class CreateTodoItemTests(<ProjectName>WebAppFactory factory) : IClassFixture<<ProjectName>WebAppFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task CreateTodoItem_should_return_ok_for_valid_request()
    {
        HttpResponseMessage response = await _client.PostAsJsonAsync("todo-items", new
        {
            title = "Buy milk",
            description = "2 liters, whole"
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
```
Reset database state between tests with Respawn (add the `Respawn` package) rather than
recreating containers. Check whether this repo has authentication on its endpoints — if not,
no auth bypass plumbing is needed; if it does, add token/auth setup matching whatever the
existing endpoints/tests already use.

## 5. After scaffolding

Run `dotnet test` on the affected project(s). If `NetArchTest.Rules`/`Testcontainers`/etc.
were just added, mention to the user that these are new dependencies being introduced (not
pre-existing in this repo) so they can veto a choice (e.g. prefer Moq over NSubstitute)
before it spreads across many files.
