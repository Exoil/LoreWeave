using MessagePipe;

using LoreWeave.Application.Models;
using LoreWeave.Domain.Entities.Characters.Queries;
using LoreWeave.Domain.Exceptions;
using LoreWeave.Domain.Exceptions.Enums;
using LoreWeave.Domain.Models;
using LoreWeave.Domain.Repositories.Boards;
using LoreWeave.Domain.Repositories.Characters;
using LoreWeave.Domain.Transactions;

using ILogger = Serilog.ILogger;

namespace LoreWeave.Application.Queries.Characters.QueryHandlers;

public class GetCharacterPageQueryHandler
    : IAsyncRequestHandler<GetCharacterPageQuery, Result<IReadOnlyCollection<CharacterPayloadWithRelations>, Exception>>
{
    private readonly IExistsBoard _existsBoard;
    private readonly ICharacterReader _characterReader;
    private readonly ILogger _logger;
    private readonly ITransactionFactory _transactionFactory;

    public GetCharacterPageQueryHandler(
        ITransactionFactory transactionFactory,
        IExistsBoard existsBoard,
        ICharacterReader characterReader,
        ILogger logger)
    {
        _transactionFactory = transactionFactory;
        _existsBoard = existsBoard;
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
            var boardExists = await _existsBoard.BoardExistsAsync(transaction, request.BoardId);

            if (!boardExists.Exists)
            {
                _logger.Error("Get character page fails for not existing board: {BoardId}", request.BoardId);
                return new NotFoundException(Entities.Board);
            }

            var character = await _characterReader.GetPageAsync(
                transaction,
                request.BoardId,
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
                        .AsReadOnly(),
                    x.Facts
                        .Select(fact => new CharacterFactPayload(
                            fact.Id,
                            fact.Title,
                            fact.Content))
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
