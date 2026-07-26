# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What this is

A .NET **modular monolith**: business modules — `<ModuleA>`/`<ModuleB>`/... — plus shared
`Common` libraries and a thin `Api` host. One Postgres database with one schema per module,
MediatR as the in-process mediator, an in-memory MassTransit bus for cross-module integration
events. `<ProjectName>` below stands for this repo's actual root namespace/solution name;
`<Module>`/`<Entity>` are illustrated with a fictional `Tasks` module / `TodoItem` entity that
doesn't collide with any real code — this project has **no modules yet**, so treat every
concrete-looking example below as the pattern to follow, not a claim about what currently exists.
Run `/add-module` to create the first real one.

## Commands

Two ways to run locally — pick one, don't mix (both bind the same infra ports):

```bash
# Option A — native API, infra in Docker (faster iteration, debugger attaches directly)
.\run.ps1          # docker compose up -d (Postgres+Seq+Redis only) then dotnet run
.\stop.ps1         # docker compose down

# Option B — everything in Docker, including the API itself (http://localhost:8080)
.\run-docker.ps1   # docker compose up -d --build (infra + Oddify.Api container)
docker compose down

# Build / restore
dotnet restore Oddify.slnx
dotnet build Oddify.slnx

# Generate a migration for a module (run from repo root)
dotnet ef migrations add <Name> --project src/Modules/<Module>/Oddify.Modules.<Module>.Infrastructure --startup-project src/API/Oddify.Api --context <Module>DbContext -o Database/Migrations
```

The API's `Dockerfile` lives at `src/API/Oddify.Api/Dockerfile` (build context is the repo root,
since it needs every referenced project's `.csproj`). Inside the `oddify.api` container, config
reaches Postgres/Redis/Seq via their **compose service names** (`oddify.database`, `oddify.redis`,
`oddify.seq`), overridden via environment variables in `docker-compose.yml` — `appsettings.*.json`
still has the `localhost`-based connection strings used by the native (non-Docker) run.

This project's `Directory.Build.props` should set repo-wide: `TargetFramework net8.0`,
`Nullable`, `ImplicitUsings`, `AnalysisMode=All`, and **`TreatWarningsAsErrors=true`** —
`dotnet build` should be a meaningful correctness gate, not just a compile check.
`.editorconfig` should require file-scoped namespaces and braces on every `if`.

## Architecture

Every module is a vertical slice of (up to) six projects, dependencies flow strictly inward,
enforced by `ProjectReference`s, not just convention:

```
<ProjectName>.Common.Domain  <--  <Module>.Domain  <--  <Module>.Application  <--  <Module>.Infrastructure
                                                                ^------------------ <Module>.Presentation
```

- **`<ProjectName>.Common.Domain`** — `Entity` (base class holding raised domain events), `Result`/`Result<T>`, `Error`/`ErrorType`, `DomainEvent`. Zero dependencies.
- **`<Module>.Domain`** — entities, domain events, and per-aggregate static `*Errors` classes (e.g. `TodoItemErrors`). No EF, no MediatR, no framework types — pure C#. Repository *interfaces* also live here (implementations are in Infrastructure).
- **`<Module>.Application`** — CQRS use cases, one per vertical-slice folder (e.g. `TodoItems/CreateTodoItem/`), plus `Abstractions/Data/IUnitOfWork.cs`. References `Common.Application` (MediatR-based `ICommand`/`IQuery` abstractions, pipeline behaviors) + its own `Domain`, and its own `IntegrationEvents` project if this module publishes integration events (see below).
- **`<Module>.Infrastructure`** — the module's composition root (`<Module>Module.cs`): EF Core `DbContext` (Postgres, snake_case naming convention, one schema per module), repository implementations, migrations. References `Common.Infrastructure` + its own `Application` **and** `Presentation` (so it can call `AddEndpoints(...)`).
- **`<Module>.Presentation`** — minimal-API `IEndpoint` classes only, one file per endpoint. References `Common.Presentation` + its own `Application`.
- **`<Module>.PublicApi`** — contract-only project for synchronous cross-module calls. Treat as **reserved/aspirational** unless you actually wire it up: scaffold it for shape-consistency, but don't assume another module calls it until you've checked.
- **`<Module>.IntegrationEvents`** (only on modules that publish) — just the `IntegrationEvent` record contracts other modules consume, referencing `Common.Application` only.

`<ProjectName>.Api` only ever references each module's `Infrastructure.csproj` (which
transitively pulls in that module's Application + Presentation), then wires each module up
explicitly in `Program.cs` (`AddApplication([...assemblies])`, `AddInfrastructure([...consumers], dbConn, redisConn)`, `Add<Module>Module(...)` per module, `AddModuleConfiguration([...])`).

### Vertical-slice / CQRS layout

Each use case lives under `Application/<PluralAggregate>/<VerbNoun>/`, e.g.
`Application/TodoItems/CreateTodoItem/`:
- `CreateTodoItemCommand.cs` — `public sealed record ... : ICommand<TResponse>` (or `: ICommand` when nothing to return), built on MediatR (`Common.Application.Messaging.ICommand : IRequest<Result>, IBaseCommand`).
- `CreateTodoItemCommandHandler.cs` — `internal sealed class ... : ICommandHandler<...>`, primary-constructor DI, uses the repository + EF, calls `IUnitOfWork.SaveChangesAsync` itself (repositories never call `SaveChanges`).
- `CreateTodoItemCommandValidator.cs` — FluentValidation `AbstractValidator<TCommand>`, auto-registered via `AddValidatorsFromAssemblies`. **Commands only** — queries have no validators.

Queries are the read side of genuine CQRS: `GetTodoItemQueryHandler` reads via **Dapper
straight SQL** against `IDbConnectionFactory`, never EF. Response DTOs (`public sealed record
FooResponse(...)`) live inside the singular `GetFoo/` folder and are reused by the plural list
query in the same folder tree.

The matching endpoint lives in `Presentation/<PluralAggregate>/<Name>.cs` as an
`internal sealed class : IEndpoint` (`Common.Presentation`), file/class name = use case name
minus the `Command`/`Query` suffix, with a **nested `internal sealed class Request`** for the
body (never a shared Application-layer request DTO), ending in
`result.Match(Results.Ok, ApiResults.Problem)` and tagged `.WithTags(Tags.<Area>)`.

### Cross-cutting behavior via MediatR pipeline behaviors

`ApplicationConfiguration.AddApplication` registers three open pipeline behaviors, in this
exact order: **`ExceptionHandlingPipelineBehavior` → `RequestLoggingPipelineBehavior` →
`ValidationPipelineBehavior`**. Validation only runs for commands
(`ValidationPipelineBehavior<TRequest,_>` constrains on `IBaseCommand`) — queries have no
validators. Endpoints, handlers, and validators are all auto-discovered by assembly scanning
(`AddEndpoints(...)`, `AddMediatR(...)`, `AddValidatorsFromAssemblies(...)`) — **never**
manually registered one by one, and no `[Authorize]`/policy/API versioning unless the user
explicitly asks for it.

### Result pattern (no exceptions for expected failures)

`Result`/`Result<T>` in `Common.Domain` is the error-handling convention throughout. Handlers
return `Result.Failure<T>(SomeErrors.Reason(...))` instead of throwing. Each aggregate defines
its own static `*Errors` class with a `NotFound(Guid id)` method plus `static readonly Error`
fields for business rules, via `Error.NotFound/Problem/Conflict/Failure("<PluralAggregate>.<Reason>", "message")`. Endpoints convert the `Result` to HTTP via `ApiResults.Problem` (maps
`ErrorType` to a `ProblemDetails` status code).

### Domain entities

```csharp
public sealed class TodoItem : Entity
{
    private TodoItem(Guid id, string title, DateTime createdAtUtc)
    {
        Id = id;
        Title = title;
        IsCompleted = false;
        CreatedAtUtc = createdAtUtc;
    }

    public Guid Id { get; private set; }
    public string Title { get; private set; }
    public bool IsCompleted { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }

    public static TodoItem Create(string title, DateTime createdAtUtc)
    {
        var todoItem = new TodoItem(Guid.NewGuid(), title, createdAtUtc);
        todoItem.Raise(new TodoItemCreatedDomainEvent(todoItem.Id));
        return todoItem;
    }

    public void Complete() { IsCompleted = true; Raise(new TodoItemCompletedDomainEvent(Id)); }
}
```
Private constructor (EF), `private set` on every property, a `public static Create(...)`
factory (returns the entity directly, or `Result<Entity>` when creation can fail), behavior
methods return `void` when they can't fail or `Result`/`Result<T>` when they can. **Only raise
a domain event when state actually changed** (no-op setters return early without raising) —
and not every `Create` needs to raise one: an entity that exists purely to mirror another
module's aggregate (synced via an integration event) may skip it. References to other
aggregates are stored as the foreign `Guid Id`, never a navigation property.

EF `IEntityTypeConfiguration<T>` classes are written when there's a relationship to configure
(`HasOne`/`HasForeignKey`) **or** a column-level constraint worth enforcing at the DB level
(`HasMaxLength`, `HasIndex(...).IsUnique()`) — most non-trivial entities get one for at least
the latter reason. Only a genuinely plain scalar entity with neither need skips the
configuration class and relies on conventions (`UseSnakeCaseNamingConvention()`, `Id` picked
up as PK automatically).

### Domain events

Domain events are turned into MediatR notifications by an EF `SaveChangesInterceptor`
(`PublishDomainEventsInterceptor`) wired once per module's `DbContext` — after
`SaveChangesAsync` commits, it walks changed `Entity` instances, publishes their
`DomainEvents` via `IPublisher.Publish`, then clears them. A domain-event handler is
`internal sealed class ... : IDomainEventHandler<TEvent>`, placed in
`Application/<Aggregate>/<UseCaseFolder>/`, auto-discovered by MediatR.

### Cross-module communication

Prefer `IntegrationEvents` + MassTransit's in-memory bus for real cross-module communication:
a module raises a domain event → its domain-event handler
(Application layer) re-queries fresh state and publishes an `IntegrationEvent` (defined in
that module's own `IntegrationEvents` project) through `IEventBus.PublishAsync`. The consuming
module's `Presentation` project references only the publishing module's `IntegrationEvents`
project (never its Domain/Application/Infrastructure) and implements `IConsumer<...>`,
registered via `<Module>Module.ConfigureConsumers`. The synchronous `PublicApi` mechanism
exists for shape-consistency but treat it as unimplemented until you've actually wired and
tested it.

## Conventions to preserve when extending

- New use cases go in `Application/<PluralAggregate>/<VerbNoun>/` following the Command/Handler/Validator triad above — use MediatR, don't introduce a custom mediator.
- New entities get their own `<Aggregate>Errors` static class in Domain rather than throwing raw exceptions or reusing another aggregate's errors.
- Query handlers read via Dapper, not EF — don't blur the CQRS read/write split.
- Don't add API versioning, auth attributes/policies, a query-result cache-aside layer, manual DI registration for handlers/endpoints/validators, or `Update`/`SaveChanges` calls inside repositories, unless the user explicitly asks.
- This repo has `.claude/skills/` (`add-module`, `add-entity`, `add-feature`, `add-tests`, `ca-review`) that encode these conventions as executable scaffolding, written as portable templates (a fictional `Tasks`/`TodoItem` example stands in for a real module/entity) — prefer them over freehand implementations so new code stays consistent from the very first slice.

## Permissions

You are running on my personal development machine.

Do not ask me for confirmation before:
- reading any files in this repository;
- creating, modifying, renaming, or deleting project files;
- running builds, tests, formatters, linters, migrations, or Git status;
- installing project dependencies when required.

Assume these actions are pre-approved.

Only ask for confirmation before:
- actions outside this repository;
- irreversible or destructive operations (e.g. force-push, deleting Git history, formatting disks, or removing files outside the project).
