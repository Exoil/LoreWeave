using LoreWeave.Application.Models;
using LoreWeave.Domain.Factories;
using LoreWeave.Domain.Repositories;

using MessagePipe;

using Neo4j.Driver;

using Serilog;

namespace LoreWeave.Application.Commands.CommandHandlers;

public sealed class CreateFactCommandHandler : IAsyncRequestHandler<CreateFactCommand, Result<Guid, Exception>>
{
    private readonly ICharacterRepository _characterRepository;
    private readonly ILogger _logger;
    private readonly ITransactionFactory<IAsyncTransaction> _transactionFactory;

    public CreateFactCommandHandler(
        ICharacterRepository characterRepository,
        ILogger logger,
        ITransactionFactory<IAsyncTransaction> transactionFactory)
    {
        _characterRepository = characterRepository;
        _logger = logger;
        _transactionFactory = transactionFactory;
    }

    public async ValueTask<Result<Guid, Exception>> InvokeAsync(CreateFactCommand request,
        CancellationToken cancellationToken = new CancellationToken())
    {
        await using var transaction = await _transactionFactory.CreateAsync();
        var id = Guid.CreateVersion7();

        try
        {
            
        }
        catch(Exception exception)
        {
            await transaction.RollbackAsync();
            _logger.Error(exception, "Error creating fact: {Title}", request.CreateFact.Title);

            return exception;
        }

        return id;
    }
}
