using MessagePipe;

using LoreWeave.Application.Models;
using LoreWeave.Domain.Exceptions;
using LoreWeave.Domain.Exceptions.Enums;
using LoreWeave.Domain.Repositories.Characters;
using LoreWeave.Domain.Repositories.Facts;
using LoreWeave.Domain.Transactions;

using ILogger = Serilog.ILogger;

namespace LoreWeave.Application.Commands.Facts.CommandHandlers;

public class ConnectFactToCharacterCommandHandler
    : IAsyncRequestHandler<ConnectFactToCharacterCommand, Result<Exception>>
{
    private readonly IExistsCharacter _existsCharacter;
    private readonly IExistsFact _existsFact;
    private readonly IFactConnection _factConnection;
    private readonly ILogger _logger;
    private readonly ITransactionFactory _transactionFactory;

    public ConnectFactToCharacterCommandHandler(
        ITransactionFactory transactionFactory,
        IExistsCharacter existsCharacter,
        IExistsFact existsFact,
        IFactConnection factConnection,
        ILogger logger)
    {
        _transactionFactory = transactionFactory;
        _existsCharacter = existsCharacter;
        _existsFact = existsFact;
        _factConnection = factConnection;
        _logger = logger;
    }

    public async ValueTask<Result<Exception>> InvokeAsync(
        ConnectFactToCharacterCommand request,
        CancellationToken cancellationToken = new())
    {
        await using var transaction = await _transactionFactory.CreateAsync();

        try
        {
            var characterExists = await _existsCharacter.CharacterExistsAsync(transaction, request.BoardId, request.CharacterId);

            if (!characterExists.Exists)
            {
                _logger.Error(
                    "Connect fact fails for not existing character: {CharacterId}",
                    request.CharacterId);
                return new NotFoundException(Entities.Character);
            }

            var factExists = await _existsFact.FactExistsAsync(transaction, request.BoardId, request.FactId);

            if (!factExists.Exists)
            {
                _logger.Error("Connect fact fails for not existing fact: {FactId}", request.FactId);
                return new NotFoundException(Entities.Fact);
            }

            var connectionExists = await _factConnection.FactConnectionExistsAsync(
                transaction,
                request.CharacterId,
                request.FactId);

            if (connectionExists)
            {
                _logger.Error(
                    "Connect fact fails because character {CharacterId} is already connected to fact {FactId}",
                    request.CharacterId,
                    request.FactId);
                return new ConflictException("Character is already connected to the fact.");
            }

            await _factConnection.ConnectFactToCharacterAsync(transaction, request.CharacterId, request.FactId);
            await transaction.CommitAsync();
            _logger.Information(
                "Fact {FactId} connected to character {CharacterId}",
                request.FactId,
                request.CharacterId);
        }
        catch (Exception exception)
        {
            await transaction.RollbackAsync();
            _logger.Error(
                exception,
                "Error connecting fact {FactId} to character {CharacterId}",
                request.FactId,
                request.CharacterId);

            return exception;
        }

        return new Result<Exception>();
    }
}
