---
name: add-entity
description: Scaffold a new domain entity/aggregate (entity class, domain events, errors, repository interface + EF implementation, DbContext wiring, migration) inside an existing module of this modular monolith, following this template's DDD conventions exactly.
---

# add-entity

Scaffold a new aggregate/entity inside an **existing** module of this repo (or one just created
via `/add-module`). Read this repo's root `CLAUDE.md` first if one exists, for the entity
conventions this skill applies — it takes precedence over this skill wherever the two differ.
Ask the user for: the **module** it belongs to, the **entity name** (PascalCase singular, e.g.
`Venue`), its **properties**, and whether it references another aggregate (by id).

Before scaffolding, find and read 1-2 existing entities in this repo as concrete references —
ideally one simple one with no relationships and one richer one with a relationship and/or a
`Result`-based factory/behavior — and match their exact style (naming, visibility, constructor
shape). Below, the worked example scaffolds a `TodoItem` entity (title, description, completion
state) in a fictional `Tasks` module — a stand-in that won't collide with any real entity in this
repo, purely to show the pattern end to end. Swap `TodoItem`/`Tasks` for whatever the user
actually asked for; `<ProjectName>` stands for this repo's actual root namespace/solution name.

## 1. Domain project — `Domain/TodoItems/TodoItem.cs`

```csharp
using <ProjectName>.Common.Domain;

namespace <ProjectName>.Modules.Tasks.Domain.TodoItems;

public sealed class TodoItem : Entity
{
    private TodoItem() { }

    public Guid Id { get; private set; }
    public string Title { get; private set; }
    public string Description { get; private set; }
    public bool IsCompleted { get; private set; }
    public DateTime? CompletedAtUtc { get; private set; }
    // public Guid ProjectId { get; private set; }   // store the foreign aggregate's id, never a navigation property

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

    // For a no-op change, return early WITHOUT raising an event:
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

Rules, no exceptions:
- `sealed class : Entity`; every settable property is `{ get; private set; }`; a private
  constructor (parameterless, using an object initializer in `Create` as above, **or** taking the
  required fields directly and assigning them in the constructor body — check which style
  sibling entities in this module already use and match it, don't mix the two within one module).
- A `public static Create(...)` factory. Returns the entity directly when creation can't fail, or
  `Result<TEntity>` when a cross-field invariant can reject it (e.g. `EndDate < StartDate`) — in
  that case return `Result.Failure<TodoItem>(TodoItemErrors.SomeInvariant)` before constructing
  the entity.
- IDs are generated **inside** the factory with `Guid.NewGuid()` — never left for the database or
  passed in by the caller, *except* for a mirror entity that must reuse a foreign aggregate's id
  (see the "skip the created event" cases below).
- Raise a `TodoItemCreatedDomainEvent` from `Create(...)` when creation is itself a business event
  other handlers should react to — this is the common case, default to it. Skip it only when one
  of these two specific shapes applies (don't invent a third reason):
  1. **Mirror entity** — this entity exists purely to project/replicate another module's
     aggregate, created from a domain-event handler reacting to an integration event (its `Create`
     takes the **foreign** id as its own `Id`, not `Guid.NewGuid()`). It has no independent
     "created" meaning of its own.
  2. **Aggregate-scoped child entity** created as a line-item detail of a parent aggregate root's
     own operation (e.g. a `TodoChecklistItem` created via `TodoItem.AddChecklistItem(...)`) —
     but treat this exception with real suspicion: default to raising the event anyway unless you
     have a concrete reason no subscriber would ever care, since skipping it silently is an easy
     way to end up with a dead event class that nothing raises — a common pitfall in this style of
     codebase, see this repo's `CLAUDE.md` §8/§15. Don't introduce it without a reason.
- Behavior methods raise their own domain event only when state actually changed — see
  `ChangeTitle`'s early return above for the no-op case.

## 2. Domain events — one file per event, same folder/namespace

`Domain/TodoItems/TodoItemCreatedDomainEvent.cs`:
```csharp
using <ProjectName>.Common.Domain;

namespace <ProjectName>.Modules.Tasks.Domain.TodoItems;

public sealed class TodoItemCreatedDomainEvent(Guid todoItemId) : DomainEvent
{
    public Guid TodoItemId { get; init; } = todoItemId;
}
```
Name: `{Entity}{PastTenseVerb}DomainEvent`, always a `sealed class` with a primary constructor
(never a `record`, matching every domain event in this template). Add one per behavior method
that raises an event (here: `TodoItemCompletedDomainEvent(Guid TodoItemId)`,
`TodoItemTitleChangedDomainEvent(Guid TodoItemId, string Title)`). A domain event doesn't need a
handler to exist yet — it's fine (and common in this template) for an event to be raised with
zero current subscribers.

## 3. Domain errors — `Domain/TodoItems/TodoItemErrors.cs`

```csharp
using <ProjectName>.Common.Domain;

namespace <ProjectName>.Modules.Tasks.Domain.TodoItems;

public static class TodoItemErrors
{
    public static Error NotFound(Guid todoItemId) =>
        Error.NotFound("TodoItems.NotFound", $"The todo item with the identifier {todoItemId} was not found");

    public static readonly Error AlreadyCompleted = Error.Problem(
        "TodoItems.AlreadyCompleted",
        "The todo item is already completed");
}
```
Always include the `NotFound(Guid)` method — it's the standard "get by id or fail" error every
command handler returns after a `null` repository lookup. Code format is
`"<PluralAggregate>.<PascalCaseReason>"`. Pick the factory by what actually went wrong:
`Error.Problem` for a business-rule violation (the common case), `Error.Conflict` for a
uniqueness/concurrency clash, `Error.NotFound` only for the id-lookup case, `Error.Failure` only
for a truly generic/unexpected failure. Never reuse another aggregate's `*Errors` class, even for
an identical-sounding rule — each aggregate owns its own error vocabulary.

## 4. Repository interface — `Domain/TodoItems/ITodoItemRepository.cs`

```csharp
namespace <ProjectName>.Modules.Tasks.Domain.TodoItems;

public interface ITodoItemRepository
{
    Task<TodoItem?> GetAsync(Guid id, CancellationToken cancellationToken = default);
    void Insert(TodoItem todoItem);
}
```
Add more `GetXxxAsync` overloads only if a command handler genuinely needs a different lookup
(e.g. by a natural/external key). **Never add `Update` or `Save` methods** — EF change tracking
plus the handler's own `IUnitOfWork.SaveChangesAsync()` call handles persistence; a repository in
this template is purely a query-by-id-and-insert gateway, nothing else.

## 5. Repository implementation — `Infrastructure/TodoItems/TodoItemRepository.cs`

```csharp
using <ProjectName>.Modules.Tasks.Domain.TodoItems;
using <ProjectName>.Modules.Tasks.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;

namespace <ProjectName>.Modules.Tasks.Infrastructure.TodoItems;

internal sealed class TodoItemRepository(TasksDbContext context) : ITodoItemRepository
{
    public async Task<TodoItem?> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await context.TodoItems.SingleOrDefaultAsync(t => t.Id == id, cancellationToken);
    }

    public void Insert(TodoItem todoItem)
    {
        context.TodoItems.Add(todoItem);
    }
}
```
`internal sealed class`, primary-constructor DI on the module's `DbContext`, `SingleOrDefaultAsync`
(not `FirstOrDefaultAsync`) for a lookup-by-id.

## 6. EF configuration — when the entity has a relationship or a column-level constraint

Add `Infrastructure/TodoItems/TodoItemConfiguration.cs` when the entity references another
aggregate **or** has a column-level constraint worth enforcing at the DB level (max length on a
string, a unique index, etc.) — most non-trivial entities end up with one for at least the latter
reason, even with zero relationships:
```csharp
using <ProjectName>.Modules.Tasks.Domain.TodoItems;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace <ProjectName>.Modules.Tasks.Infrastructure.TodoItems;

internal sealed class TodoItemConfiguration : IEntityTypeConfiguration<TodoItem>
{
    public void Configure(EntityTypeBuilder<TodoItem> builder)
    {
        builder.Property(t => t.Title).HasMaxLength(200);
        builder.Property(t => t.Description).HasMaxLength(2000);

        // Only if the entity references another aggregate, by its stored Guid id:
        // builder.HasOne<Project>().WithMany().HasForeignKey(t => t.ProjectId);
    }
}
```
Skip the configuration class entirely only when the entity has **neither** a relationship **nor**
a constraint worth enforcing (a genuinely plain scalar entity) — check an existing entity in this
module first to see which case is more common there before deciding.

## 7. Wire into the module's DbContext

In `Infrastructure/Database/TasksDbContext.cs`:
```csharp
internal DbSet<TodoItem> TodoItems { get; set; }
```
and, only if step 6 produced a configuration class, inside `OnModelCreating`:
```csharp
modelBuilder.ApplyConfiguration(new TodoItemConfiguration());
```

## 8. Register the repository in the module's composition root

In `Infrastructure/TasksModule.cs`, inside `AddInfrastructure`:
```csharp
services.AddScoped<ITodoItemRepository, TodoItemRepository>();
```

## 9. Generate the migration

```
dotnet ef migrations add Add_TodoItems --project src/Modules/Tasks/<ProjectName>.Modules.Tasks.Infrastructure --startup-project src/API/<ProjectName>.Api --context TasksDbContext -o Database/Migrations
```
Adjust the `--project`/`--startup-project` paths and the module/entity names to match the real
request if this repo's actual folder layout differs. Never hand-write migration files — always
generate them, then commit the generated `.cs`/`.Designer.cs`/updated `ModelSnapshot.cs` as-is.
If this is the module's very first entity, the migration name convention is `Create_Database`
instead of `Add_<Entities>` (matching `/add-module`'s step 9).

## 10. Next step

The entity now exists but has no use cases — tell the user to run `/add-feature` to add
Create/Get/Update/etc. commands and queries for it, and `/add-tests` afterward to cover the new
entity's behavior methods and its handlers.
