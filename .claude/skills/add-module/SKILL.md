---
name: add-module
description: Scaffold a brand-new business module (Domain/Application/Infrastructure/Presentation/PublicApi/IntegrationEvents projects, DbContext, module composition root) and wire it into the solution and API host, following this modular monolith template's exact conventions.
---

# add-module

Scaffold a brand-new top-level module in this modular monolith, matching the pattern of modules
that already exist in this repo exactly. Read this repo's root `CLAUDE.md` first if one exists —
this skill assumes you already know the layered dependency rules it documents; where the two
disagree, this repo's own `CLAUDE.md` wins (it's the ground truth for this specific codebase).

Ask the user for the **module name** (PascalCase, e.g. `Billing`) and the **schema name**
(snake_case, usually just the module name lowercased) if not given. Confirm whether the module
needs an `IntegrationEvents` project (only if it will **publish** events for other modules to
consume — a module that only *consumes* another module's events doesn't need one of its own).
Below, the worked example scaffolds a fictional `Tasks` module (schema `tasks`) — a stand-in that
won't collide with any real module in this repo — swap it for whatever the user actually asked
for. `<ProjectName>` stands for this repo's actual root namespace/solution name.

## 1. Create the projects

Under `src/Modules/Tasks/`, create these class-library projects, matching the `TargetFramework`
and analyzer settings this repo's `Directory.Build.props` already sets solution-wide (don't
re-specify them per-project):

| Project | `ProjectReference`(s) |
|---|---|
| `<ProjectName>.Modules.Tasks.Domain` | `..\..\..\Common\<ProjectName>.Common.Domain\<ProjectName>.Common.Domain.csproj` |
| `<ProjectName>.Modules.Tasks.Application` | `Common.Application` + own `Domain` (+ own `IntegrationEvents`, if this module publishes) |
| `<ProjectName>.Modules.Tasks.Infrastructure` | `Common.Infrastructure` + own `Application` **and** own `Presentation` |
| `<ProjectName>.Modules.Tasks.Presentation` | `Common.Presentation` + own `Application` (+ **another** module's `IntegrationEvents` project only, if this module consumes it) |
| `<ProjectName>.Modules.Tasks.PublicApi` | **none** — this project references nothing, not even `Common.Domain` (see step 6) |
| `<ProjectName>.Modules.Tasks.IntegrationEvents` (only if publishing) | `Common.Application` only |

Use `dotnet new classlib -o <path> -n <ProjectName>.Modules.Tasks.<Layer>` then
`dotnet add reference` for each dependency — or copy an existing module's `.csproj` files and
rename/re-point the references, whichever is faster; don't hand-write the XML from scratch.

## 2. Domain project skeleton

- No `AssemblyReference.cs` here — nothing reflection-scans `Domain` (only `Application` and
  `Presentation` expose one, step 4).
- No files needed beyond the empty project; the folder structure per entity is created by
  `/add-entity` as entities are added.

## 3. Application project skeleton

`AssemblyReference.cs`:
```csharp
using System.Reflection;

namespace <ProjectName>.Modules.Tasks.Application;

public static class AssemblyReference
{
    public static readonly Assembly Assembly = typeof(AssemblyReference).Assembly;
}
```

`Abstractions/Data/IUnitOfWork.cs` — **each module defines its own copy of this interface**, it
is not shared from `Common.Application`:
```csharp
namespace <ProjectName>.Modules.Tasks.Application.Abstractions.Data;

public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
```

## 4. Presentation project skeleton

`AssemblyReference.cs` — identical shape to Application's, own namespace.

`Tags.cs`:
```csharp
namespace <ProjectName>.Modules.Tasks.Presentation;

internal static class Tags
{
    // one const string per aggregate area, added as /add-feature scaffolds endpoints
    // internal const string TodoItems = "TodoItems";
}
```

## 5. Infrastructure project skeleton — the module's composition root

`Database/Schemas.cs`:
```csharp
namespace <ProjectName>.Modules.Tasks.Infrastructure.Database;

internal static class Schemas
{
    internal const string Tasks = "tasks";
}
```

`Database/TasksDbContext.cs` — implements the module's **own** `IUnitOfWork` directly:
```csharp
using <ProjectName>.Modules.Tasks.Application.Abstractions.Data;
using Microsoft.EntityFrameworkCore;

namespace <ProjectName>.Modules.Tasks.Infrastructure.Database;

public sealed class TasksDbContext(DbContextOptions<TasksDbContext> options) : DbContext(options), IUnitOfWork
{
    // internal DbSet<TodoItem> TodoItems { get; set; }  — added per entity by /add-entity

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schemas.Tasks);

        // modelBuilder.ApplyConfiguration(new TodoItemConfiguration()); — added per entity, if it needs one
    }
}
```

`TasksModule.cs` — the composition root every wiring step below calls into:
```csharp
using <ProjectName>.Common.Infrastructure.Outbox;
using <ProjectName>.Common.Presentation.Endpoints;
using <ProjectName>.Modules.Tasks.Application.Abstractions.Data;
using <ProjectName>.Modules.Tasks.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace <ProjectName>.Modules.Tasks.Infrastructure;

public static class TasksModule
{
    public static IServiceCollection AddTasksModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddEndpoints(Presentation.AssemblyReference.Assembly);
        services.AddInfrastructure(configuration);
        return services;
    }

    // Only add this method if the module CONSUMES another module's integration events:
    // public static void ConfigureConsumers(IRegistrationConfigurator registrationConfigurator) =>
    //     registrationConfigurator.AddConsumer<SomeIntegrationEventConsumer>();

    private static void AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        string databaseConnectionString = configuration.GetConnectionString("Database")!;

        services.AddDbContext<TasksDbContext>((sp, options) => options
            .UseNpgsql(databaseConnectionString, npgsqlOptions => npgsqlOptions
                .MigrationsHistoryTable(HistoryRepository.DefaultTableName, Schemas.Tasks))
            .UseSnakeCaseNamingConvention()
            .AddInterceptors(sp.GetRequiredService<PublishDomainEventsInterceptor>()));

        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<TasksDbContext>());

        // services.AddScoped<ITodoItemRepository, TodoItemRepository>(); — added per entity by /add-entity
    }
}
```
`PublishDomainEventsInterceptor` lives in `Common.Infrastructure`'s `Outbox` namespace — despite
the name, it's a synchronous in-process MediatR publish that runs right after `SaveChangesAsync`
commits, **not** a persisted transactional outbox (no outbox table, no retry). See this repo's
`CLAUDE.md` for the full explanation before assuming any at-least-once delivery guarantee exists.

## 6. `PublicApi` project — reserved/aspirational, verify before assuming it works

```csharp
namespace <ProjectName>.Modules.Tasks.PublicApi;

public interface ITasksApi
{
    // Task<TodoItemSummary?> GetTodoItemAsync(Guid todoItemId, CancellationToken cancellationToken = default);
}
```
Give it its own self-contained response DTOs (never reference `Application`'s DTOs — this
project must depend on nothing, not even `Common.Domain`). **Check this repo's other modules'
`PublicApi` projects before doing anything more than scaffolding this skeleton**: are they added
to the `.sln`? Is the interface actually implemented and registered anywhere? Referenced by any
other module's `.csproj`? In this template's baseline, the answer to all three is no — these
projects exist purely as a reserved contract shape for a synchronous cross-module call mechanism
that was never finished. Match that precedent (scaffold it, leave it unimplemented and out of the
`.sln`) unless the user explicitly asks you to make one real, which is a deliberate architectural
decision worth confirming explicitly rather than assuming.

## 7. Wire into the solution

```
dotnet sln <ProjectName>.sln add src/Modules/Tasks/<ProjectName>.Modules.Tasks.Domain/<ProjectName>.Modules.Tasks.Domain.csproj --solution-folder Modules/Tasks
dotnet sln <ProjectName>.sln add src/Modules/Tasks/<ProjectName>.Modules.Tasks.Application/<ProjectName>.Modules.Tasks.Application.csproj --solution-folder Modules/Tasks
dotnet sln <ProjectName>.sln add src/Modules/Tasks/<ProjectName>.Modules.Tasks.Infrastructure/<ProjectName>.Modules.Tasks.Infrastructure.csproj --solution-folder Modules/Tasks
dotnet sln <ProjectName>.sln add src/Modules/Tasks/<ProjectName>.Modules.Tasks.Presentation/<ProjectName>.Modules.Tasks.Presentation.csproj --solution-folder Modules/Tasks
```
Match the existing modules' solution-folder nesting under `Modules\` in this repo exactly. Per
step 6, **don't** add `PublicApi` to the `.sln` unless this repo's other `PublicApi` projects
already are (check first) — and only add `IntegrationEvents` if this module publishes.

## 8. Wire into the API host

1. `<ProjectName>.Api.csproj` — add a `ProjectReference` to
   `<ProjectName>.Modules.Tasks.Infrastructure.csproj` only (it transitively pulls in
   Application + Presentation — never reference a module's Application/Presentation directly
   from the API host project).
2. `Program.cs`:
   - Add `<ProjectName>.Modules.Tasks.Application.AssemblyReference.Assembly` to the array
     passed to `builder.Services.AddApplication([...])`.
   - Add `builder.Services.AddTasksModule(builder.Configuration);` alongside the other
     `Add<X>Module(...)` calls (order between modules doesn't matter — they don't depend on
     each other at this point).
   - Add `"tasks"` to the array passed to `builder.Configuration.AddModuleConfiguration([...])`.
   - If (and only if) the module consumes another module's integration events, add
     `TasksModule.ConfigureConsumers` to the array passed into
     `builder.Services.AddInfrastructure([...], databaseConnectionString, redisConnectionString)`.
3. Create `src/API/<ProjectName>.Api/modules.tasks.json` (content: `{}` unless the module needs
   its own config root) and, optionally, `modules.tasks.Development.json` — matching this repo's
   existing per-module config file naming exactly.
4. Wherever migrations are applied at startup (typically `Extensions/MigrationExtensions.cs`),
   add `ApplyMigration<TasksDbContext>(scope);` inside the migration-apply routine, alongside the
   other modules' DbContexts. This path only runs in Development — never assume it applies
   migrations in any other environment.

## 9. First migration

Once at least one entity exists (via `/add-entity`), generate the initial migration:
```
dotnet ef migrations add Create_Database --project src/Modules/Tasks/<ProjectName>.Modules.Tasks.Infrastructure --startup-project src/API/<ProjectName>.Api --context TasksDbContext -o Database/Migrations
```
Adjust paths/names to match the real request if this repo's actual folder layout differs. Never
hand-write a migration file — always generate it, then commit the generated
`.cs`/`.Designer.cs`/updated `ModelSnapshot.cs` as-is.

## 10. After scaffolding

Run `dotnet build` on the solution to confirm everything compiles — check `Directory.Build.props`
for `TreatWarningsAsErrors`/`AnalysisMode`; if set repo-wide (it is in this template's baseline),
a warning fails the build exactly like an error. Tell the user the module is wired but empty —
the next step is `/add-entity` to add aggregates, then `/add-feature` for use cases.
