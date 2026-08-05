---
name: add-tests
description: Scaffold unit tests, architecture-fitness tests, and integration tests for this modular monolith, bootstrapping test projects with xUnit (+ FluentAssertions/NSubstitute/NetArchTest.Rules/Testcontainers as needed) and wiring them into the solution.
---

# add-tests

**Check the repo's actual state before assuming anything — do not trust this skill's own
description of "the" stack over what's really there.** This template's test projects
(`tests/<ProjectName>.UnitTests`, `tests/<ProjectName>.ArchitectureTests`,
`tests/<ProjectName>.IntegrationTests`) may already contain real tests, be non-existent, or be a
half-finished scaffold: a folder with only `bin`/`obj` and **no `.csproj`, no `.cs` files at all**
— i.e. a project that was `dotnet new xunit`'d at some point (its `obj/*.nuget.g.props` shows a
restore that pulled in nothing but `xunit.analyzers`) and then never actually committed or
finished. Verify with `git status`/`ls`/`find <dir> -type f` and by checking whether each project
is referenced in the `.sln` before deciding whether this is "bootstrap from scratch," "finish an
abandoned scaffold," or "extend what's there." If real tests already exist, mirror their exact
style instead of the templates below — they're the actual ground truth for this repo, this skill
is only the fallback for when there's nothing to mirror yet.

If you're bootstrapping from nothing (or from an abandoned `xunit`-only scaffold), the
**recommended** stack — matching this template's overall style (primary-constructor DI, fluent
assertions elsewhere in the codebase's own conventions) and Milan Jovanović-style Modular
Monolith courses this template descends from — is **xUnit + FluentAssertions + NSubstitute** for
mocking, **NetArchTest.Rules** for architecture tests, **Testcontainers** + **Respawn** for
integration tests. Treat this as a recommendation to confirm with the user, not a pre-existing
fact about this repo — say explicitly that these are new dependencies being introduced, the same
way step 5 below already asks you to. Don't substitute a different framework (Moq, Shouldly,
NUnit, MSTest, etc.) unless the repo already uses it or the user asks. Below, the examples cover
a fictional `TodoItem` entity in a fictional `Tasks` module (a stand-in that won't collide with
any real entity in this repo) with a `CreateTodoItem` command — swap for whatever the user is
actually covering. `<ProjectName>` stands for this repo's actual root namespace/solution name.

## 0. One-time bootstrap (skip any project that already exists, is wired in, and has real code)

For each missing or empty-scaffold test project:
```
dotnet new xunit -o tests/<ProjectName>.<X>Tests -n <ProjectName>.<X>Tests
```
then set `TargetFramework` in the new `.csproj` to match this repo's `Directory.Build.props`
(don't leave whatever the template default is), then wire it into the solution:
```
dotnet sln <ProjectName>.sln add tests/<ProjectName>.UnitTests/<ProjectName>.UnitTests.csproj
dotnet sln <ProjectName>.sln add tests/<ProjectName>.ArchitectureTests/<ProjectName>.ArchitectureTests.csproj
dotnet sln <ProjectName>.sln add tests/<ProjectName>.IntegrationTests/<ProjectName>.IntegrationTests.csproj
```

Add `FluentAssertions` to every test project. Then, only for the project(s) you're actually
populating right now:

- **`<ProjectName>.UnitTests`** — add a `ProjectReference` to the module project(s) under test
  (e.g. `<ProjectName>.Modules.Tasks.Application`, `<ProjectName>.Modules.Tasks.Domain`) plus
  `<ProjectName>.Common.Domain`. Add `NSubstitute` for mocking repositories/`IUnitOfWork`/
  `IDateTimeProvider` if no mocking library exists yet — it pairs naturally with
  FluentAssertions and this template's primary-constructor DI style; don't introduce Moq unless
  the user asks for it.
- **`<ProjectName>.ArchitectureTests`** — add `NetArchTest.Rules` (if not present) and a
  `ProjectReference` to every assembly whose layering you want to assert
  (`<ProjectName>.Modules.Tasks.Domain/Application/Infrastructure/Presentation` for each module
  you're covering).
- **`<ProjectName>.IntegrationTests`** — add `Microsoft.AspNetCore.Mvc.Testing`,
  `Testcontainers.PostgreSql` (or whatever datastore this repo actually uses — check for Redis
  too, since a `Cart`/cache-backed feature needs a Testcontainers Redis image as well), and a
  `ProjectReference` to the API host project (needs `WebApplicationFactory<Program>` — check
  whether `Program.cs` needs a `public partial class Program {}` marker for the factory to see
  it, and add one if missing — top-level statement `Program.cs` files don't expose the class by
  default).

Code must build warning-free under this repo's `Directory.Build.props`/`.editorconfig` settings
(`TreatWarningsAsErrors`, `AnalysisMode=All`, mandatory file-scoped namespaces and braces on every
`if`) — write scaffolded test code in that style, not throwaway style.

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
For a handler that loads an existing aggregate first (e.g. `CompleteTodoItemCommandHandler`), add
one failure test per guard clause (not-found, business-rule violation) plus the happy path, and
mock any injected `IDateTimeProvider` explicitly rather than leaving it to wall-clock time:
```csharp
[Fact]
public async Task Handle_should_return_failure_when_todo_item_not_found()
{
    var command = new CompleteTodoItemCommand(Guid.NewGuid());
    _todoItemRepository.GetAsync(command.TodoItemId, Arg.Any<CancellationToken>()).Returns((TodoItem?)null);

    var handler = new CompleteTodoItemCommandHandler(_todoItemRepository, _dateTimeProvider, _unitOfWork);

    Result result = await handler.Handle(command, CancellationToken.None);

    result.IsFailure.Should().BeTrue();
    result.Error.Should().Be(TodoItemErrors.NotFound(command.TodoItemId));
}
```
Use xUnit's `[Fact]`/`[Theory]`, method names as
`MethodOrScenario_should_expectedBehavior_when_condition` (check `.editorconfig` for a `CA1707`
underscore-naming suppression scoped to test projects — add one if it's missing and the build
otherwise rejects underscored test names under `TreatWarningsAsErrors`). Build the arrange step
through the entity's real `Create`/behavior methods, never by reflection or bypassing invariants
— that's the whole point of testing against the domain model instead of a data bag.

## 2. Unit test — domain entity behavior

`tests/<ProjectName>.UnitTests/Modules/Tasks/TodoItems/TodoItemTests.cs`:
```csharp
using <ProjectName>.Common.Domain;
using <ProjectName>.Modules.Tasks.Domain.TodoItems;
using FluentAssertions;
using Xunit;

namespace <ProjectName>.UnitTests.Modules.Tasks.TodoItems;

public sealed class TodoItemTests
{
    [Fact]
    public void Create_should_raise_TodoItemCreatedDomainEvent()
    {
        TodoItem todoItem = TodoItem.Create("Buy milk", "2 liters, whole");

        todoItem.DomainEvents.Should().ContainSingle(e => e is TodoItemCreatedDomainEvent);
    }

    [Fact]
    public void Complete_should_return_failure_when_already_completed()
    {
        TodoItem todoItem = TodoItem.Create("Buy milk", "2 liters, whole");
        todoItem.Complete(DateTime.UtcNow);

        Result result = todoItem.Complete(DateTime.UtcNow);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(TodoItemErrors.AlreadyCompleted);
    }

    [Fact]
    public void ChangeTitle_should_not_raise_event_when_title_unchanged()
    {
        TodoItem todoItem = TodoItem.Create("Buy milk", "2 liters, whole");
        todoItem.ClearDomainEvents();

        todoItem.ChangeTitle("Buy milk");

        todoItem.DomainEvents.Should().BeEmpty();
    }
}
```
The no-op-behavior-method test (`ChangeTitle` above) is not optional filler — it's the concrete
proof for the "raise only when state actually changed" rule this template's `CLAUDE.md`
documents; write it for every behavior method that has an early-return guard.

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

    [Fact]
    public void Repositories_should_only_be_implemented_in_infrastructure()
    {
        TestResult result = Types.InAssembly(typeof(<ProjectName>.Modules.Tasks.Infrastructure.TasksModule).Assembly)
            .That()
            .ImplementInterface(typeof(<ProjectName>.Modules.Tasks.Domain.TodoItems.ITodoItemRepository))
            .Should()
            .ResideInNamespace(InfrastructureNamespace)
            .GetResult();

        result.IsSuccessful.Should().BeTrue();
    }
}
```
(If the module's Domain project has no `AssemblyReference` class, use
`typeof(<ProjectName>.Modules.Tasks.Domain.TodoItems.TodoItem).Assembly` instead, as above.) Add
one cross-module rule too, but only assert what this repo's `CLAUDE.md` actually documents as the
allowed exception (an `IntegrationEvents` project reference) — don't invent a `PublicApi`
assertion that would currently fail simply because those projects sit unused:
```csharp
[Fact]
public void Modules_should_not_reference_each_others_domain_or_application()
{
    // For each pair of modules, assert neither's Domain/Application namespace is referenced by
    // the other's Domain/Application/Infrastructure/Presentation — the only allowed cross-module
    // reference is <ModuleA>.Presentation depending on <ModuleB>.IntegrationEvents, per this
    // repo's CLAUDE.md §10.
}
```

## 4. Integration test — hitting an endpoint

`tests/<ProjectName>.IntegrationTests/Modules/Tasks/TodoItems/CreateTodoItemTests.cs`, backed by
a shared `WebApplicationFactory` fixture with real dependencies (DB, cache, etc.) via
Testcontainers (create `tests/<ProjectName>.IntegrationTests/<ProjectName>WebAppFactory.cs` once,
reuse via an xUnit `IClassFixture<<ProjectName>WebAppFactory>` / collection fixture across all
integration tests — don't spin up a container per test class):

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
recreating containers. Check whether this repo actually has authentication configured anywhere
(`AddAuthentication`/`[Authorize]`) before adding any auth/token setup to the test factory — per
this repo's `CLAUDE.md`, an `.AllowAnonymous()` call existing on one endpoint does **not** by
itself mean authentication is wired up; verify the real state rather than inferring it from a
single attribute.

## 5. After scaffolding

Run `dotnet test` on the affected project(s). If `FluentAssertions`/`NSubstitute`/
`NetArchTest.Rules`/`Testcontainers`/`Respawn` were just added, explicitly tell the user these are
new dependencies being introduced — this repo's own test projects, where they exist at all,
previously had nothing beyond a bare `dotnet new xunit` scaffold, so don't present this stack as
something already established here. Give the user the chance to veto a choice (e.g. prefer Moq
over NSubstitute) before it spreads across many files.
