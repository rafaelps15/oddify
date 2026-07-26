---
name: add-module
description: Scaffold a brand-new business module (Domain/Application/Infrastructure/Presentation/PublicApi projects, DbContext, module composition root) and wire it into the solution and API host, following this modular monolith template's exact conventions.
---

# add-module

Scaffold a brand-new top-level module in this modular monolith, following the pattern of
modules that already exist in the repo. Read this repo's `CLAUDE.md` (or equivalent
architecture doc) at the repo root first, if one exists — this skill assumes you already know
the layered dependency rules it documents.

Ask the user for the **module name** (PascalCase) and the **schema name** (snake_case) if not
given. Confirm whether the module needs an `IntegrationEvents` project (only if it will publish
events for other modules to consume). Below, the worked example scaffolds a fictional `Tasks`
module (schema `tasks`) — a stand-in that doesn't collide with any real module in this repo —
swap it for whatever the user actually asked for. `<ProjectName>` stands for this repo's actual
root namespace/solution name.

## 1. Create the projects

Under `src/Modules/Tasks/`, create these class-library projects (matching the target
framework and settings in this repo's `Directory.Build.props`):

| Project | ProjectReference(s) |
|---|---|
| `<ProjectName>.Modules.Tasks.Domain` | `..\..\..\Common\<ProjectName>.Common.Domain\<ProjectName>.Common.Domain.csproj` |
| `<ProjectName>.Modules.Tasks.Application` | `Common.Application` + own `Domain` |
| `<ProjectName>.Modules.Tasks.Infrastructure` | `Common.Infrastructure` + own `Application` **and** own `Presentation` |
| `<ProjectName>.Modules.Tasks.Presentation` | `Common.Presentation` + own `Application` |
| `<ProjectName>.Modules.Tasks.PublicApi` | none — this project references nothing, not even `Common.Domain` |
| `<ProjectName>.Modules.Tasks.IntegrationEvents` (only if needed) | `Common.Application` only |

Use `dotnet new classlib -o <path> -n <ProjectName>.Modules.Tasks.<Layer>` then
`dotnet add reference` for each dependency — don't hand-write `.csproj` XML unless faster to
just copy an existing module's `.csproj` files and rename.

## 2. Domain project skeleton

- `AssemblyReference.cs` is **not** needed in Domain (only Application and Presentation expose one — see below).
- Create the folder structure per entity as the user adds entities later (`/add-entity`); no scaffolding needed here beyond the empty project.

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

`Abstractions/Data/IUnitOfWork.cs`:
```csharp
namespace <ProjectName>.Modules.Tasks.Application.Abstractions.Data;

public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
```

## 4. Presentation project skeleton

`AssemblyReference.cs` (same shape as Application's, own namespace).

`Tags.cs`:
```csharp
namespace <ProjectName>.Modules.Tasks.Presentation;

internal static class Tags
{
    // one const string per aggregate area, added as features are scaffolded
    // internal const string TodoItems = "TodoItems";
}
```

## 5. Infrastructure project skeleton — the module composition root

`Database/Schemas.cs`:
```csharp
namespace <ProjectName>.Modules.Tasks.Infrastructure.Database;

internal static class Schemas
{
    internal const string Tasks = "tasks";
}
```

`Database/TasksDbContext.cs`:
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

`TasksModule.cs` (the composition root every other wiring step calls into):
```csharp
using <ProjectName>.Common.Infrastructure.Interceptors;
using <ProjectName>.Modules.Tasks.Application.Abstractions.Data;
using <ProjectName>.Modules.Tasks.Infrastructure.Database;
using <ProjectName>.Common.Presentation.Endpoints;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
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

    private static void AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        string databaseConnectionString = configuration.GetConnectionString("Database")!;

        services.AddDbContext<TasksDbContext>((sp, options) =>
            options
                .UseNpgsql(databaseConnectionString, npgsqlOptions => npgsqlOptions
                    .MigrationsHistoryTable(HistoryRepository.DefaultTableName, Schemas.Tasks))
                .UseSnakeCaseNamingConvention()
                .AddInterceptors(sp.GetRequiredService<PublishDomainEventsInterceptor>()));

        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<TasksDbContext>());

        // services.AddScoped<ITodoItemRepository, TodoItemRepository>(); — added per entity by /add-entity
    }

    // Only add this if the module needs to consume integration events published by another module:
    // public static void ConfigureConsumers(IRegistrationConfigurator registrationConfigurator) =>
    //     registrationConfigurator.AddConsumer<SomeIntegrationEventConsumer>();
}
```

## 6. PublicApi project (scaffold for convention-consistency — check this repo's CLAUDE.md for whether it's actually wired anywhere; in many repos following this template it's a reserved/aspirational contract-only project, not yet implemented)

```csharp
namespace <ProjectName>.Modules.Tasks.PublicApi;

public interface ITasksApi
{
    // Task<TodoItemSummary?> GetTodoItemAsync(Guid todoItemId, CancellationToken cancellationToken = default);
}
```
Give it its own self-contained response DTOs (don't reference Application's DTOs — this
project must stay dependency-free). Do not implement or wire this interface anywhere unless
the user explicitly asks for real synchronous cross-module calls; check whether this repo's
other `PublicApi` projects are actually referenced by the solution file, and match that
precedent rather than assuming.

## 7. Wire into the solution

```
dotnet sln <ProjectName>.sln add src/Modules/Tasks/<ProjectName>.Modules.Tasks.Domain/<ProjectName>.Modules.Tasks.Domain.csproj --solution-folder Modules/Tasks
dotnet sln <ProjectName>.sln add src/Modules/Tasks/<ProjectName>.Modules.Tasks.Application/<ProjectName>.Modules.Tasks.Application.csproj --solution-folder Modules/Tasks
dotnet sln <ProjectName>.sln add src/Modules/Tasks/<ProjectName>.Modules.Tasks.Infrastructure/<ProjectName>.Modules.Tasks.Infrastructure.csproj --solution-folder Modules/Tasks
dotnet sln <ProjectName>.sln add src/Modules/Tasks/<ProjectName>.Modules.Tasks.Presentation/<ProjectName>.Modules.Tasks.Presentation.csproj --solution-folder Modules/Tasks
```
Match the existing modules' solution-folder nesting under `Modules\` in this repo. Check
whether this repo's `PublicApi` projects are conventionally added to the `.sln` or not, and
mention that decision to the user rather than silently deciding.

## 8. Wire into the API host

1. `<ProjectName>.Api.csproj` — add a `ProjectReference` to
   `<ProjectName>.Modules.Tasks.Infrastructure.csproj` only (it transitively pulls in
   Application + Presentation).
2. `Program.cs`:
   - Add `<ProjectName>.Modules.Tasks.Application.AssemblyReference.Assembly` to the array
     passed to `builder.Services.AddApplication([...])`.
   - Add `builder.Services.AddTasksModule(builder.Configuration);` alongside the other
     `Add<X>Module(...)` calls.
   - Add `"tasks"` to the array passed to `builder.Configuration.AddModuleConfiguration([...])`.
   - If the module consumes another module's integration events, add
     `TasksModule.ConfigureConsumers` to the array passed into
     `builder.Services.AddInfrastructure([...], databaseConnectionString, redisConnectionString)`.
3. Create `src/API/<ProjectName>.Api/modules.tasks.json` (content: `{}` or a module-specific
   config root, matching this repo's existing per-module config file naming) and optionally
   `modules.tasks.Development.json`.
4. `Extensions/MigrationExtensions.cs` (or wherever migrations are applied at startup) — add
   `ApplyMigration<TasksDbContext>(scope);` inside the migration-apply routine.

## 9. First migration

Once at least one entity exists (via `/add-entity`), generate the initial migration:
```
dotnet ef migrations add Create_Database --project src/Modules/Tasks/<ProjectName>.Modules.Tasks.Infrastructure --startup-project src/API/<ProjectName>.Api --context TasksDbContext -o Database/Migrations
```

## 10. After scaffolding

Run `dotnet build` on the solution to confirm everything compiles (check `Directory.Build.props`
for whether `TreatWarningsAsErrors` is on repo-wide — if so, warnings fail the build). Tell the
user the module is wired but empty — the next step is `/add-entity` to add aggregates, then
`/add-feature` for use cases.
