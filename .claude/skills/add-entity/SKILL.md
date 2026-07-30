---
name: add-entity
description: Scaffold a new domain entity/aggregate (entity class, domain events, errors, repository interface + EF implementation, DbContext wiring, migration) inside an existing module of this modular monolith, following this template's DDD conventions exactly.
---

# add-entity

Scaffold a new aggregate/entity inside an **existing** module of this repo (or one just
created via `/add-module`). Read this repo's `CLAUDE.md` (or equivalent architecture doc)
first, if one exists, for the entity conventions this skill applies. Ask the user for: the
**module** it belongs to, the **entity name** (PascalCase singular, e.g. `Venue`), its
**properties**, and whether it references another aggregate (by id).

Before scaffolding, find and read 1-2 existing entities in this repo as concrete references —
ideally one simple one with no relationships and one richer one with a relationship and/or
`Result`-based factory/behavior — and match their exact style (naming, visibility, namespace
layout). Below, the worked example scaffolds a `TodoItem` entity (title, description,
completion state) in a fictional `Tasks` module — a stand-in that doesn't collide with any
real entity in this repo, purely to show the pattern end to end. Swap `TodoItem`/`Tasks` for
whatever the user actually asked for; `<ProjectName>` stands for this repo's actual root
namespace/solution name.

## 1. Domain project — `Domain/TodoItems/TodoItem.cs`

```csharp
using <ProjectName>.Common.Domain;

namespace <ProjectName>.Modules.Tasks.Domain.TodoItems;

public sealed class TodoItem : Entity
{
    private TodoItem()
    {
    }

    public Guid Id { get; private set; }
    public string Title { get; private set; }
    public string Description { get; private set; }
    public bool IsCompleted { get; private set; }
    public DateTime? CompletedAtUtc { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    // public Guid RelatedAggregateId { get; private set; }   // store the FK id, not a navigation

    public static TodoItem Create(string title, string description, DateTime createdAtUtc)
    {
        var todoItem = new TodoItem
        {
            Id = Guid.NewGuid(),
            Title = title,
            Description = description,
            IsCompleted = false,
            CreatedAtUtc = createdAtUtc
        };

        todoItem.Raise(new TodoItemCreatedDomainEvent(todoItem.Id));

        return todoItem;
    }

    public Result Complete(DateTime completedAtUtc)
    {
        if (IsCompleted)
        {
            return Result.Failure(TodoItemErrors.AlreadyCompleted(Id));
        }

        IsCompleted = true;
        CompletedAtUtc = completedAtUtc;

        Raise(new TodoItemCompletedDomainEvent(Id));

        return Result.Success();
    }

    // For a no-op change, return early WITHOUT raising an event:
    public void UpdateDescription(string description)
    {
        if (Description == description)
        {
            return;
        }

        Description = description;

        Raise(new TodoItemDescriptionChangedDomainEvent(Id, description));
    }
}
```

Rules, no exceptions: `sealed class : Entity`; every settable property is
`{ get; private set; }`; a private **parameterless** constructor (EF); a `public static
Create(...)` factory that assembles the instance via an **object initializer** — never a
parameterized private constructor, and never split field assignment between a constructor
argument list and the initializer. Raise a `TodoItemCreatedDomainEvent`
from `Create(...)` when creation is itself a business event other handlers should react to
(the common case) — skip it only for an entity that exists purely to mirror/project state from
elsewhere (e.g. a local read-model copy of another module's aggregate, synced via an
integration event) and has no independent "created" meaning of its own. Behavior methods raise
their own domain event only when state actually changed — see `UpdateDescription`'s early
return above for the no-op case.

## 2. Domain events — one file per event, same folder/namespace

`Domain/TodoItems/TodoItemCreatedDomainEvent.cs`:
```csharp
using <ProjectName>.Common.Domain;

namespace <ProjectName>.Modules.Tasks.Domain.TodoItems;

public sealed class TodoItemCreatedDomainEvent(Guid todoItemId) : DomainEvent
{
    public Guid TodoItemId { get; } = todoItemId;
}
```
Name: `{Entity}{PastTenseVerb}DomainEvent`. Add one per behavior method that raises an event
(here: `TodoItemCompletedDomainEvent(Guid TodoItemId)`, `TodoItemDescriptionChangedDomainEvent(Guid TodoItemId, string Description)`).

## 3. Domain errors — `Domain/TodoItems/TodoItemErrors.cs`

```csharp
using <ProjectName>.Common.Domain;

namespace <ProjectName>.Modules.Tasks.Domain.TodoItems;

public static class TodoItemErrors
{
    public static Error NotFound(Guid todoItemId) =>
        Error.NotFound("TodoItems.NotFound", $"The todo item with the identifier {todoItemId} was not found");

    public static Error AlreadyCompleted(Guid todoItemId) =>
        Error.Problem("TodoItems.AlreadyCompleted", $"The todo item with the identifier {todoItemId} is already completed");
}
```
Always include the `NotFound(Guid)` method — it's the standard "get by id or fail" error used
by command handlers. Use `Error.Problem` for business-rule violations, `Error.Conflict` for
concurrency/uniqueness conflicts, `Error.Failure` only for generic unexpected failures.

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
(e.g. by a natural key). **Never add `Update` or `Save` methods** — EF change tracking plus
the handler's own `IUnitOfWork.SaveChangesAsync()` call handles that.

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

## 6. EF configuration — if the entity has a relationship or a column-level constraint

Add `Infrastructure/TodoItems/TodoItemConfiguration.cs` when the entity references another
aggregate **or** has a column-level constraint worth enforcing at the DB level (max length on a
string, a unique index, etc.) — most non-trivial entities end up with one for at least the
latter reason, even with zero relationships:
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

        // Only if the entity references another aggregate:
        // builder.HasOne<RelatedEntity>().WithMany().HasForeignKey(t => t.RelatedAggregateId);
    }
}
```
Only skip the configuration class entirely when the entity has **neither** a relationship
**nor** a constraint worth enforcing (a genuinely plain scalar entity) — check an existing
entity in this module first to see which case is more common here.

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
Adjust the `--project`/`--startup-project` paths and the module/entity names to match the
real request if this repo's actual folder layout differs. Never hand-write migration files —
always generate them, then commit the generated `.cs`/`.Designer.cs`/updated
`ModelSnapshot.cs` as-is.

## 10. Next step

The entity now exists but has no use cases — tell the user to run `/add-feature` to add
Create/Get/Update/etc. commands and queries for it.
