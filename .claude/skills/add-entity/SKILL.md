---
name: add-entity
description: Scaffold a new Domain entity (aggregate root) in a .NET Clean Architecture / Modular Monolith solution — entity class with private setters and a static factory method, an Errors static class, domain events, a repository interface, optional EF Core configuration, and DI/DbContext wiring. Use when the user asks to add/create a new entity, aggregate, or domain model in a project that follows Clean Architecture with CQRS, MediatR, Result pattern, and EF Core.
argument-hint: <entity description, e.g. "TodoList with a name, an owner, and a due date">
---

# Add Entity

Scaffolds a new Domain-layer entity following strict Clean Architecture encapsulation rules: private
setters, a private parameterless constructor, a static factory method, behavior methods that raise
domain events, and zero public mutation surface. This skill assumes the target repository is a
**Modular Monolith** organized into per-module `Domain` / `Application` / `Infrastructure` /
`Presentation` projects, using the **Result pattern** (no exceptions for expected failures),
**MediatR** for CQRS, **FluentValidation**, **Dapper** for reads and **EF Core** for writes.

If the project does not already show these patterns, stop and confirm with the user before applying
this template — it is not a generic entity generator, it reproduces one specific architecture exactly.

## Step 0 — Detect the project's real conventions

Never hard-code a namespace. Before writing anything, inspect the target repository and resolve:

1. **Root namespace** — find the shared kernel project (commonly named `*.Common.Domain` or
   `*.SharedKernel`) that defines the base `Entity`, `Result`/`Result<T>`, `Error`, `ErrorType`,
   `DomainEvent`/`IDomainEvent` types. Its namespace prefix (everything before `Common.Domain`) is the
   root namespace. Below it is written as `<RootNamespace>`.
2. **Module name** — the bounded context this entity belongs to (e.g. `Orders`, `Catalog`, `Todos`).
   Confirm by looking at `src/Modules/<Module>/` (or equivalent) for existing
   `<RootNamespace>.Modules.<Module>.Domain` / `.Application` / `.Infrastructure` / `.Presentation`
   projects. If the module doesn't exist yet, ask the user before creating a brand-new module — that's
   a bigger structural decision than adding an entity to an existing one.
3. **Base `Entity` class shape** — read it. It typically exposes a protected/no-op constructor, an
   internal `List<IDomainEvent>` backing field, a public `DomainEvents` read-only view, and a
   `protected void Raise(IDomainEvent)`. Confirm the exact member names before writing `Raise(...)` calls.
4. **`Result` / `Error` shape** — read `Result`, `Result<T>`, `Error`, `ErrorType`. Confirm the static
   factory names on `Error` (commonly `Error.Failure`, `Error.NotFound`, `Error.Problem`,
   `Error.Conflict`) and match them exactly — do not invent new ones.

Everywhere below, `<RootNamespace>` and `<Module>` are placeholders — substitute the real values you
just detected. The worked example uses a generic `TodoItem` entity in a `Todos` module purely to show
the shape; adapt the entity name, properties, and business rules to what the user actually asked for.

## Step 1 — The entity class

File: `src/Modules/<Module>/<RootNamespace>.Modules.<Module>.Domain/<Entities>/<Entity>.cs`
(`<Entities>` is the plural folder, e.g. `TodoItems`)

Rules, in order of importance:

- `public sealed class <Entity> : Entity` — always `sealed`, always inherits the shared `Entity` base.
- A **private parameterless constructor** with an empty body. This exists purely for EF Core materialization and to block external `new`.
- Every property is `{ get; private set; }`. Never `{ get; set; }`, never a public setter — all
  mutation happens through named behavior methods.
- `Id` is a `Guid`, assigned once in the factory method via `Guid.NewGuid()`.
- Creation happens through one or more **static factory methods** named `Create(...)` (or a more
  specific verb if there are multiple creation paths). The factory:
  - Validates whatever can be validated with only the constructor arguments (cross-field invariants
    that don't need a database lookup).
  - Returns the bare entity type (`<Entity>`) if creation cannot fail, or `Result<<Entity>>` if it can.
  - Builds the instance with an object initializer against the private setters (this works because the
    initializer runs inside the same class), sets `Id = Guid.NewGuid()`, then calls
    `<instance>.Raise(new <Entity>CreatedDomainEvent(<instance>.Id))` before returning.
- Every other state transition is a **named instance method** (not a generic `Update`/`Set`), e.g.
  `Complete()`, `Rename(string title)`, `Archive()`, `Cancel(DateTime utcNow)`. Each method:
  - Returns `void` if it cannot fail, or `Result`/`Result<T>` if it can.
  - Guards invalid transitions by returning `Result.Failure(<Entity>Errors.SomeError)` — never throws
    for expected business-rule violations.
  - Is a no-op (early `return`) when called with a value that wouldn't actually change state (see the
    `Rename` example — this avoids raising a domain event for a no-op change).
  - Raises exactly one domain event per meaningful transition via `Raise(new <Entity><Verb>DomainEvent(...))`.
- If the entity is an aggregate root owning child entities (e.g. an order with line items), the
  children are held in `private readonly List<Child> _children = [];` with a public
  `IReadOnlyCollection<Child> Children => _children.ToList();` projection, and added through a method
  on the parent (`AddItem(...)`) that calls an `internal static Child.Create(...)` factory on the
  child — never `_children.Add(new Child(...))` directly with public setters on the child.

### Worked example — `TodoItem`

```csharp
using <RootNamespace>.Common.Domain;

namespace <RootNamespace>.Modules.Todos.Domain.TodoItems;

public sealed class TodoItem : Entity
{
    private TodoItem()
    {
    }

    public Guid Id { get; private set; }

    public string Title { get; private set; }

    public string? Description { get; private set; }

    public bool IsCompleted { get; private set; }

    public DateTime? DueDateUtc { get; private set; }

    public static Result<TodoItem> Create(string title, string? description, DateTime? dueDateUtc)
    {
        if (dueDateUtc.HasValue && dueDateUtc < DateTime.UtcNow)
        {
            return Result.Failure<TodoItem>(TodoItemErrors.DueDateInPast);
        }

        var todoItem = new TodoItem
        {
            Id = Guid.NewGuid(),
            Title = title,
            Description = description,
            DueDateUtc = dueDateUtc,
            IsCompleted = false
        };

        todoItem.Raise(new TodoItemCreatedDomainEvent(todoItem.Id));

        return todoItem;
    }

    public Result Complete()
    {
        if (IsCompleted)
        {
            return Result.Failure(TodoItemErrors.AlreadyCompleted);
        }

        IsCompleted = true;

        Raise(new TodoItemCompletedDomainEvent(Id));

        return Result.Success();
    }

    public void Rename(string title)
    {
        if (Title == title)
        {
            return;
        }

        Title = title;

        Raise(new TodoItemRenamedDomainEvent(Id, Title));
    }
}
```

Note the two creation shapes side by side: `Create` returns `Result<TodoItem>` here because it has a
rule that can fail (due date in the past). When a factory truly cannot fail, return the bare entity
type instead (see the Domain events step below for why this still matters for callers) — mirror
whichever shape the entity you're adding actually needs, don't default to `Result<T>` everywhere.

## Step 2 — The Errors static class

File: same folder, `<Entity>Errors.cs`.

```csharp
using <RootNamespace>.Common.Domain;

namespace <RootNamespace>.Modules.Todos.Domain.TodoItems;

public static class TodoItemErrors
{
    public static Error NotFound(Guid todoItemId) =>
        Error.NotFound("TodoItems.NotFound", $"The to-do item with the identifier {todoItemId} was not found");

    public static readonly Error AlreadyCompleted = Error.Problem(
        "TodoItems.AlreadyCompleted",
        "The to-do item was already completed");

    public static readonly Error DueDateInPast = Error.Problem(
        "TodoItems.DueDateInPast",
        "The to-do item due date is in the past");
}
```

Rules:
- `NotFound` is a **method** taking the id, because the message interpolates it. Every other error is a
  **`static readonly Error` field**, not a method, unless it also needs to interpolate a parameter.
- Error codes follow `<PluralEntity>.<PascalCaseReason>` (e.g. `TodoItems.AlreadyCompleted`) — no
  spaces, no module prefix, matches the folder/class name exactly.
- Pick the `Error` factory by HTTP-intent, not by feel: `Error.NotFound` for missing-resource,
  `Error.Conflict` for state conflicts the client could resolve by retrying differently,
  `Error.Problem` for business-rule violations (400), `Error.Failure` only for unclassified internal
  failures. Confirm these factory names against the `Error` type you read in Step 0 before using them.

## Step 3 — Domain events

One file per event, same folder, named `<Entity><PastTenseVerb>DomainEvent.cs`. Each is a `sealed
class` with a **primary constructor**, inheriting the shared `DomainEvent` base, re-exposing every
constructor parameter as an `init` property assigned from the parameter:

```csharp
using <RootNamespace>.Common.Domain;

namespace <RootNamespace>.Modules.Todos.Domain.TodoItems;

public sealed class TodoItemCreatedDomainEvent(Guid todoItemId) : DomainEvent
{
    public Guid TodoItemId { get; init; } = todoItemId;
}

public sealed class TodoItemCompletedDomainEvent(Guid todoItemId) : DomainEvent
{
    public Guid TodoItemId { get; init; } = todoItemId;
}

public sealed class TodoItemRenamedDomainEvent(Guid todoItemId, string title) : DomainEvent
{
    public Guid TodoItemId { get; init; } = todoItemId;

    public string Title { get; init; } = title;
}
```

One event per behavior method that actually changes state (not for the no-op early-return branches).
Name the event after what happened (past tense), not after the method that caused it, if they differ
(e.g. a method called `Cancel` raises a `...CanceledDomainEvent`).

## Step 4 — Repository interface

File: same folder, `I<Entity>Repository.cs`. Domain-layer interface, implemented in Infrastructure:

```csharp
namespace <RootNamespace>.Modules.Todos.Domain.TodoItems;

public interface ITodoItemRepository
{
    Task<TodoItem?> GetAsync(Guid id, CancellationToken cancellationToken = default);

    void Insert(TodoItem todoItem);
}
```

Rules:
- `GetAsync` is the only async member — it's the only one that actually touches the database
  synchronously with this call; `Insert` just marks the aggregate as tracked (EF Core `Add`), the
  actual write happens later when the unit of work saves.
- `Insert` is `void`, not `Task`. Don't add `async`/`Task` here.
- Only add more members (`GetByXAsync`, etc.) when a real query handler or command handler needs them —
  don't pre-build a generic CRUD surface. If a future feature needs hard deletion, add a narrow
  `Remove(<Entity> entity)` at that point; don't add it speculatively. Prefer a state-transition method
  (like `Archive()`/`Cancel()`) over physical deletion when the domain allows it, matching how sibling
  aggregates in this codebase soft-delete.

## Step 5 — EF Core configuration (only if needed)

Simple entities with only scalar properties need **no** `IEntityTypeConfiguration<T>` class — EF Core
maps them by convention straight from the `DbSet<T>`, private setters included. Only add a
`<Entity>Configuration.cs` in the Infrastructure project's matching folder when the entity has
something convention can't infer: a relationship, an owned/complex type, an explicit index, or a
column type override.

```csharp
using <RootNamespace>.Modules.Todos.Domain.TodoItems;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace <RootNamespace>.Modules.Todos.Infrastructure.TodoItems;

internal sealed class TodoItemConfiguration : IEntityTypeConfiguration<TodoItem>
{
    public void Configure(EntityTypeBuilder<TodoItem> builder)
    {
        // e.g. builder.HasOne<TodoList>().WithMany();
    }
}
```

If you do add one, register it in the module `DbContext`'s `OnModelCreating`:
`modelBuilder.ApplyConfiguration(new TodoItemConfiguration());`.

## Step 6 — Repository implementation, DbContext, and DI wiring

**Repository implementation** — `src/Modules/<Module>/<RootNamespace>.Modules.<Module>.Infrastructure/<Entities>/<Entity>Repository.cs`:

```csharp
using <RootNamespace>.Modules.Todos.Domain.TodoItems;
using <RootNamespace>.Modules.Todos.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;

namespace <RootNamespace>.Modules.Todos.Infrastructure.TodoItems;

internal sealed class TodoItemRepository(TodosDbContext context) : ITodoItemRepository
{
    public async Task<TodoItem?> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await context.TodoItems.SingleOrDefaultAsync(e => e.Id == id, cancellationToken);
    }

    public void Insert(TodoItem todoItem)
    {
        context.TodoItems.Add(todoItem);
    }
}
```

Note the primary-constructor DI (`(TodosDbContext context)` right on the class), and `internal sealed`
— repository implementations are never public, only the interface (in Domain) is.

**DbContext** — add a `DbSet<TodoItem>` to the module's `DbContext` (find it in
`.../Infrastructure/Database/<Module>DbContext.cs`):

```csharp
internal DbSet<TodoItem> TodoItems { get; set; }
```

And, only if you created a configuration class in Step 5, apply it inside `OnModelCreating`.

**Module DI registration** — find the module's `Add<Module>Module(...)` extension (commonly
`.../Infrastructure/<Module>Module.cs`) and add the repository registration next to its siblings:

```csharp
services.AddScoped<ITodoItemRepository, TodoItemRepository>();
```

Nothing else needs manual registration — MediatR handlers, FluentValidation validators, and minimal-API
endpoints are all discovered by assembly scanning elsewhere in the composition root, not per-entity.

## Checklist before finishing

- [ ] Entity is `sealed`, has a private parameterless ctor, only `private set`/`init` properties
- [ ] Every mutation is a named method or a static `Create`, never a public setter
- [ ] Every meaningful state transition raises exactly one domain event; no-op transitions raise none
- [ ] Factory/behavior methods return `Result`/`Result<T>` when they can fail, bare types otherwise
- [ ] `<Entity>Errors` uses `NotFound(id)` as a method, everything else as `static readonly Error`
- [ ] Repository interface lives in Domain; implementation is `internal sealed` in Infrastructure
- [ ] `DbSet<<Entity>>` added to the module `DbContext`; configuration class added only if needed
- [ ] Repository registered with `services.AddScoped<I<Entity>Repository, <Entity>Repository>()`
- [ ] No namespace, type, or error code references a module the entity doesn't belong to
