using MessagePipe;

using LoreWeave.Application.Models;
using LoreWeave.Domain.Entities.Characters.Queries;
using LoreWeave.Domain.Models;
using LoreWeave.Domain.Repositories.Characters;
using LoreWeave.Domain.Transactions;

using ILogger = Serilog.ILogger;

namespace LoreWeave.Application.Queries.Characters.QueryHandlers;

public class GetCharacterPageQueryHandler
    : IAsyncRequestHandler<GetCharacterPageQuery, Result<IReadOnlyCollection<CharacterPayloadWithRelations>, Exception>>
{
    private readonly ICharacterReader _characterReader;
    private readonly ILogger _logger;
    private readonly ITransactionFactory _transactionFactory;

    public GetCharacterPageQueryHandler(
        ITransactionFactory transactionFactory,
        ICharacterReader characterReader,
        ILogger logger)
    {
        _transactionFactory = transactionFactory;
        _characterReader = characterReader;
        _logger = logger;
    }

    public async ValueTask<Result<IReadOnlyCollection<CharacterPayloadWithRelations>, Exception>> InvokeAsync(
        GetCharacterPageQuery request,
        CancellationToken cancellationToken = new())
    {
        await using var transaction = await _transactionFactory.CreateAsync();

        try
        {
            var character = await _characterReader.GetPageAsync(
                transaction,
                new GetCharacterPage(
                    request.Number,
                    request.Size,
                    request.SortType,
                    request.SortOrder),
                new CharacterSearchFilter(
                    request.CharacterName));

            _logger.Information(
                "Character page found: {Number} - {Size}",
                request.Number,
                request.Size);

            return character
                .Select(x => new CharacterPayloadWithRelations(
                    x.Id,
                    x.Name,
                    x.KnowRelations
                        .Select(relation => new KnowCharacterRelationPayload(
                            relation.CharacterId,
                            relation.Description,
                            relation.IsStrongRelation))
                        .ToList()
                        .AsReadOnly()))
                .ToList()
                .AsReadOnly();
        }
        catch (Exception exception)
        {
            _logger.Error(
                exception,
                "Error getting character page: {Number} - {Size}",
                request.Number,
                request.Size);
            return exception;
        }
    }
}
