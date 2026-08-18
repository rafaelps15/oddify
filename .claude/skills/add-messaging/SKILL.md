---
name: add-messaging
description: Wire a cross-module integration event in a .NET Modular Monolith solution — publish via the Outbox from the publishing module, consume via the Inbox in the consuming module(s). Use when the user asks to publish/emit/raise an integration event, notify another module, react to another module's event, or set up outbox/inbox messaging for a module that doesn't have it yet.
argument-hint: <what happened and which module(s) need to react, e.g. "TodoItem completed, mirror it into the Rewards module">
---

# Add Messaging

Wires one **cross-module integration event** end to end: the contract, the publishing module's
outbox write, and each consuming module's inbox + real handler. This assumes the same architecture
as `add-feature`/`add-entity`: Modular Monolith with per-module `Domain`/`Application`/
`Infrastructure`/`Presentation` projects, MediatR, the `Result` pattern, and — specific to
messaging — a **hand-rolled in-memory event bus** (`InMemoryEventBus`, process-static
`Subscribe`/`Publish`, no message-broker library) plus a **persisted Outbox/Inbox** for
at-least-once delivery across modules. Full mechanics live in `CLAUDE.md` §5/§10 — read those
first; this skill is their executable counterpart, not a replacement for understanding the flow.

If the target repo uses a real message broker (MassTransit, NServiceBus, raw RabbitMQ/Kafka client)
instead of a hand-rolled bus, stop and confirm with the user before applying this skill — it
encodes one specific, simpler mechanism, not messaging in general.

## The flow this skill wires (memorize this before touching any file)

```
[Publishing module]                              [Consuming module]
Domain event raised on an aggregate
        │
        ▼
InsertOutboxMessagesInterceptor (automatic,       ← nothing to write for this step
same transaction as the business write)
        │
        ▼
Domain-event handler (Application) builds the
integration event contract, calls
IOutboxWriter.Enqueue(...) + IUnitOfWork.Save...
        │
        ▼
outbox_messages row (that module's own schema)
        │
        ▼  (OutboxProcessorJob, polling)
InMemoryEventBus.Publish  ───────────────────────▶  IntegrationEventGenericHandler<T>
                                                     (subscribed by <Module>Module.Initialize)
                                                              │
                                                              ▼
                                                     inbox_messages row (consumer's own schema)
                                                              │
                                                              ▼  (ProcessInboxJob, polling)
                                                     Real IIntegrationEventHandler<T>
                                                     (Presentation/IntegrationEvents/)
                                                              │
                                                              ▼
                                                     ISender.Send(some command)
```

Two independent things can be missing, and the failure mode is always silent (row sits
unprocessed, nothing throws): a publishing module without `AddOutboxProcessor(schema)`, or a
consuming module without `AddInboxProcessor(schema, presentationAssembly)` **and**
`<Module>Module.Initialize(...)` called from `Program.cs`. Check both ends before assuming the
contract or handler is wrong.

## Step 0 — Detect the project's real conventions

1. Resolve `<RootNamespace>` and confirm module names the same way as `add-entity` Step 0.
2. Read `Common.Application/EventBus/` (`IEventBus`, `IIntegrationEvent`, `IIntegrationEventHandler`,
   `IntegrationEvent`, `IntegrationEventHandler<T>`) and `Common.Application/Outbox/IOutboxWriter.cs`
   — confirm these exact shapes before writing against them.
3. Read one existing publisher (a domain-event handler that calls `IOutboxWriter.Enqueue`) and one
   existing consumer (an `IntegrationEventHandler<T>` in some module's `Presentation/IntegrationEvents/`)
   end to end — architectures drift, don't blindly trust this file over the code that's actually there.
4. Check whether the **consuming** module already consumes *anything* — look for an
   `Infrastructure/Inbox/IntegrationEventGenericHandler.cs`, a `<Module>Module.Initialize(...)`
   method, and a `<Module>Module.Initialize(app.Services)` call in `Program.cs`. If none of these
   exist yet, this is that module's **first** consumed event — Step 4 below has extra one-time
   setup; skip it if the module already consumes something else.
5. Check whether the **publishing** module already publishes *anything* — look for a
   `<Module>.IntegrationEvents` project and a `services.AddOutboxProcessor(Schemas.<Module>)` call
   in its composition root. If missing, this is that module's first published event — Step 2 below
   has extra one-time setup.

## Step 1 — The integration event contract

File: `src/Modules/<Publisher>/<RootNamespace>.Modules.<Publisher>.IntegrationEvents/<Entity><PastTenseVerb>IntegrationEvent.cs`.
This project depends on `Common.Application` only — never on the publisher's own
Domain/Application/Infrastructure/Presentation, and never referenced by anything except: the
publisher's own `Application` layer (to construct it) and any consumer's `Presentation` project (to
implement a handler for it).

```csharp
using <RootNamespace>.Common.Application.EventBus;

namespace <RootNamespace>.Modules.Todos.IntegrationEvents;

public sealed class TodoItemCompletedIntegrationEvent(Guid id, DateTime occurredOnUtc, Guid todoItemId, string title)
    : IntegrationEvent(id, occurredOnUtc)
{
    public Guid TodoItemId { get; init; } = todoItemId;

    public string Title { get; init; } = title;
}
```

Plain data, `{ get; init; }` properties, one file per event. Never reuse a Domain entity or an
Application `Response` type here — the wire contract must be independent of either.

## Step 2 — Publish it (publishing module)

In the **publishing** module's `Application` layer, a domain-event handler builds the contract and
enqueues it. It typically **re-queries fresh state** via `ISender.Send(new Get<X>Query(...))`
rather than trusting the domain-event notification's own payload (state may have moved on further
by the time this runs):

```csharp
internal sealed class TodoItemCompletedDomainEventHandler(ISender sender, IOutboxWriter outboxWriter, IUnitOfWork unitOfWork)
    : IDomainEventHandler<TodoItemCompletedDomainEvent>
{
    public async Task Handle(TodoItemCompletedDomainEvent notification, CancellationToken cancellationToken)
    {
        Result<TodoItemResponse> result = await sender.Send(new GetTodoItemQuery(notification.TodoItemId), cancellationToken);

        if (result.IsFailure)
        {
            throw new AppException(nameof(GetTodoItemQuery), result.Error);
        }

        outboxWriter.Enqueue(
            new TodoItemCompletedIntegrationEvent(notification.Id, notification.OccurredOnUtc, result.Value.Id, result.Value.Title));

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
```

A `Result.Failure` here is escalated with `throw new AppException(...)` — a domain notification
handler has no `Result` channel of its own. **`IEventBus.PublishAsync(...)` directly (skipping the
outbox) is a deliberate exception, not a shortcut** — only for an event where losing it on a crash
between commit and publish is genuinely acceptable; default to `IOutboxWriter` for everything else.

**One-time setup, only if this module has never published before** (Step 0.5):
- Add `DbSet<OutboxMessage> OutboxMessages { get; set; }` to the module's `DbContext`, and
  `modelBuilder.ApplyConfiguration(new OutboxMessageConfiguration())` in `OnModelCreating` (this
  configuration lives in `Common.Infrastructure`, so `ApplyConfigurationsFromAssembly` won't find
  it on its own).
- `.AddInterceptors(sp.GetRequiredService<InsertOutboxMessagesInterceptor>())` on that `DbContext`'s
  `AddDbContext` call, if not already there (needed regardless of Outbox — it's also what makes
  in-module domain events reach their local `IDomainEventHandler<T>`s).
- In the module's composition root: `services.AddOutboxWriter<TasksDbContext>();` and
  `services.AddOutboxProcessor(Schemas.Tasks);` — the latter is what actually registers the Quartz
  job draining this schema's `outbox_messages`. Forgetting it is silent: the row gets written, the
  request succeeds, and nothing ever publishes it.
- Generate the EF migration (`dotnet ef migrations add Add_Outbox --project ... --context
  TasksDbContext -o Database/Migrations`, per `CLAUDE.md` §9) if `OutboxMessage` isn't in this
  module's schema yet.

## Step 3 — Consume it (consuming module's real handler)

In the **consuming** module's `Presentation/IntegrationEvents/` folder, one file per event:

```csharp
public sealed class TodoItemCompletedIntegrationEventConsumer(ISender sender)
    : IntegrationEventHandler<TodoItemCompletedIntegrationEvent>
{
    public override async Task Handle(TodoItemCompletedIntegrationEvent integrationEvent, CancellationToken cancellationToken = default)
    {
        Result result = await sender.Send(new MirrorTodoItemCommand(integrationEvent.TodoItemId, integrationEvent.Title), cancellationToken);

        if (result.IsFailure)
        {
            throw new AppException(nameof(MirrorTodoItemCommand), result.Error);
        }
    }
}
```

Extend the abstract `IntegrationEventHandler<T>` base (bridges the untyped
`IIntegrationEventHandler.Handle(IIntegrationEvent, ct)` that `ProcessInboxJob` calls through to
this typed override) — never implement `IIntegrationEventHandler<T>` directly by hand. This
project needs a `ProjectReference` to the publisher's `<Module>.IntegrationEvents` project **only**
(never the publisher's Domain/Application/Infrastructure/Presentation).

**No manual registration for this class** — `AddIntegrationEventHandlers` (below) finds it by
reflection. If the target aggregate needs to be created rather than just mutated by the consumed
event (a "mirror" entity that only exists to shadow another module's aggregate), use `add-entity`
first — its `Create(Guid id, ...)` factory should take the **foreign** id directly as its own `Id`
and raise no domain event of its own (see `CLAUDE.md` §8/§10, "mirror" case).

## Step 4 — One-time inbox setup (only if this module has never consumed anything before)

Skip entirely if Step 0.4 found this module already has `Initialize`/`IntegrationEventGenericHandler`
— adding a second consumed event type needs nothing here, reflection picks it up automatically.

1. **`Infrastructure/Inbox/IntegrationEventGenericHandler.cs`** — a generic, business-logic-free
   handler that just inserts whatever it's given into this module's own `inbox_messages`:
   ```csharp
   internal sealed class IntegrationEventGenericHandler<TIntegrationEvent>(IServiceProvider serviceProvider)
       : IntegrationEventHandler<TIntegrationEvent>
       where TIntegrationEvent : IIntegrationEvent
   {
       private static readonly JsonSerializerOptions SerializerOptions = new() { PropertyNameCaseInsensitive = true };

       public override async Task Handle(TIntegrationEvent integrationEvent, CancellationToken cancellationToken = default)
       {
           using IServiceScope scope = serviceProvider.CreateScope();
           IDbConnectionFactory dbConnectionFactory = scope.ServiceProvider.GetRequiredService<IDbConnectionFactory>();
           await using DbConnection connection = await dbConnectionFactory.OpenConnectionAsync();

           const string sql = "INSERT INTO tasks.inbox_messages(id, type, content, occurred_on_utc) VALUES (@Id, @Type, @Content::jsonb, @OccurredOnUtc)";

           await connection.ExecuteAsync(new CommandDefinition(sql, new
           {
               integrationEvent.Id,
               Type = typeof(TIntegrationEvent).AssemblyQualifiedName,
               Content = JsonSerializer.Serialize(integrationEvent, SerializerOptions),
               integrationEvent.OccurredOnUtc
           }, cancellationToken: cancellationToken));
       }
   }
   ```
   Schema is a hard-coded literal in the SQL (`tasks.inbox_messages`, not string-interpolated from
   a variable) — match whatever the target repo's own existing generic handlers already do here
   (some repos parametrize it safely because of registration order, some hard-code it; verify
   before choosing).
2. **`<Module>Module.Initialize(IServiceProvider serviceProvider)`** — see the Step 5 code block in
   `CLAUDE.md` §10 for the exact reflection + `Activator.CreateInstance` + `eventBus.Subscribe(...)`
   shape. Add it next to `ConfigureConsumers`-style methods if the repo still has an older name for
   this; otherwise this is a brand-new method.
3. In the module's composition root: `services.AddIntegrationEventHandlers(Presentation.AssemblyReference.Assembly);`
   (registers the real handlers from Step 3 for later resolution) and
   `services.AddInboxProcessor(Schemas.Tasks, Presentation.AssemblyReference.Assembly);` (wires the
   Quartz job that drains this schema's `inbox_messages`).
4. Add `DbSet<InboxMessage> InboxMessages { get; set; }` to the module's `DbContext`, and
   `modelBuilder.ApplyConfiguration(new InboxMessageConfiguration())` in `OnModelCreating`.
5. In `Program.cs`, **after** `builder.Build()` (it needs `IEventBus` from the built container) and
   **before** any request-handling code runs: `TasksModule.Initialize(app.Services);`.
6. Generate the EF migration for the new `inbox_messages` table.

## Checklist before finishing

- [ ] Integration event contract is a plain, sealed `IntegrationEvent` subclass in
      `<Publisher>.IntegrationEvents`, `{ get; init; }` properties only
- [ ] Publishing domain-event handler enqueues via `IOutboxWriter.Enqueue` (not `IEventBus.PublishAsync`,
      unless the non-durable exception is a deliberate, confirmed choice) and calls
      `unitOfWork.SaveChangesAsync` itself
- [ ] Publishing module has `AddOutboxWriter<TContext>()` + `AddOutboxProcessor(schema)` — check
      this even if the event contract already existed; a *new publisher* of an existing event still
      needs both
- [ ] Real consumer extends `IntegrationEventHandler<T>` (not `IIntegrationEventHandler<T>` directly),
      lives in `Presentation/IntegrationEvents/`, references only the publisher's `IntegrationEvents`
      project
- [ ] Consuming module has `AddIntegrationEventHandlers(...)` + `AddInboxProcessor(...)` +
      `Initialize(...)` wired, and `Program.cs` calls `<Module>Module.Initialize(app.Services)`
      after `builder.Build()` — all four, every one of them silent-failure if skipped
- [ ] No cross-module reference beyond the sanctioned ones: publisher's `Application` builds the
      event, consumer's `Presentation` implements the handler — never a consumer's `Infrastructure`
      referencing the publisher's `IntegrationEvents` project by name (reflection avoids this, see
      `CLAUDE.md` §10 step 5)
- [ ] EF migrations generated for any new `OutboxMessage`/`InboxMessage` table this touched
