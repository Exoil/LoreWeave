using MessagePipe;

using LoreWeave.Application.Models;
using LoreWeave.Domain.Entities.Facts.Commands;
using LoreWeave.Domain.Exceptions;
using LoreWeave.Domain.Exceptions.Enums;
using LoreWeave.Domain.Repositories.Characters;
using LoreWeave.Domain.Repositories.Facts;
using LoreWeave.Domain.Transactions;

using ILogger = Serilog.ILogger;

namespace LoreWeave.Application.Commands.Facts.CommandHandlers;

public sealed class CreateFactCommandHandler : IAsyncRequestHandler<CreateFactCommand, Result<Guid, Exception>>
{
    private readonly IExistsCharacter _existsCharacter;
    private readonly IFactWriter _factWriter;
    private readonly ILogger _logger;
    private readonly ITransactionFactory _transactionFactory;

    public CreateFactCommandHandler(
        ITransactionFactory transactionFactory,
        IExistsCharacter existsCharacter,
        IFactWriter factWriter,
        ILogger logger)
    {
        _transactionFactory = transactionFactory;
        _existsCharacter = existsCharacter;
        _factWriter = factWriter;
        _logger = logger;
    }

    public async ValueTask<Result<Guid, Exception>> InvokeAsync(CreateFactCommand request,
        CancellationToken cancellationToken = new CancellationToken())
    {
        await using var transaction = await _transactionFactory.CreateAsync();

        var id = Guid.CreateVersion7();

        try
        {
            var createFact = new CreateFact(id, request.Title, request.Content);

            var existCharacter = await _existsCharacter
                .CharacterExistsAsync(transaction, request.BoardId, request.CharacterId);

            if (!existCharacter.Exists)
            {
                _logger.Information("Fact can't be created for not existing character.");
                return new NotFoundException(Entities.Character);
            }

            await _factWriter.CreateAsync(
                transaction,
                request.CharacterId,
                createFact);

            await transaction.CommitAsync();

            return id;
        }
        catch(Exception exception)
        {
            await transaction.RollbackAsync();
            _logger.Error(exception, "Error creating fact: {Title}", request.Title);

            return exception;
        }
    }
}
