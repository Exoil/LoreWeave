using LoreWeave.Application.Models;
using LoreWeave.Domain.Exceptions;
using LoreWeave.Domain.Exceptions.Enums;
using LoreWeave.Domain.Factories;
using LoreWeave.Domain.Repositories;

using MessagePipe;

using Neo4j.Driver;

using Serilog;

namespace LoreWeave.Application.Commands.CommandHandlers;

public sealed class CreateFactCommandHandler : IAsyncRequestHandler<CreateFactCommand, Result<Guid, Exception>>
{
    private readonly IFactRepository _factRepository;
    private readonly IExistsCharacter _existsCharacter;
    private readonly ILogger _logger;
    private readonly ITransactionFactory<IAsyncTransaction> _transactionFactory;

    public CreateFactCommandHandler(
        ICharacterRepository characterRepository,
        ILogger logger,
        ITransactionFactory<IAsyncTransaction> transactionFactory)
    {
        _factRepository = characterRepository;
        _existsCharacter = characterRepository;
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
            var existCharacter = await _existsCharacter
                .CharacterExistsAsync(transaction, request.CharacterId);

            if (!existCharacter.Exists)
            {
                _logger.Information("Fact can't be created for not existing character.");
                return new NotFoundException(Entities.Character);
            }

            await _factRepository.CreateAsync(
                transaction,
                request.CharacterId,
                request.CreateFact);

            await transaction.CommitAsync();
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
