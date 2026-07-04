using MessagePipe;

using LoreWeave.Application.Models;
using LoreWeave.Domain.Exceptions;
using LoreWeave.Domain.Exceptions.Enums;
using LoreWeave.Domain.Repositories.Characters;
using LoreWeave.Domain.Transactions;

using ILogger = Serilog.ILogger;

namespace LoreWeave.Application.Queries.Characters.QueryHandlers;

public class
    GetCharacterByIdQueryHandler : IAsyncRequestHandler<GetCharacterByIdQuery, Result<CharacterPayload, Exception>>
{
    private readonly IExistsCharacter _existsCharacter;
    private readonly ICharacterReader _characterReader;
    private readonly ILogger _logger;
    private readonly ITransactionFactory _transactionFactory;

    public GetCharacterByIdQueryHandler(
        ITransactionFactory transactionFactory,
        IExistsCharacter existsCharacter,
        ICharacterReader characterReader,
        ILogger logger)
    {
        _transactionFactory = transactionFactory;
        _existsCharacter = existsCharacter;
        _characterReader = characterReader;
        _logger = logger;
    }

    public async ValueTask<Result<CharacterPayload, Exception>> InvokeAsync(
        GetCharacterByIdQuery request,
        CancellationToken cancellationToken = new())
    {
        await using var transaction = await _transactionFactory.CreateAsync();

        try
        {
            var id = request.Id;

            var exists = await _existsCharacter.CharacterExistsAsync(transaction, id);

            if (!exists.Exists)
            {
                _logger.Error("Get character by id fails for not existing character: {Id}", request.Id);
                return new NotFoundException(Entities.Character);
            }

            var character = await _characterReader.GetAsync(transaction, id);
            _logger.Information("Character found: {Id}", request.Id);

            return new CharacterPayload(character.Id, character.Name, character.Version);
        }
        catch (Exception exception)
        {
            _logger.Error(exception, "Error getting character: {Id}", request.Id);
            return exception;
        }
    }
}
