using MessagePipe;

using Neo4j.Driver;

using LoreWeave.Application.Models;
using LoreWeave.Domain.Exceptions;
using LoreWeave.Domain.Exceptions.Enums;
using LoreWeave.Domain.Factories;
using LoreWeave.Domain.Repositories;

using ILogger = Serilog.ILogger;

namespace LoreWeave.Application.Commands.CommandHandlers;

public class ConnectFactToCharacterCommandHandler
    : IAsyncRequestHandler<ConnectFactToCharacterCommand, Result<Exception>>
{
    private readonly IExistsCharacter _existsCharacter;
    private readonly IFactRepository _factRepository;
    private readonly ILogger _logger;
    private readonly ITransactionFactory<IAsyncTransaction> _transactionFactory;

    public ConnectFactToCharacterCommandHandler(
        ITransactionFactory<IAsyncTransaction> transactionFactory,
        ICharacterRepository characterRepository,
        ILogger logger)
    {
        _transactionFactory = transactionFactory;
        _existsCharacter = characterRepository;
        _factRepository = characterRepository;
        _logger = logger;
    }

    public async ValueTask<Result<Exception>> InvokeAsync(
        ConnectFactToCharacterCommand request,
        CancellationToken cancellationToken = new())
    {
        await using var transaction = await _transactionFactory.CreateAsync();

        try
        {
            var characterExists = await _existsCharacter.CharacterExistsAsync(transaction, request.CharacterId);

            if (!characterExists.Exists)
            {
                _logger.Error(
                    "Connect fact fails for not existing character: {CharacterId}",
                    request.CharacterId);
                return new NotFoundException(Entities.Character);
            }

            var factExists = await _factRepository.FactExistsAsync(transaction, request.FactId);

            if (!factExists.Exists)
            {
                _logger.Error("Connect fact fails for not existing fact: {FactId}", request.FactId);
                return new NotFoundException(Entities.Fact);
            }

            await _factRepository.ConnectFactToCharacterAsync(transaction, request.CharacterId, request.FactId);
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