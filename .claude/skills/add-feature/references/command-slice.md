# Command Slice Templates

Files go in `Application/<Entities>/<Verb><Entity>/`. Replace `<RootNamespace>`, `<Module>`,
`<Entities>`/`<Entity>` with the real values resolved in Step 0.

## A1. `<Verb><Entity>Command.cs`

```csharp
using <RootNamespace>.Common.Application.Messaging;

namespace <RootNamespace>.Modules.Todos.Application.TodoItems.CreateTodoItem;

public sealed record CreateTodoItemCommand(string Title, string? Description, DateTime? DueDateUtc)
    : ICommand<Guid>;
```

- `public sealed record`, implements `ICommand<TResponse>` when it returns something (typically the new
  id), or plain `ICommand` when it returns nothing (e.g. a state transition like completing an item).
- Positional record parameters mirror exactly what the caller must supply — no more, no less. Anything
  the domain can derive (an `Id`, a status) does not appear here.

For a command with no response (state transition), same shape, no generic:

```csharp
public sealed record CompleteTodoItemCommand(Guid TodoItemId) : ICommand;
```

## A2. `<Verb><Entity>CommandHandler.cs`

```csharp
using <RootNamespace>.Common.Application.Messaging;
using <RootNamespace>.Common.Domain;
using <RootNamespace>.Modules.Todos.Application.Abstractions.Data;
using <RootNamespace>.Modules.Todos.Domain.TodoItems;

namespace <RootNamespace>.Modules.Todos.Application.TodoItems.CreateTodoItem;

internal sealed class CreateTodoItemCommandHandler(ITodoItemRepository todoItemRepository, IUnitOfWork unitOfWork)
    : ICommandHandler<CreateTodoItemCommand, Guid>
{
    public async Task<Result<Guid>> Handle(CreateTodoItemCommand request, CancellationToken cancellationToken)
    {
        Result<TodoItem> createResult = TodoItem.Create(request.Title, request.Description, request.DueDateUtc);

        if (createResult.IsFailure)
        {
            return Result.Failure<Guid>(createResult.Error);
        }

        TodoItem todoItem = createResult.Value;

        todoItemRepository.Insert(todoItem);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return todoItem.Id;
    }
}
```

And a state-transition handler that first loads the aggregate:

```csharp
internal sealed class CompleteTodoItemCommandHandler(ITodoItemRepository todoItemRepository, IUnitOfWork unitOfWork)
    : ICommandHandler<CompleteTodoItemCommand>
{
    public async Task<Result> Handle(CompleteTodoItemCommand request, CancellationToken cancellationToken)
    {
        TodoItem? todoItem = await todoItemRepository.GetAsync(request.TodoItemId, cancellationToken);

        if (todoItem is null)
        {
            return Result.Failure(TodoItemErrors.NotFound(request.TodoItemId));
        }

        Result completeResult = todoItem.Complete();

        if (completeResult.IsFailure)
        {
            return completeResult;
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
```

Rules — these are non-negotiable, they're what makes this a Modular Monolith following the domain model
rather than an anemic transaction script:

- `internal sealed class`, primary-constructor DI for the repository and `IUnitOfWork`.
- **All** business logic lives on the entity (`TodoItem.Create`, `todoItem.Complete()`), never
  re-implemented in the handler. The handler's job is: load → delegate to the entity → persist. Never
  set entity properties directly from the handler.
- Every failure path returns via `Result.Failure(...)`/`Result.Failure<T>(...)` — never `throw`.
- `unitOfWork.SaveChangesAsync(cancellationToken)` is called exactly once, only after every possible
  failure has already returned. Never call it before a check that can still fail.
- When the return type is a bare value that converts implicitly (`Guid`, an entity id, a response
  record), return the bare value at the end (`return todoItem.Id;`) — rely on `Result<T>`'s implicit
  conversion instead of wrapping it in `Result.Success(...)` explicitly. For `Result` (no value), return
  `Result.Success()` explicitly.

## A3. `<Verb><Entity>CommandValidator.cs`

```csharp
using FluentValidation;

namespace <RootNamespace>.Modules.Todos.Application.TodoItems.CreateTodoItem;

internal sealed class CreateTodoItemCommandValidator : AbstractValidator<CreateTodoItemCommand>
{
    public CreateTodoItemCommandValidator()
    {
        RuleFor(c => c.Title).NotEmpty();
        RuleFor(c => c.DueDateUtc)
            .GreaterThan(DateTime.UtcNow)
            .When(c => c.DueDateUtc.HasValue);
    }
}
```

Rules:
- `internal sealed class : AbstractValidator<TCommand>`, constructor-only rules, no methods.
- Validate structural/input constraints here (`NotEmpty`, `GreaterThan`, `MaximumLength`, id-not-empty).
  Do **not** validate business rules that need a database lookup or domain knowledge (e.g. "title must
  be unique", "item must not already be completed") — those stay in the entity/handler, surfaced as a
  `Result.Failure` with a specific `<Entity>Errors` entry, not a validation error.
- Use FluentValidation's built-in messages/error codes as-is — don't call `.WithMessage(...)` or
  `.WithErrorCode(...)` unless the existing codebase already does so elsewhere. The validation pipeline
  wraps every failing rule into a `ValidationError` (`ErrorType.Validation`, HTTP 400) automatically; you
  never construct that error type by hand.
- Every `ICommand`/`ICommand<T>` needs a validator file, even if it only validates that an id
  `NotEmpty()` — don't skip it because the rule feels trivial.
- Validators, handlers, and endpoints are all discovered by assembly scanning at startup — do **not**
  add manual DI registrations for any of the three.

## Domain additions (if needed)

If the command's rule needs a new `<Entity>Errors` entry or a new domain event, add them on the entity
itself (see the `add-entity` skill) rather than working around their absence in the handler.
