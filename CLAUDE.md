# CLAUDE.md

This file documents the architectural conventions of a **.NET Modular Monolith** template:
CQRS with MediatR, a `Result`-based error channel, EF Core for writes / Dapper for reads,
integration events over an in-memory bus, and a strict layered/vertical-slice project split.
It is written to be **fully portable**: every code example uses a fictional `Tasks` module with
a `TodoItem` aggregate, never a real domain, so this file can be dropped into any new Modular
Monolith .NET project as the authoritative style guide unchanged. `<ProjectName>` stands for the
real root namespace/solution name of whichever repo this file lives in.

Treat this document, and the `.claude/skills/` it references, as **the** source of truth for
how code in this kind of repository is written. New code should be indistinguishable from old
code, and — for a repo just starting to adopt this template — existing code should be migrated
toward it over time rather than this document being rewritten to match whatever the code
currently does. Where this document and a generic "clean architecture" opinion disagree, this
document wins. A handful of common pitfalls for this style of monolith are called out explicitly
in §15 rather than left for someone to rediscover the hard way.

## 1. Tech stack

- **.NET 8**, `Nullable` + `ImplicitUsings` enabled, `AnalysisLevel=latest` / `AnalysisMode=All`,
  **`TreatWarningsAsErrors=true`** and `EnforceCodeStyleInBuild=true` set once in
  `Directory.Build.props` for every project in the solution — `dotnet build` is a correctness
  gate, not just a compile check. `SonarAnalyzer.CSharp` is referenced repo-wide (excluded only
  for `.dcproj` docker-compose projects).
- `.editorconfig` is strict and mostly `:error` severity: **file-scoped namespaces**, **braces
  on every `if`** (no single-statement bodies without braces), `var` only when the type is
  apparent, expression-bodied members required for accessors/properties/indexers/operators,
  `this.`/`Me.` qualification forbidden, predefined type keywords (`string`, not `String`)
  required. Read the full file before writing code by hand instead of via a skill template.
- **MediatR** — in-process mediator for both commands/queries (request/response) and domain
  events (notifications).
- **FluentValidation** — commands only (see §7).
- **Dapper** — all query-side (read) database access.
- **EF Core + Npgsql**, `UseSnakeCaseNamingConvention()` (via `EFCore.NamingConventions`) — all
  command-side (write) database access. One Postgres database, **one schema per module**.
- **A hand-rolled in-memory event bus** (`InMemoryEventBus`, `Common.Infrastructure/EventBus/`) —
  the bus for cross-module integration events. A process-static `Subscribe`/`Publish` singleton,
  not a message-broker library: no transport, no redelivery, no persistence of its own. Modules
  only depend on `IOutboxWriter`/`IEventBus` (§10) — nothing outside `Common.Infrastructure`
  references `InMemoryEventBus` directly.
- **Redis** (`StackExchange.Redis` + `Microsoft.Extensions.Caching.StackExchangeRedis`) via a
  generic `ICacheService` — falls back to `AddDistributedMemoryCache()` in-process if the Redis
  connection throws at startup, so local dev works without a Redis container.
- **Serilog** (console/file/Seq sinks), request logging, and a per-request `LogContext` property
  (`Module`) pushed by a pipeline behavior (§6).
- **Swashbuckle** (Swagger/OpenAPI), **HealthChecks** (`AspNetCore.HealthChecks.NpgSql` +
  `.Redis` + `.UI.Client`), `ProblemDetails` for error responses.
- Minimal APIs — no MVC controllers anywhere.
- IDs are `Guid`s generated in the domain factory (`Guid.NewGuid()`), never database-generated.

## 2. Solution layout

```
src/
  API/<ProjectName>.Api/                         — the only executable; thin composition host
  Common/
    <ProjectName>.Common.Domain/                 — zero dependencies
    <ProjectName>.Common.Application/             — depends on Common.Domain
    <ProjectName>.Common.Infrastructure/          — depends on Common.Application
    <ProjectName>.Common.Presentation/            — depends on Common.Domain
  Modules/
    <Module>/
      <ProjectName>.Modules.<Module>.Domain/
      <ProjectName>.Modules.<Module>.Application/
      <ProjectName>.Modules.<Module>.Infrastructure/
      <ProjectName>.Modules.<Module>.Presentation/
      <ProjectName>.Modules.<Module>.PublicApi/            — optional, see §11
      <ProjectName>.Modules.<Module>.IntegrationEvents/    — optional, only if this module publishes (§10)
tests/
  <ProjectName>.UnitTests/
  <ProjectName>.ArchitectureTests/
  <ProjectName>.IntegrationTests/
```

Every module is a vertical slice of up to six projects. Dependencies flow strictly inward and
this is enforced by actual `ProjectReference`s, not just convention:

```
Common.Domain  <--  <Module>.Domain  <--  <Module>.Application  <--  <Module>.Infrastructure
                                                  ^-------------------- <Module>.Presentation
```

| Project | References |
|---|---|
| `<Module>.Domain` | `Common.Domain` only |
| `<Module>.Application` | `Common.Application` + own `Domain` (+ own `IntegrationEvents` if it publishes; + another module's `PublicApi` project **only** if this repo has deliberately wired `PublicApi` up for real synchronous cross-module calls — see §11, and verify before assuming either way) |
| `<Module>.Infrastructure` | `Common.Infrastructure` + own `Application` **and** own `Presentation` (needs both: EF/repos from Application's abstractions, and `AddEndpoints(...)` from Presentation) |
| `<Module>.Presentation` | `Common.Presentation` + own `Application` (+ another module's `IntegrationEvents` project **only**, if this module consumes that module's events — never that module's Domain/Application/Infrastructure/Presentation) |
| `<Module>.PublicApi` | **nothing** — not even `Common.Domain` (see §11) |
| `<Module>.IntegrationEvents` | `Common.Application` only |

`<ProjectName>.Api` references only each module's `Infrastructure.csproj` (which transitively
pulls in that module's `Application` + `Presentation`) — never a module's `Domain`/`Application`
project directly.

**Cross-module rule:** a module's `Domain`/`Application`/`Infrastructure` project never
references another module's `Domain`/`Application`/`Infrastructure`/`Presentation` project — never
Domain, Application, Infrastructure, or Presentation directly. There are exactly two sanctioned
exceptions, both to a *contract-only* project, never to another module's real implementation
layers: one module's `Presentation` project depending on another module's `IntegrationEvents`
project to implement `IIntegrationEventHandler<TheirEvent>` (§10), and one module's `Application` project
depending on another module's `PublicApi` project for a synchronous read (§11) — the latter only
in a repo that has actually wired `PublicApi` up for real; verify before assuming either way.

## 3. Common.Domain — the shared kernel

Zero dependencies (not even MediatR — its `INotification` marker is referenced, but that's it).

**`Entity`** — base class for every aggregate/entity:
```csharp
public abstract class Entity
{
    private readonly List<IDomainEvent> _domainEvents = [];

    protected Entity() { }

    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.ToList();

    public void ClearDomainEvents() => _domainEvents.Clear();

    protected void Raise(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);
}
```

**`IDomainEvent : INotification`** and **`DomainEvent`** (abstract base with `Guid Id` +
`DateTime OccurredOnUtc`, both auto-populated in the parameterless protected constructor).

**`Result` / `Result<TValue>`** — the error-handling convention for the entire codebase. No
exceptions for expected/business failures.
```csharp
public class Result
{
    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public Error Error { get; }

    public static Result Success() => new(true, Error.None);
    public static Result<TValue> Success<TValue>(TValue value) => new(value, true, Error.None);
    public static Result Failure(Error error) => new(false, error);
    public static Result<TValue> Failure<TValue>(Error error) => new(default, false, error);
}

public class Result<TValue> : Result
{
    [NotNull] public TValue Value => IsSuccess ? _value! : throw new InvalidOperationException(...);

    public static implicit operator Result<TValue>(TValue? value) =>
        value is not null ? Success(value) : Failure<TValue>(Error.NullValue);

    public static Result<TValue> ValidationFailure(Error error) => new(default, false, error);
}
```
The constructor throws `ArgumentException` if `isSuccess`/`error` are inconsistent (success with
a real error, or failure with `Error.None`) — this makes an invalid `Result` unrepresentable.
The implicit operator lets a handler `return someNullableEntity;` and get `NullValue` failure for
free, but this codebase's handlers overwhelmingly return explicit `Result.Failure(XErrors.NotFound(id))`
instead — prefer the explicit form for anything with a real not-found error to report.

**`Error`** — a record with `Code`, `Description`, `ErrorType`, plus static factories:
`Error.Failure(code, description)`, `Error.NotFound(...)`, `Error.Problem(...)`,
`Error.Conflict(...)`. `Error.None` and `Error.NullValue` are the two built-in sentinels.

**`ErrorType`** enum: `Failure = 0, Validation = 1, Problem = 2, NotFound = 3, Conflict = 4`.

**`ValidationError : Error`** (sealed record) — wraps an `Error[]` of individual FluentValidation
failures, code `"General.Validation"`, built via `ValidationError.FromResults(IEnumerable<Result>)`
or directly from `ValidationFailure[]` in the pipeline behavior (§7).

## 4. Common.Application — cross-cutting application abstractions

**Messaging markers** (all thin interfaces over MediatR's `IRequest`/`INotificationHandler`):
```csharp
public interface IBaseCommand;
public interface ICommand : IRequest<Result>, IBaseCommand;
public interface ICommand<TResponse> : IRequest<Result<TResponse>>, IBaseCommand;

public interface ICommandHandler<in TCommand> : IRequestHandler<TCommand, Result> where TCommand : ICommand;
public interface ICommandHandler<in TCommand, TResponse> : IRequestHandler<TCommand, Result<TResponse>> where TCommand : ICommand<TResponse>;

public interface IQuery<TResponse> : IRequest<Result<TResponse>>;
public interface IQueryHandler<in TQuery, TResponse> : IRequestHandler<TQuery, Result<TResponse>> where TQuery : IQuery<TResponse>;

public interface IDomainEventHandler<in TDomainEvent> : INotificationHandler<TDomainEvent> where TDomainEvent : IDomainEvent;
```
`IBaseCommand` exists purely so the validation pipeline behavior can constrain itself to commands
without also matching queries (queries have no `IBaseCommand`).

**`ApplicationConfiguration.AddApplication(IServiceCollection, Assembly[] moduleAssemblies)`** —
called **once** from the API host with every module's `Application` assembly. Registers MediatR
(`AddMediatR`) with three **open pipeline behaviors, in this exact order**:
1. `ExceptionHandlingPipelineBehavior<,>`
2. `RequestLoggingPipelineBehavior<,>`
3. `ValidationPipelineBehavior<,>`

Then `AddValidatorsFromAssemblies(moduleAssemblies, includeInternalTypes: true)` — validators are
`internal`, so this flag is required or none of them would be discovered.

**Pipeline behaviors** (`Common.Application/Behaviors/`, all `internal sealed`):
- `ExceptionHandlingPipelineBehavior<TRequest, TResponse>` — try/catch around `next()`; logs and
  rethrows as a wrapped application exception (`AppException(typeof(TRequest).Name, innerException: exception)`)
  carrying the request type name and (optionally) an `Error`. This is a **last-resort safety
  net**, not the primary error channel — the primary channel is returning `Result.Failure(...)`
  from the handler itself.
- `RequestLoggingPipelineBehavior<TRequest, TResponse>` (`where TResponse : Result`) — pushes a
  Serilog `LogContext` property `Module` (extracted as `typeof(TRequest).FullName!.Split('.')[2]`
  — i.e. the third namespace segment, `<ProjectName>.Modules.<Module>...`), logs "Processing
  request", then "Completed request" on success or "Completed request ... with error" (pushing
  the `Error` into the log context too) on `Result.IsFailure`. Uses `[LoggerMessage]`
  source-generated logging methods (`partial` class), not raw `logger.LogInformation(...)` calls.
- `ValidationPipelineBehavior<TRequest, TResponse>` (`where TRequest : IBaseCommand`) — resolves
  `IEnumerable<IValidator<TRequest>>`, short-circuits to `next()` if there are none or all pass,
  otherwise builds a `ValidationError` from the failures and returns it as a `Result`/`Result<T>`
  failure **without invoking the handler**. Uses reflection (`Result<>.ValidationFailure` looked
  up via `MakeGenericType`) to construct the right generic `Result<T>` shape — this is the one
  place in the template where reflection substitutes for a cleaner generic constraint, because
  `TResponse` can be either `Result` or `Result<T>` for the same behavior instance.

**`EventBus/`** — `IIntegrationEvent` (`Guid Id`, `DateTime OccurredOnUtc`), the abstract
`IntegrationEvent` base class implementing it, and `IEventBus.PublishAsync<T>(T,
CancellationToken)` — a direct, non-durable publish straight onto the bus; the concrete
`InMemoryEventBus`-backed implementation lives in `Common.Infrastructure` (§5). Legitimate for an
event where losing it on a crash between commit and publish is genuinely acceptable, but that's a
deliberate exception, not the default — see `Outbox/` below.

**`Outbox/`** — `IOutboxWriter.Enqueue<T>(T integrationEvent) where T : IIntegrationEvent` — the
**default** abstraction modules use to publish (§10). The concrete EF-backed writer, the shared
`OutboxMessage` entity, and the Quartz job that actually drains it onto `InMemoryEventBus` all
live in `Common.Infrastructure` (§5).

**`Data/IDbConnectionFactory`** — `ValueTask<DbConnection> OpenConnectionAsync()`. The only way
query handlers touch the database.

**`Caching/ICacheService`** — `GetAsync<T>`/`SetAsync<T>(..., TimeSpan? expiration)`/`RemoveAsync`,
generic JSON-serialized cache-aside over `IDistributedCache`.

**`Clock/IDateTimeProvider`** — `DateTime UtcNow { get; }`. Inject this instead of calling
`DateTime.UtcNow` directly inside a **handler** when "now" is a decision input worth controlling
in a test (e.g. comparing against an entity's timestamp) — entities themselves may still take
`DateTime` as a plain constructor/factory parameter (see §8) rather than depending on this
interface, since Domain has zero dependencies and can't reference an Application abstraction.

**`Exceptions/AppException`** — the one custom exception type, thrown only by
`ExceptionHandlingPipelineBehavior` and, in one specific place, by a domain-event handler that
needs to escalate an unexpected `Result.Failure` into a hard failure (§10) since a notification
handler has no `Result` return channel of its own.

## 5. Common.Infrastructure — the shared plumbing

**`InfrastructureConfiguration.AddInfrastructure(IServiceCollection, IConfiguration configuration, string databaseConnectionString, string redisConnectionString)`**
— called once from the API host. Registers, in order:
1. A singleton `NpgsqlDataSource` (built once from the connection string) + scoped
   `IDbConnectionFactory` → `DbConnectionFactory` (wraps `dataSource.OpenConnectionAsync()`).
2. Singleton `InsertOutboxMessagesInterceptor` (see below).
3. Singleton `IDateTimeProvider` → `DateTimeProvider` (`DateTime.UtcNow` passthrough).
4. Redis: `try` to `ConnectionMultiplexer.Connect(redisConnectionString)` and register
   `AddStackExchangeRedisCache`; on **any** exception, silently fall back to
   `AddDistributedMemoryCache()`. Either way, `ICacheService` → `CacheService` is registered on
   top.
5. Singleton `IEventBus` → `EventBus` (`Common.Infrastructure/EventBus/`), a thin client that
   delegates every call to the process-static `InMemoryEventBus.Instance` (`Subscribe`/`Publish`
   in a `Dictionary<string, List<IIntegrationEventHandler>>` — no transport, no redelivery). This
   is a direct, non-durable publish — see the Outbox subsection below for why `IOutboxWriter` is
   the *default* choice and when reaching for `IEventBus` directly is still legitimate.
6. Quartz bootstrap shared by every module's jobs: `services.Configure<OutboxProcessorOptions>(...)`,
   `AddQuartz()`, `AddQuartzHostedService(...)`, and `AddHostedService<OutboxCleanupBackgroundService>()`.
   Each module contributes its **own** outbox/inbox job from its own composition root (§12) via
   `AddOutboxProcessor(schema)`/`AddInboxProcessor(schema, presentationAssembly)` — nothing about
   which modules publish or consume is threaded through `AddInfrastructure` itself.

**`InsertOutboxMessagesInterceptor : SaveChangesInterceptor`** (`Common.Infrastructure/Outbox/`) —
a **real, transactional outbox capture**, not just an in-process notification. It hooks
`SavingChanges`/`SavingChangesAsync` (*before* the underlying `SaveChanges` commits, not after),
sweeps every tracked `Entity`'s `DomainEvents`, and inserts one `OutboxMessage` row per event into
the *same* `DbContext`/transaction that's about to commit:
```csharp
public sealed class InsertOutboxMessagesInterceptor : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        InsertOutboxMessages(eventData.Context!);
        return base.SavingChanges(eventData, result);
    }

    private static void InsertOutboxMessages(DbContext context)
    {
        var outboxMessages = context.ChangeTracker.Entries<Entity>()
            .Select(entry => entry.Entity)
            .SelectMany(entity => { var events = entity.DomainEvents; entity.ClearDomainEvents(); return events; })
            .Select(domainEvent => new OutboxMessage(
                domainEvent.Id, domainEvent.GetType().AssemblyQualifiedName!,
                JsonSerializer.Serialize(domainEvent, domainEvent.GetType(), EventSerializerOptions.Instance),
                domainEvent.OccurredOnUtc))
            .ToList();

        context.Set<OutboxMessage>().AddRange(outboxMessages);
    }
}
```

`Outbox/`, `Inbox/`, `EventBus/` (both `Common.Application` and `Common.Infrastructure`) and
`Processing/` (`ICommandsScheduler`/`InternalCommand`) are deliberately written in the structural
style of the reference project (Modular Monolith with DDD, Kamil Grzybek) rather than this
template's own Milan/Evently conventions: block-scoped namespaces, `sealed` only where he uses it
(the `InMemoryEventBus` singleton), and a traditional constructor (`private readonly` field +
assignment in the body) instead of a primary constructor or a static `Create` factory. Each of
these folders carries a local `.editorconfig` relaxing `csharp_style_namespace_declarations` (and
`CA1852`/`S3260` where needed) to make that legal — don't "fix" these files back to file-scoped
namespaces or primary constructors; that would undo a deliberate choice, not correct a mistake.
The actual wiring underneath still uses this project's own stack (Microsoft DI, not Autofac; EF
Core for writes and `System.Text.Json`, not Dapper writes and Newtonsoft.Json) and the async
outbox+job dispatch for domain events, not Kamil's synchronous decorator-based dispatch — only the
messaging code's shape/style mirrors his project, not the DI container or the dispatch model.
Registered once per module's `DbContext` via `.AddInterceptors(sp.GetRequiredService<InsertOutboxMessagesInterceptor>())`
in that module's composition root (§12). Because the outbox row is added to the *same*
`ChangeTracker`/transaction as the business write, "the write succeeded" and "the event got
durably queued" succeed or fail together — there is no window where one commits without the
other. This is genuinely the persisted, at-least-once mechanism (there is no separate
"non-durable" domain-event path to confuse it with).

**Outbox (`Common.Infrastructure/Outbox/`)** — shared by every module that publishes this way (not
module-specific; don't reintroduce a per-module copy):
- **`IOutboxWriter.Enqueue<T>(T integrationEvent)`** (`Common.Application`) — for an integration
  event a command handler builds explicitly (not just a domain event the interceptor already
  captures automatically). Enqueues onto the `DbContext`'s change tracker only; the caller still
  has to call its own `IUnitOfWork.SaveChangesAsync(...)` for it to actually persist — same
  durability guarantee as `InsertOutboxMessagesInterceptor` above, just an explicit second path.
  `IEventBus.PublishAsync(...)` (above) is still available as a direct, non-durable publish — a
  deliberate choice for an event where losing it on a crash between commit and publish is
  genuinely acceptable, never the default reach just because it's one line shorter. If you're not
  sure which one a new event needs, it needs `IOutboxWriter`.
- **`OutboxMessage`** (`Common.Infrastructure`) — `Id` (same `Id` as the underlying domain/
  integration event, never a new `Guid`), `Type` (`AssemblyQualifiedName`), `Content` (JSON,
  `jsonb` column), `OccurredOnUtc`, `ProcessedOnUtc`. A module that publishes adds a
  `DbSet<OutboxMessage>` and an explicit `modelBuilder.ApplyConfiguration(new OutboxMessageConfiguration())`
  in its own `DbContext` (this configuration lives in `Common.Infrastructure`, so the module's own
  `ApplyConfigurationsFromAssembly` scan doesn't pick it up automatically) — the table lands in
  that module's own schema, same as every other table it owns. No shared cross-module
  `outbox_messages` table, ever, not even for plumbing like this — same cross-module rule as §2.
- **`EfOutboxWriter<TContext>`** — the `IOutboxWriter` implementation, generic over the module's
  own `DbContext` type. A module registers it with `services.AddOutboxWriter<TasksDbContext>()`
  (§12) rather than referencing the concrete writer type directly.
- **`OutboxModule(string Schema)`** — one DI-registered instance per publishing module, contributed
  by that module's own `services.AddOutboxProcessor(Schemas.Tasks)` call (§12), not built centrally
  in `Program.cs`. `Schema` only ever comes from code the host itself controls, never from a
  request — that is what makes it safe for `OutboxProcessorJob`/`OutboxCleanupBackgroundService` to
  interpolate it directly into SQL table names instead of parameterizing it.
- **`OutboxProcessorJob`** — one Quartz job instance per `OutboxModule` (distinct `JobKey`, `Schema`
  passed via `JobDataMap`). Polls `SELECT ... FROM {schema}.outbox_messages WHERE processed_on_utc
  IS NULL ORDER BY occurred_on_utc` — no batching, no row locking, no retry/failure tracking:
  every unprocessed row is read and processed one at a time, in order. Resolves the CLR type via
  `Type.GetType(message.Type)`; if it's an `IIntegrationEvent` (explicitly `IOutboxWriter`-enqueued),
  publishes straight onto `InMemoryEventBus.Instance` — never through MediatR. Otherwise it's a
  domain event captured automatically by the interceptor: deserializes to `IDomainEvent` and
  dispatches it locally via MediatR's `IPublisher.Publish` (in-module `IDomainEventHandler<T>`
  reactions only — if one of those needs to notify another module, it calls `IEventBus.PublishAsync(...)`
  itself, from inside its own `Handle`). Each row is marked `processed_on_utc = now()` right after
  it's handled; if handling throws, the exception propagates out of `Execute` and that row (and any
  still-unprocessed rows after it in the same poll) stays pending for the next tick — a poisoned
  message blocks nothing but itself and needs to be fixed and left to drain on the next poll, not
  chased with automatic retries.
- **`OutboxCleanupBackgroundService`** — one shared instance (not one per module) that loops over
  every `OutboxModule`/`InboxModule` on an interval (`OutboxProcessorOptions.CleanupInterval`) and
  deletes processed rows older than `RetentionPeriod` from both `outbox_messages` and
  `inbox_messages`.

**`CacheService`** — `System.Text.Json` (`Utf8JsonWriter` + `ArrayBufferWriter<byte>`) serialize/
deserialize over `IDistributedCache`'s byte-array API.

## 6. Common.Presentation — minimal-API glue

**`IEndpoint`** — `void MapEndpoint(IEndpointRouteBuilder app);`. One implementation per
endpoint, always `internal sealed class`.

**`EndpointExtensions`**:
- `AddEndpoints(this IServiceCollection, params Assembly[] assemblies)` — reflection-scans the
  given assemblies for non-abstract, non-interface types assignable to `IEndpoint`, registers
  each as `ServiceDescriptor.Transient(typeof(IEndpoint), type)` via `TryAddEnumerable`. Called
  once per module, from that module's composition root, with only that module's own
  `Presentation` assembly.
- `MapEndpoints(this WebApplication app, RouteGroupBuilder? routeGroupBuilder = null)` — resolves
  every registered `IEndpoint` and calls `MapEndpoint` on each. Called once from `Program.cs`
  after every module has been wired.

**No manual registration ever** — a new endpoint class in a module's `Presentation` project is
picked up automatically the next time the app starts. If it isn't showing up, the bug is a
namespace/assembly mismatch, not a missing registration line.

**`ApiResults.Problem(Result result)`** — converts a **failed** `Result` (throws
`InvalidOperationException` if called on a success) to `Microsoft.AspNetCore.Http.Results.Problem(...)`,
mapping `ErrorType` → HTTP status + RFC 7231 problem `type` URI:

| `ErrorType` | Status code |
|---|---|
| `Validation` | 400 |
| `Problem` | 400 |
| `NotFound` | 404 |
| `Conflict` | 409 |
| `Failure` (default) | 500 |

For `ValidationError` specifically, adds an `errors` extension containing the underlying
`Error[]`.

**`ResultExtensions.Match`** — the standard way every endpoint converts a `Result`/`Result<T>` to
an `IResult`:
```csharp
result.Match(Results.Ok, ApiResults.Problem);                    // Result<T>, success → 200 + body
result.Match(() => Results.Ok(), ApiResults.Problem);             // Result, success → 200 empty
result.Match(Results.NoContent, ApiResults.Problem);               // Result, success → 204 empty
```
**Repos following this template can end up inconsistent between the last two** for "command with
no return value" — some update/state-transition endpoints return `Results.Ok()`, others
`Results.NoContent()`, with neither one enforced as a house style. When adding a new one, match
the sibling endpoints already in the module/aggregate you're extending; if there's no sibling to
match, prefer `Results.NoContent()` (more correct REST semantics for a body-less success) unless
the user asks otherwise.

## 7. Vertical-slice / CQRS layout

Every use case lives under `Application/<PluralAggregate>/<VerbNoun>/`, e.g.
`Application/TodoItems/CreateTodoItem/`. See `.claude/skills/add-feature/SKILL.md` for the full
worked example (command with/without a return value, single-item query, list query, paginated
search query). The shape, summarized:

- **`<VerbNoun>Command.cs`** — `public sealed record ... : ICommand<TResponse>` (or `: ICommand`
  when there's nothing to return).
- **`<VerbNoun>CommandHandler.cs`** — `internal sealed class ... : ICommandHandler<...>`,
  primary-constructor DI (repository + `IUnitOfWork`, plus `IDateTimeProvider` when "now" matters).
  Loads the aggregate via the repository (or constructs a new one), calls a domain method/factory,
  calls `unitOfWork.SaveChangesAsync(cancellationToken)` itself — **repositories never call
  `SaveChanges`**.
- **`<VerbNoun>CommandValidator.cs`** — FluentValidation `AbstractValidator<TCommand>`, `internal
  sealed class`. **Commands only** — a query never gets a validator file (the validation pipeline
  behavior only runs for `IBaseCommand`).
- **`<Noun>Response.cs`** (in the singular `Get<Noun>/` folder) — `public sealed record` DTO,
  reused by that aggregate's other queries (list/search) via a namespace-qualified reference
  rather than redefined.
- **`Get<Noun>Query.cs`** / **`Get<Noun>QueryHandler.cs`** — reads via **Dapper**, never EF,
  against `IDbConnectionFactory`. SQL uses `nameof(Response.Property)` as every column alias so
  the projection stays compiler-checked against the response shape, and passes the request
  record itself as the Dapper parameters object when property names line up with `@Param`
  placeholders.
- A **paginated/filtered search query** (e.g. `Search<Noun>Query(filters..., int Page, int
  PageSize) : IQuery<Search<Noun>Response>`) runs two SQL statements against the same filter
  predicate — one `SELECT ... OFFSET @Skip LIMIT @Take` for the page, one `SELECT COUNT(*)` for
  the total — through a `private sealed record <Noun>Parameters(...)` that pre-computes
  `Skip = (Page - 1) * PageSize` once and is passed as the Dapper parameter object to both
  statements. Returns a `Response(int Page, int PageSize, int TotalCount, IReadOnlyCollection<Item> Items)`.

The matching endpoint lives in `Presentation/<PluralAggregate>/<Name>.cs` (file/class name = use
case name minus `Command`/`Query`), `internal sealed class : IEndpoint`, with a **nested
`internal sealed class Request`** for the body (never a shared Application-layer request DTO),
ending in `result.Match(...)` (§6) and tagged `.WithTags(Tags.<Area>)`. Route-param-only inputs
(`Guid id`) bind as a plain minimal-API lambda parameter matched to `{id}` — no `[FromRoute]`.
Query-string inputs on a `GET` (filters, `page`, `pageSize`) bind the same way, as plain lambda
parameters with C# default values doubling as the endpoint's defaults
(`int page = 0, int pageSize = 15`).

## 8. Domain entities

```csharp
public sealed class TodoItem : Entity
{
    private TodoItem() { }

    public Guid Id { get; private set; }
    public string Title { get; private set; }
    public string Description { get; private set; }
    public bool IsCompleted { get; private set; }
    public DateTime? CompletedAtUtc { get; private set; }

    public static TodoItem Create(string title, string description)
    {
        var todoItem = new TodoItem
        {
            Id = Guid.NewGuid(),
            Title = title,
            Description = description,
            IsCompleted = false
        };

        todoItem.Raise(new TodoItemCreatedDomainEvent(todoItem.Id));

        return todoItem;
    }

    public Result Complete(DateTime completedAtUtc)
    {
        if (IsCompleted)
        {
            return Result.Failure(TodoItemErrors.AlreadyCompleted);
        }

        IsCompleted = true;
        CompletedAtUtc = completedAtUtc;

        Raise(new TodoItemCompletedDomainEvent(Id));

        return Result.Success();
    }

    public void ChangeTitle(string title)
    {
        if (Title == title)
        {
            return;
        }

        Title = title;

        Raise(new TodoItemTitleChangedDomainEvent(Id, Title));
    }
}
```

Rules, no exceptions observed anywhere in this template:
- `sealed class : Entity`, private (parameterless, or taking required fields — match sibling
  entities in the same module) constructor for EF, every settable property `{ get; private set; }`.
- A `public static Create(...)` factory. Returns the entity directly when creation can't fail,
  or `Result<TEntity>` when it can (a cross-field invariant like "end can't precede start").
  IDs are generated inside the factory with `Guid.NewGuid()` — never left to the database.
- Behavior methods return `void` when they truly cannot fail, `Result`/`Result<T>` when they can.
- **Raise a domain event only when state actually changed** — a no-op setter (new value equals
  current value) returns early, before `Raise(...)`, as `ChangeTitle` does above.
- **Not every `Create` raises a "created" event**, and the exceptions fall into two different
  shapes — know which one applies before skipping it:
  1. An entity that exists purely to **mirror another module's aggregate**, synced by a
     domain-event handler reacting to an integration event (e.g. a local `Customer`-style
     read-mirror created from a `UserRegistered`-style integration event) — it has no
     independent "created" meaning of its own, so it raises nothing.
  2. A **child entity created as a detail of a larger aggregate-root operation** (e.g. a
     `TodoChecklistItem`-style line-item created via `TodoItem.AddChecklistItem(...)` or its own
     `CreateChecklistItem` use case, but always scoped under a parent `TodoItem`) — watch out for
     a common failure mode here: defining a `TodoChecklistItemCreatedDomainEvent` class for this
     case and then forgetting to actually call `Raise(...)` for it anywhere, and never writing a
     handler either. A defined-but-dead event class is easy to introduce by accident and easy to
     miss in review — **don't let it happen silently**. Default to raising the created event for
     a standalone child entity unless you have a specific reason a subscriber would never care.
- References to another aggregate (in the same or a different module) are stored as a bare
  `Guid <Name>Id`, **never** a navigation property — this is true even *within* a module across
  two different aggregate roots (e.g. `TodoItem.ProjectId`, not `TodoItem.Project`).
- A collection of child entities owned by an aggregate root is a `private readonly List<TChild>
  _children = [];` backing field, exposed as `public IReadOnlyCollection<TChild> Children =>
  _children.ToList();`, mutated only through a method on the aggregate root (`AddItem(...)`,
  never `order.OrderItems.Add(...)` from outside).

## 9. Domain events, errors, and EF configuration

**Domain events** — one file per event, same folder/namespace as the entity, named
`{Entity}{PastTenseVerb}DomainEvent`, `sealed class` with a **primary constructor** whose
parameter(s) become `{ get; init; }`-mapped properties (not a `record` — every domain event in
this template is a class):
```csharp
public sealed class TodoItemCreatedDomainEvent(Guid todoItemId) : DomainEvent
{
    public Guid TodoItemId { get; init; } = todoItemId;
}
```
A `IDomainEventHandler<TEvent>` (`internal sealed class`) lives in
`Application/<Aggregate>/<UseCaseFolder>/` (the folder of whichever use case's behavior raises
that event), auto-discovered by MediatR. **A handler is optional, not mandatory** — it's normal
and expected for most domain events in a codebase following this template to have **zero**
handlers at any given time (raised for future extensibility, currently inert), and a handler
that intentionally does nothing yet is a legitimate no-op stub (`return Task.CompletedTask;`),
not a bug. Don't assume every domain event needs a handler; only add one when something must
actually react.

**Domain errors** — one `static class <Aggregate>Errors` per aggregate in `Domain/<Aggregate>/`:
```csharp
public static class TodoItemErrors
{
    public static Error NotFound(Guid todoItemId) =>
        Error.NotFound("TodoItems.NotFound", $"The todo item with the identifier {todoItemId} was not found");

    public static readonly Error AlreadyCompleted = Error.Problem(
        "TodoItems.AlreadyCompleted",
        "The todo item is already completed");
}
```
Always include `NotFound(Guid)` when a "get-or-fail" path exists for that aggregate — it's the
standard error every command handler returns after a `null` repository lookup. Code format is
`"<PluralAggregate>.<PascalCaseReason>"`. Choose the factory by what actually went wrong:
`Error.Problem` for a business-rule violation, `Error.Conflict` for a uniqueness/concurrency
clash, `Error.NotFound` only for the identifier-lookup case, `Error.Failure` only for a truly
generic/unexpected failure. Never reuse one aggregate's errors for another, even for an
identical-sounding rule.

**EF `IEntityTypeConfiguration<T>`** — write one when the entity has a relationship to configure
(`builder.HasOne<Other>().WithMany()...`, always by convention with no navigation collection on
the "one" side unless the aggregate genuinely owns that collection) **or** a column-level
constraint worth enforcing at the DB level (`HasMaxLength`, `HasIndex(...).IsUnique()`). Most
non-trivial entities end up with one for at least the latter reason. Skip the configuration class
entirely only for a genuinely plain scalar entity with neither need — conventions alone
(`UseSnakeCaseNamingConvention()`, `Id` auto-picked-up as PK) cover it.

**Migrations** — always `dotnet ef migrations add <Name> --project src/Modules/<Module>/<ProjectName>.Modules.<Module>.Infrastructure --startup-project src/API/<ProjectName>.Api --context <Module>DbContext -o Database/Migrations`,
run from the repo root. Never hand-edit a generated migration or its `.Designer.cs`/
`ModelSnapshot.cs` — if the generated shape is wrong, fix the entity/configuration and
regenerate. Each module's DbContext calls `modelBuilder.HasDefaultSchema(Schemas.<Module>)` and
its migrations history table lives in that same schema
(`.MigrationsHistoryTable(HistoryRepository.DefaultTableName, Schemas.<Module>)`), so one
module's migrations never collide with another's.

## 10. Cross-module communication (integration events)

The **only working** cross-module mechanism in this template is: domain event → **outbox write**
(§5, automatic via `InsertOutboxMessagesInterceptor`) → that module's `OutboxProcessorJob` →
`InMemoryEventBus` → the consuming module's own generic inbox-writer handler → that module's
**own** `inbox_messages` table → that module's `ProcessInboxJob` → the real
`IIntegrationEventHandler<T>` in its `Presentation` project → a command in that module. (A
`PublicApi` synchronous-call project also exists per module — see §11 — but is a separate,
currently-unimplemented mechanism; don't conflate the two.)

**Why this is always asynchronous, never a synchronous call into another module's command
surface**: the publishing module has no business reason to know how — or whether — any other
module reacts to something that happened in its own domain; more than one module might subscribe
to the same event; and making the original request's success depend on a downstream reaction it
doesn't own would be the wrong coupling regardless of how reliable that downstream module is
today. The one sanctioned *synchronous* crossing between modules is a **read** through a
`PublicApi` project (§11) — never a write, and never something that can fail the read-only side of
a request because another module's business logic failed. If you ever find yourself wanting a
module to call another module's command synchronously and wait for the result, that is a signal
the two modules are more tightly coupled than this architecture assumes — reconsider the module
boundary itself rather than adding a synchronous escape hatch here.

1. An aggregate's behavior method raises a **domain event** (§9).
2. A **domain-event handler**, in the *publishing* module's `Application` layer, needs to react.
   If it needs to publish an integration event, it typically **re-queries fresh state** (via
   `ISender.Send(new Get<X>Query(...))`) rather than trusting the notification's own payload,
   since by the time the notification runs other things may have already changed further state.
   By default it writes to the **outbox** via `IOutboxWriter.Enqueue(...)` followed by its own
   `IUnitOfWork.SaveChangesAsync(...)` — the same ordinary `SaveChanges` call persists the queued
   row (§5 covers when `IEventBus.PublishAsync(...)`, a direct non-durable publish, is the
   deliberate exception instead):
   ```csharp
   internal sealed class TodoItemCreatedDomainEventHandler(ISender sender, IOutboxWriter outboxWriter, IUnitOfWork unitOfWork)
       : IDomainEventHandler<TodoItemCreatedDomainEvent>
   {
       public async Task Handle(TodoItemCreatedDomainEvent notification, CancellationToken cancellationToken)
       {
           Result<TodoItemResponse> result = await sender.Send(new GetTodoItemQuery(notification.TodoItemId), cancellationToken);

           if (result.IsFailure)
           {
               throw new AppException(nameof(GetTodoItemQuery), result.Error);
           }

           outboxWriter.Enqueue(
               new TodoItemCreatedIntegrationEvent(notification.Id, notification.OccurredOnUtc, result.Value.Id, result.Value.Title));

           await unitOfWork.SaveChangesAsync(cancellationToken);
       }
   }
   ```
   A `Result.Failure` at this point is escalated with `throw new AppException(...)` — a domain
   notification handler has no `Result` return channel to propagate failure through, so this is
   the one sanctioned place outside `ExceptionHandlingPipelineBehavior` that throws deliberately.
3. The **integration event contract** (`sealed class TodoItemCreatedIntegrationEvent :
   IntegrationEvent`, plain data, `{ get; init; }` properties) is defined in the *publishing*
   module's own `<Module>.IntegrationEvents` project — never in `Application`/`Domain`. This
   project depends on `Common.Application` only.
4. The **consuming module**'s `Presentation` project takes a `ProjectReference` to the
   publisher's `IntegrationEvents` project (and *only* that project — never the publisher's
   Domain/Application/Infrastructure/Presentation) and implements the real business reaction,
   deriving from the shared `IntegrationEventHandler<T>` base (bridges the untyped
   `IIntegrationEventHandler.Handle(IIntegrationEvent, ct)` the inbox job calls through to the
   strongly-typed override below):
   ```csharp
   public sealed class TodoItemCreatedIntegrationEventConsumer(ISender sender)
       : IntegrationEventHandler<TodoItemCreatedIntegrationEvent>
   {
       public override async Task Handle(TodoItemCreatedIntegrationEvent integrationEvent, CancellationToken cancellationToken = default)
       {
           Result result = await sender.Send(new MirrorTodoItemCommand(integrationEvent.TodoItemId, integrationEvent.Title), cancellationToken);

           if (result.IsFailure)
           {
               throw new AppException(nameof(MirrorTodoItemCommand), result.Error);
           }
       }
   }
   ```
5. **No manual registration for this class** — `<Module>Module.AddXModule(...)` already calls
   `services.AddIntegrationEventHandlers(Presentation.AssemblyReference.Assembly)` (§12), which
   finds every `IIntegrationEventHandler<T>` in that assembly by reflection. What *does* need
   wiring is the bus subscription that gets the raw message durably into this module's own inbox
   in the first place — that's `<Module>Module.Initialize(IServiceProvider serviceProvider)`,
   called once from `Program.cs` **after** `builder.Build()` (§14), which reuses that same
   reflection scan to discover which event types this module's handlers care about, and for each
   one subscribes a **generic, business-logic-free** handler onto the bus:
   ```csharp
   public static void Initialize(IServiceProvider serviceProvider)
   {
       IEventBus eventBus = serviceProvider.GetRequiredService<IEventBus>();

       foreach (Type integrationEventType in /* every T from this module's IIntegrationEventHandler<T> */)
       {
           Type genericHandlerType = typeof(IntegrationEventGenericHandler<>).MakeGenericType(integrationEventType);
           var genericHandler = (IIntegrationEventHandler)Activator.CreateInstance(genericHandlerType, serviceProvider)!;

           eventBus.Subscribe(integrationEventType, genericHandler);
       }
   }
   ```
   `IntegrationEventGenericHandler<T>` (`Infrastructure/Inbox/`, one per consuming module) does
   nothing but INSERT the raw message into that module's own `inbox_messages` table — the same
   durability-first, no-business-logic role a message broker's own receive endpoint would play.
   The reflection happens here (in `Infrastructure`) rather than citing `TodoItemCreatedIntegrationEvent`
   by name, specifically so `Infrastructure` never needs a `ProjectReference` to another module's
   `IntegrationEvents` project — only `Presentation` is allowed that (§2).
6. The **publishing** module calls `services.AddOutboxProcessor(Schemas.Tasks)` from its own
   composition root (§12) — this is what actually wires up the `OutboxProcessorJob` that drains
   the table step 2 just wrote to. Forgetting this step is a real, easy-to-miss failure mode: the
   module builds, the write succeeds, rows accumulate in that module's own `outbox_messages`
   table, and nothing ever publishes them, because no job was ever registered to poll it. The
   **consuming** module symmetrically needs `services.AddInboxProcessor(Schemas.Tasks, Presentation.AssemblyReference.Assembly)`
   for `ProcessInboxJob` to drain what `IntegrationEventGenericHandler<T>` wrote in step 5.

A "mirror" entity created this way (case 1 in §8's domain-event exceptions) typically has a
`Create(Guid id, ...)` factory that takes the **foreign** id directly as its own `Id` (not a new
`Guid.NewGuid()`) and raises no domain event of its own.

## 11. `PublicApi` projects — reserved, verify before relying on them

Each module *may* have a `<Module>.PublicApi` project: a contract-only interface
(`ITodoApi`/etc.) plus its own self-contained response DTOs, intended for **synchronous**
cross-module calls as an alternative to the async integration-event flow in §10. Its defining
trait is that it depends on **nothing** — not `Common.Domain`, not even itself transitively
importing another project — by design, so any module could reference it without pulling in
anything else.

**By default, treat a freshly scaffolded `PublicApi` project as unused** unless proven otherwise
in this specific repo: not added to the `.sln`, no concrete implementation registered in any
composition root, not referenced by any other module's `.csproj`. Different repos following this
template land in different states here — some never wire it up at all, others make it a real,
actively-referenced synchronous-read mechanism. **Always verify the actual current state** (check
the `.sln` file and grep for the interface name and for `ProjectReference`s to the `PublicApi`
project) before writing code that assumes either way. If a task genuinely needs synchronous
cross-module reads and no integration-event mirror already covers it, implementing and wiring a
`PublicApi` for real is a deliberate architectural decision — flag it to the user rather than
silently making it "real" as a side effect of an unrelated change, and just as importantly: if
this repo has already made that decision and wired `PublicApi` up for real, don't silently treat
it as reserved/unimplemented either — match what's actually there.

## 12. Module composition root

`Infrastructure/<Module>Module.cs` is the module's own DI wiring, called once from `Program.cs`:
```csharp
public static class TasksModule
{
    public static IServiceCollection AddTasksModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddEndpoints(Presentation.AssemblyReference.Assembly);

        // Only present on a module that CONSUMES another module's integration events — registers
        // every IIntegrationEventHandler<T> found in Presentation (§10).
        services.AddIntegrationEventHandlers(Presentation.AssemblyReference.Assembly);

        services.AddInfrastructure(configuration);
        return services;
    }

    // Only present on a module that CONSUMES another module's integration events — subscribes a
    // generic inbox-writer handler per event type onto the bus (§10). Called from Program.cs
    // *after* builder.Build(), once IEventBus exists in the container.
    public static void Initialize(IServiceProvider serviceProvider)
    {
        IEventBus eventBus = serviceProvider.GetRequiredService<IEventBus>();

        foreach (Type integrationEventType in /* every T from this module's IIntegrationEventHandler<T> */)
        {
            Type genericHandlerType = typeof(IntegrationEventGenericHandler<>).MakeGenericType(integrationEventType);
            var genericHandler = (IIntegrationEventHandler)Activator.CreateInstance(genericHandlerType, serviceProvider)!;

            eventBus.Subscribe(integrationEventType, genericHandler);
        }
    }

    private static void AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<TasksDbContext>((sp, options) => options
            .UseNpgsql(configuration.GetConnectionString("Database"),
                npgsqlOptions => npgsqlOptions.MigrationsHistoryTable(HistoryRepository.DefaultTableName, Schemas.Tasks))
            .UseSnakeCaseNamingConvention()
            .AddInterceptors(sp.GetRequiredService<InsertOutboxMessagesInterceptor>()));

        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<TasksDbContext>());
        services.AddScoped<ITodoItemRepository, TodoItemRepository>();

        // Only present on a module that PUBLISHES integration events (§10) — wires up the
        // OutboxProcessorJob that drains this module's own outbox_messages table. A module that
        // only consumes never needs this.
        // services.AddOutboxWriter<TasksDbContext>();
        // services.AddOutboxProcessor(Schemas.Tasks);

        // Only present on a module that CONSUMES another module's integration events — wires up
        // the ProcessInboxJob that drains what Initialize's generic handler wrote (§10).
        // services.AddInboxProcessor(Schemas.Tasks, Presentation.AssemblyReference.Assembly);

        // A Redis-only aggregate (no EF, no repository — see §13) registers its own service instead:
        // services.AddSingleton<TodoDraftService>();
    }
}
```
Each module defines **its own** `IUnitOfWork` interface in
`Application/Abstractions/Data/IUnitOfWork.cs` (`Task<int> SaveChangesAsync(CancellationToken)`)
— this is **not** shared from `Common.Application`; every module's `DbContext` implements its own
module-local `IUnitOfWork` directly (`public sealed class TasksDbContext(...) : DbContext(...),
IUnitOfWork`), and the composition root registers it via `sp.GetRequiredService<TasksDbContext>()`.

`Infrastructure/Database/Schemas.cs` — one `internal const string` per schema this module owns
(almost always exactly one, matching the module name lowercased).

`Presentation/AssemblyReference.cs` and `Application/AssemblyReference.cs` — identical shape,
own namespace each:
```csharp
public static class AssemblyReference
{
    public static readonly Assembly Assembly = typeof(AssemblyReference).Assembly;
}
```
(`Domain` does **not** get one — nothing scans it by assembly reflection.)

`Presentation/Tags.cs` — one `internal const string` per aggregate/area, used by every endpoint's
`.WithTags(...)` call for Swagger grouping.

## 13. The one non-EF aggregate pattern: Redis-backed state

Not every piece of module state lives in Postgres. A short-lived, per-user, non-authoritative
piece of state (this template's own example: a shopping-cart-style draft) can instead be a plain
class with no `Entity` base, no repository, no domain events, stored directly through
`ICacheService`:
```csharp
public sealed class TodoDraft
{
    public Guid OwnerId { get; init; }
    public List<TodoDraftItem> Items { get; init; } = [];

    internal static TodoDraft CreateDefault(Guid ownerId) => new() { OwnerId = ownerId };
}

public sealed class TodoDraftService(ICacheService cacheService)
{
    private static readonly TimeSpan DefaultExpiration = TimeSpan.FromMinutes(20);

    public async Task<TodoDraft> GetAsync(Guid ownerId, CancellationToken cancellationToken = default)
    {
        return await cacheService.GetAsync<TodoDraft>(CacheKey(ownerId), cancellationToken)
            ?? TodoDraft.CreateDefault(ownerId);
    }

    public async Task AddItemAsync(Guid ownerId, TodoDraftItem item, CancellationToken cancellationToken = default)
    {
        TodoDraft draft = await GetAsync(ownerId, cancellationToken);
        draft.Items.Add(item);
        await cacheService.SetAsync(CacheKey(ownerId), draft, DefaultExpiration, cancellationToken);
    }

    private static string CacheKey(Guid ownerId) => $"todo-drafts:{ownerId}";
}
```
Registered as a **singleton** in the module's composition root (it holds no per-request state
itself — `ICacheService` is the actual store). A command handler that needs it injects the
service directly (`TodoDraftService`, not an interface — this template doesn't abstract it
further since there's exactly one implementation and it's module-internal). Cache key convention:
`"<kebab-plural-noun>:<ownerId>"`. Reach for this pattern only for state that's genuinely
disposable/reconstructable — anything that must survive a cache eviction or be queried
relationally belongs in Postgres via the normal entity/repository path instead.

## 14. API host composition (`Program.cs`)

Fixed order, don't reshuffle without a reason:
1. `WebApplication.CreateBuilder(args)`.
2. `UseSerilog(...)` reading from configuration.
3. `AddExceptionHandler<GlobalExceptionHandler>()` + `AddProblemDetails()`.
4. `AddEndpointsApiExplorer()` + `AddSwaggerGen(...)` (custom `CustomSchemaIds` to flatten
   nested `Request` class names — `t.FullName?.Replace("+", ".")`).
5. `AddApplication([Module1.Application.AssemblyReference.Assembly, Module2..., ...])` — every
   module's `Application` assembly, one array, one call.
6. Read `Database`/`Cache` connection strings from configuration.
7. `AddInfrastructure(configuration, databaseConnectionString, redisConnectionString)` — no
   per-module arrays here; each module wires its own outbox/inbox job and consumer registration
   from its own composition root (§5/§12).
8. `configuration.AddModuleConfiguration(["module1", "module2", ...])` — loads
   `modules.<name>.json` (required) + `modules.<name>.Development.json` (optional) per module,
   layered into the same `IConfiguration`.
9. `AddHealthChecks().AddNpgSql(...).AddRedis(...)`.
10. `Add<Module>Module(configuration)` — once per module, in any order (they don't depend on
    each other at this point).
11. `builder.Build()`.
12. `<Module>Module.Initialize(app.Services)` — once per module that **consumes** another
    module's integration events (§10/§12); a module that only publishes contributes nothing here.
    Must come after `builder.Build()`, since it resolves `IEventBus` from the now-built container.
13. `if (Environment.IsDevelopment())`: `UseSwagger()` + `UseSwaggerUI()` + `app.ApplyMigrations()`
    (auto-applies every module's pending EF migrations — **Development only**, never in
    Production; a real deployment applies migrations out-of-band).
14. `app.MapEndpoints()` — maps every module's endpoints (§6).
15. `MapHealthChecks("health", ...)`.
16. `UseSerilogRequestLogging()`.
17. `UseExceptionHandler()`.
18. `app.Run()`.

`GlobalExceptionHandler : IExceptionHandler` is the absolute last resort — anything that escapes
both a handler's own `Result.Failure` path *and* `ExceptionHandlingPipelineBehavior` lands here,
logs, and returns a bare 500 `ProblemDetails` with no details leaked to the client.

## 15. Common pitfalls in this style of monolith — watch for these, don't copy them by accident

These are failure modes that show up often enough in codebases following this template that
they're worth naming explicitly, so they're recognized as bugs/gaps rather than mistaken for
intentional conventions to replicate:

- **Test projects can silently be empty scaffolds.** A repo following this template can end up
  with `tests/<X>.UnitTests` / `.ArchitectureTests` / `.IntegrationTests` folders that contain
  zero source files and aren't even referenced by the `.sln` — i.e., no working test suite,
  despite the folders existing. Always check the actual state (`git status`, is the project in
  the `.sln`, does it have any `.cs` files) before assuming tests exist to extend; see
  `.claude/skills/add-tests/SKILL.md`.
- **`PublicApi` projects can sit unimplemented indefinitely** (§11) — don't assume synchronous
  cross-module calls work anywhere until you've verified it for this specific repo, and don't
  assume the opposite either once a repo has genuinely wired one up for real.
- **A `.AllowAnonymous()` call can be vestigial.** A registration-style endpoint may carry
  `.AllowAnonymous()` while the host has **no authentication middleware configured at all** (no
  `AddAuthentication`/`AddAuthorization`/JWT anywhere). In that state the call is a no-op left
  over from a plan that was never finished — it doesn't mean authentication exists elsewhere in
  the app. Don't infer an auth scheme from its presence; grep for
  `AddAuthentication`/`[Authorize]` to check the real state before assuming any endpoint is
  actually protected.
- **A registration/create command can accept a field it never uses.** A command may take a
  parameter that's validated (e.g. `MinimumLength(...)` on a password) but never actually stored,
  hashed, or referenced again anywhere in the handler or entity — an incomplete-slice shortcut
  left behind during early scaffolding, not a security pattern to imitate. If it's a
  security-sensitive field like a password, a real implementation needs an actual hasher
  abstraction wired into the handler and a hash persisted on the entity. Flag a gap like this to
  the user rather than silently perpetuating it in new code.
- **A domain event class can be defined and never raised or handled** (§8/§9) — an artifact of
  an entity's behavior method never actually calling `Raise(...)` for it. Don't treat "the event
  class exists in the codebase" as proof the event ever actually fires; verify the entity method
  that's supposed to raise it.
- **The "command with no return value" endpoint response can end up genuinely inconsistent
  across a codebase** (§6) — some return `Ok()`, others `NoContent()`, with no single rule
  actually enforced anywhere. When you find both shapes already present, don't invent a
  "correct" one to standardize on unilaterally — match the sibling endpoints you're extending.

## 16. Conventions to preserve when extending

- New use cases go in `Application/<PluralAggregate>/<VerbNoun>/` following the
  Command/Handler/Validator triad (§7) — don't introduce a different mediator or a
  service-layer/manager-class alternative to MediatR handlers (§17).
- New entities get their own `<Aggregate>Errors` static class in `Domain` (§9) rather than
  throwing raw exceptions or reusing another aggregate's errors.
- Query handlers read via Dapper; command handlers write via EF + repository + `IUnitOfWork`.
  Don't blur this CQRS read/write split by having a query handler pull in the `DbContext`, or a
  command handler run raw SQL.
- Don't add API versioning, blanket `[Authorize]`/auth policies, a query-result cache-aside layer
  beyond the one narrow Redis-backed aggregate case (§13), manual DI registration for
  handlers/endpoints/validators, or `Update`/`SaveChanges` calls inside a repository — none of
  these exist in this template's baseline; introducing one is a scope decision to confirm with
  the user first, not a silent addition.
- Never hand-write or hand-edit an EF migration file — always `dotnet ef migrations add` (§9).
- Use `.claude/skills/` (`add-entity`, `add-feature`, `add-messaging`, `add-tests`, `ca-review`) —
  they encode every rule above as directly runnable scaffolding with a consistent fictional
  `Tasks`/`TodoItem` worked example. Prefer them over freehand implementations so new code
  matches the existing slices exactly, and run `ca-review` before considering a change done.

## 17. No generic "Services" — Policies and Command re-send instead

Two upstream reference architectures this template draws from — Milan Jovanovic's modular-monolith
style and Kamil Grzybek's `modular-monolith-with-ddd` — never have an `IXService`/`XService` class
holding domain or application orchestration logic anywhere in their Application or Domain layers.
This template follows the same shape: a "Service" class is a smell, not a layer.

**What counts as a forbidden "Service"**: a DI-registered class (`services.AddScoped<XService>()`)
whose constructor takes several repositories/collaborators and exposes a method a Command Handler
calls to do the actual work. What it holds determines where it actually belongs:
- a decision belonging to a single aggregate → a method on the entity (§8), not a service;
- a pure decision spanning multiple aggregates → a **Policy** (below);
- multi-step orchestration (repository calls + domain calls) that more than one Command Handler
  needs → a sign the two Handlers should collapse into **one Command, re-sent** (below), not a
  shared class.

**Exempt — plain technical infrastructure**: cache wrappers (`ICacheService`, §4),
auth/permission lookups, background jobs (`IHostedService`/Quartz-style implementations), external
API clients (payment gateways, LLM/email providers). These carry no business decision — they're the
same kind of component as `Common.Application`'s own `ICacheService`. Don't rename or restructure
these; they were never the anti-pattern.

**Cross-aggregate pure decision → a `Policy`, not a Service.**
`Domain/<Aggregate>/Policies/<Name>Policy.cs` — `public static class`, no interface, never
registered in DI, no I/O. Takes data the caller already loaded, returns a decision:
```csharp
namespace <ProjectName>.Modules.Tasks.Domain.TodoLists.Policies;

public static class TodoListCompletionPolicy
{
    public static bool IsFullyComplete(IReadOnlyCollection<bool> itemCompletionStates)
        => itemCompletionStates.Count > 0 && itemCompletionStates.All(isComplete => isComplete);
}
```
Called directly by the Command Handler that already has the data
(`TodoListCompletionPolicy.IsFullyComplete(states)`) — never injected, never wrapped in an
interface "for testability" (it's a pure static function; call it directly in a unit test).

**Same effect needed from two entry points → resend the same Command.** An operation triggered
both by a user-facing endpoint and by a batch/event trigger (a domain-event handler, or an
integration-event consumer reacting to something that affects N aggregates) should be **one
`Command` + one `CommandHandler`** — the batch/event side re-sends that same `Command` via
`ISender.Send(...)`, once per affected item, instead of sharing an orchestration class/method
between two Handlers:
```csharp
// Batch trigger: reacts to a TodoListArchivedIntegrationEvent, re-sends the same Command the
// user-facing endpoint uses, one per pending item — never a shared helper.
internal sealed class CompletePendingItemsForArchivedListCommandHandler(
    ITodoItemRepository todoItemRepository, ISender sender)
    : ICommandHandler<CompletePendingItemsForArchivedListCommand>
{
    public async Task<Result> Handle(CompletePendingItemsForArchivedListCommand request, CancellationToken cancellationToken)
    {
        IReadOnlyCollection<TodoItem> pending = await todoItemRepository.GetPendingForListAsync(request.TodoListId, cancellationToken);

        foreach (TodoItem item in pending)
        {
            await sender.Send(new CompleteTodoItemCommand(item.Id, item.OwnerId), cancellationToken);
        }

        return Result.Success();
    }
}
```
If the two triggers need different data to authorize/scope the operation (the endpoint has an
authenticated user, the batch trigger doesn't), that data travels **explicitly on the Command** —
the single shared `CommandHandler` does an explicit ownership guard against it instead of relying
on a repository-embedded `GetAsync(id, userId)` filter that only the endpoint-triggered caller can
satisfy:
```csharp
public sealed record CompleteTodoItemCommand(Guid TodoItemId, Guid OwnerId) : ICommand;

internal sealed class CompleteTodoItemCommandHandler(ITodoItemRepository todoItemRepository, IUnitOfWork unitOfWork)
    : ICommandHandler<CompleteTodoItemCommand>
{
    public async Task<Result> Handle(CompleteTodoItemCommand request, CancellationToken cancellationToken)
    {
        TodoItem? item = await todoItemRepository.GetByIdAsync(request.TodoItemId, cancellationToken);
        if (item is null || item.OwnerId != request.OwnerId)
        {
            return Result.Failure(TodoItemErrors.NotFound(request.TodoItemId));
        }

        Result result = item.Complete(DateTime.UtcNow);
        if (result.IsFailure)
        {
            return result;
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
```
The endpoint resolves `IUserContext.UserId` and passes it in explicitly; it doesn't live inside the
Handler as an ambient dependency the batch trigger can't satisfy.

**Cross-module read orchestration → inline in the one Handler that needs it.** "Call another
module's `PublicApi` (§11) several times, aggregate the responses, feed a pure calculator" doesn't
get its own `IXDataService` wrapper — when only one Handler consumes it, the fetch steps are
private helper methods on that Handler itself:
```csharp
internal sealed class GetTodoItemInsightCommandHandler(
    IProjectsApi projectsApi, ITodoItemRepository todoItemRepository, IUnitOfWork unitOfWork)
    : ICommandHandler<GetTodoItemInsightCommand, Guid>
{
    public async Task<Result<Guid>> Handle(GetTodoItemInsightCommand request, CancellationToken cancellationToken)
    {
        InsightCalculation? calculation = await ComputeInsightAsync(request.TodoItemId, cancellationToken);
        // ... persist, same shape as any other command handler
    }

    private async Task<InsightCalculation?> ComputeInsightAsync(Guid todoItemId, CancellationToken cancellationToken)
    {
        // fetch from projectsApi, feed InsightCalculator.Calculate(...) — private to this handler
    }
}
```
Don't introduce the wrapper "for reuse" before a second consumer actually exists — extracting one
once a second Handler genuinely needs the same fetch is a normal refactor at that point, not a
violation to pre-empt now.

**Don't relabel, eliminate.** Turning `internal sealed class XService(deps...) { ... }` +
`services.AddScoped<XService>()` into `internal static class X { public static Task<Result>
Y(deps as parameters, ...) }` removes the DI registration and the `Service` suffix but **not** the
underlying shape — it's the same "orchestration shared by two Handlers" responsibility with
different packaging. If a piece of code still exists purely to be called by more than one Handler
after applying the rules above, that's the thing to actually resolve (Command re-send is almost
always possible — see above); don't stop at a cosmetic rename.
