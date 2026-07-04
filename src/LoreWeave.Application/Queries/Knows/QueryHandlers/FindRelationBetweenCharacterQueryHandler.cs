using MessagePipe;

using LoreWeave.Application.Models;
using LoreWeave.Domain.Exceptions;
using LoreWeave.Domain.Exceptions.Enums;
using LoreWeave.Domain.Repositories.Characters;
using LoreWeave.Domain.Repositories.Knows;
using LoreWeave.Domain.Transactions;

using ILogger = Serilog.ILogger;

namespace LoreWeave.Application.Queries.Knows.QueryHandlers;

public class FindRelationBetweenCharacterQueryHandler
    : IAsyncRequestHandler<FindRelationBetweenCharacterQuery, Result<RelationPathPayload, Exception>>
{
    private readonly IExistsCharacter _existsCharacter;
    private readonly IKnowRelationReader _knowRelationReader;
    private readonly ILogger _logger;
    private readonly ITransactionFactory _transactionFactory;

    public FindRelationBetweenCharacterQueryHandler(
        ITransactionFactory transactionFactory,
        IExistsCharacter existsCharacter,
        IKnowRelationReader knowRelationReader,
        ILogger logger)
    {
        _transactionFactory = transactionFactory;
        _existsCharacter = existsCharacter;
        _knowRelationReader = knowRelationReader;
        _logger = logger;
    }

    public async ValueTask<Result<RelationPathPayload, Exception>> InvokeAsync(
        FindRelationBetweenCharacterQuery request,
        CancellationToken cancellationToken = new CancellationToken())
    {
        await using var transaction = await _transactionFactory.CreateAsync();

        try
        {
            var fromCharacterId = request.FromCharacterId;
            var toCharacterId = request.ToCharacterId;

            var fromCharacterExists = await _existsCharacter
                .CharacterExistsAsync(transaction, fromCharacterId);

            var toCharacterExists = await _existsCharacter
                .CharacterExistsAsync(transaction, toCharacterId);

            if (!fromCharacterExists.Exists)
            {
                _logger.Error("From character not found: {Id}", request.FromCharacterId);
                return new NotFoundException(Entities.Character);
            }

            if (!toCharacterExists.Exists)
            {
                _logger.Error("To character not found: {Id}", request.ToCharacterId);
                return new NotFoundException(Entities.Character);
            }

            var path = await _knowRelationReader.FindPathBetweenCharactersAsync(
                transaction,
                fromCharacterId,
                toCharacterId,
                request.MaxHops);

            if (path.Count == 0)
            {
                _logger.Information(
                    "No relation path found between {FromId} and {ToId}",
                    request.FromCharacterId,
                    request.ToCharacterId);

                return new RelationPathPayload([], 0);
            }

            var characterIds = path;

            _logger.Information(
                "Found path with {Hops} hops between {FromId} and {ToId}",
                characterIds.Count - 1,
                request.FromCharacterId,
                request.ToCharacterId);

            return new RelationPathPayload(characterIds, characterIds.Count - 1);
        }
        catch (Exception exception)
        {
            _logger.Error(
                exception,
                "Error finding relation between {FromId} and {ToId}",
                request.FromCharacterId,
                request.ToCharacterId);

            return exception;
        }
    }
}
