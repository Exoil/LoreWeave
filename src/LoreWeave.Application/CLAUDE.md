# CLAUDE.md — Application layer

General backend rules (testing, ID conventions, optimistic concurrency,
mapping, schemas) live in `../../CLAUDE.md`. **Read that first.** This file
only documents the concrete CQRS handler templates for the Application layer.

## Application Layer Patterns

This layer uses **CQRS via MessagePipe**. Every operation is a Command or Query
record dispatched to a dedicated handler. The `LogFilter<TRequest, TResponse>`
wraps all handlers automatically — no per-handler logging setup needed.

Sequence diagrams for all existing flows are in `../../../../schemas/sequences/`
(i.e. `schemas/sequences/` at the repo root, above `backend/` and `frontend/`).

---

## Result Types

All handlers return one of two readonly structs from `Models/Result.cs`:

| Type | When to use |
|------|-------------|
| `Result<TValue, Exception>` | Operation returns a value on success (e.g. new Id) |
| `Result<Exception>` | Operation returns nothing on success (void-like) |

Implicit conversions are defined — return the value or exception directly, no wrapping needed:
```csharp
return request.Id;          // success
return someException;       // failure
return new Result<Exception>(); // void success
```

---

## Adding a Command

Commands and queries are grouped **per entity**: `Commands/Characters/`,
`Commands/Facts/`, `Commands/Knows/` (and the same under `Queries/`), each
with a nested `CommandHandlers/` / `QueryHandlers/` folder.

### 1. Create the command record — `Commands/<Entity>/`
```csharp
namespace LoreWeave.Application.Commands.Characters;

public record MyOperationCommand(Guid Id, string SomeField);
```
- Identifiers are `Guid`. Generate new entity ids with `Guid.CreateVersion7()`.
- Use primitive types only — no domain objects in command records.

### 2. Create the handler — `Commands/<Entity>/CommandHandlers/`
```csharp
using MessagePipe;
using LoreWeave.Application.Models;
using LoreWeave.Domain.Repositories.Characters;
using LoreWeave.Domain.Transactions;
using ILogger = Serilog.ILogger;

namespace LoreWeave.Application.Commands.Characters.CommandHandlers;

public class MyOperationCommandHandler : IAsyncRequestHandler<MyOperationCommand, Result<Exception>>
{
    private readonly IExistsCharacter _existsCharacter;
    private readonly ICharacterWriter _characterWriter;
    private readonly ILogger _logger;
    private readonly ITransactionFactory _transactionFactory;

    public MyOperationCommandHandler(
        ITransactionFactory transactionFactory,
        IExistsCharacter existsCharacter,
        ICharacterWriter characterWriter,
        ILogger logger)
    {
        _transactionFactory = transactionFactory;
        _existsCharacter = existsCharacter;
        _characterWriter = characterWriter;
        _logger = logger;
    }

    public async ValueTask<Result<Exception>> InvokeAsync(
        MyOperationCommand request,
        CancellationToken cancellationToken = new())
    {
        await using var transaction = await _transactionFactory.CreateAsync();

        try
        {
            // 1. Optional: check existence, return domain exception if not found
            // 2. Build domain command and call repository
            // 3. Commit
            await transaction.CommitAsync();
            _logger.Information("...", request.Id);
        }
        catch (Exception exception)
        {
            await transaction.RollbackAsync();
            _logger.Error(exception, "...", request.Id);
            return exception;
        }

        return new Result<Exception>();
    }
}
```

**Rules:**
- Always `await using var transaction = await _transactionFactory.CreateAsync()` — never pass raw sessions.
- Inject only the Domain **role interfaces** the handler needs (`IExists<Entity>`, `I<Entity>Reader`, `I<Entity>Writer`, `IFactConnection`) — never a whole repository, never driver types (`Neo4j.Driver` must not appear in this layer).
- Always `CommitAsync()` on success, `RollbackAsync()` in catch.
- Ids are `Guid` end-to-end — pass `request.Id` straight to the repository, no conversion.
- Return domain exceptions (`NotFoundException`, `PreconditionException`, `UnprocessableContentException`) directly — they implicitly convert to `Result`.
- If the command creates a new entity and must return its Id, use `Result<Guid, Exception>` instead of `Result<Exception>`.

### 3. Register the handler — `IoC/HandlerConfiguration.cs`
```csharp
.AddAsyncRequestHandler<MyOperationCommandHandler>()
```

---

## Adding a Query

### 1. Create the query record — `Queries/<Entity>/`
```csharp
namespace LoreWeave.Application.Queries.Characters;

public record MyQuery(Guid Id);
```

### 2. Create a payload model if needed — `Models/`
```csharp
namespace LoreWeave.Application.Models;

public record MyPayload(Guid Id, string Name);
```

### 3. Create the handler — `Queries/<Entity>/QueryHandlers/`
```csharp
public class MyQueryHandler : IAsyncRequestHandler<MyQuery, Result<MyPayload, Exception>>
{
    // same constructor pattern as commands

    public async ValueTask<Result<MyPayload, Exception>> InvokeAsync(
        MyQuery request,
        CancellationToken cancellationToken = new())
    {
        await using var transaction = await _transactionFactory.CreateAsync();

        try
        {
            // 1. Optional: ExistsAsync check → return NotFoundException if missing
            // 2. GetAsync from repository
            // 3. Map domain entity → payload (ids are already Guid)
            return new MyPayload(...);
        }
        catch (Exception exception)
        {
            _logger.Error(exception, "...", request.Id);
            return exception;
        }
    }
}
```

**Rules:**
- Queries do **not** commit or rollback — they are read-only.
- Always map domain entities to payload models before returning (never leak domain types).

### 4. Register the handler — `IoC/HandlerConfiguration.cs`
```csharp
.AddAsyncRequestHandler<MyQueryHandler>()
```

---

## Existence Check Pattern

When an operation requires a character to exist:
```csharp
var exists = await _existsCharacter.CharacterExistsAsync(transaction, request.Id);

if (!exists.Exists)
{
    _logger.Error("...: {Id}", request.Id);
    return new NotFoundException(Entities.Character);
}
```

For optimistic concurrency on updates, also check version:
```csharp
if (exists.Version != request.Version)
{
    _logger.Error("...: {Id}", request.Id);
    return new PreconditionException();
}
```