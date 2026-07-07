using MessagePipe;

using LoreWeave.Application.Models;
using LoreWeave.Domain.Exceptions;
using LoreWeave.Domain.Exceptions.Enums;
using LoreWeave.Domain.Repositories.Knows;
using LoreWeave.Domain.Transactions;

using ILogger = Serilog.ILogger;

namespace LoreWeave.Application.Queries.Knows.QueryHandlers;

public class GetKnowRelationQueryHandler
    : IAsyncRequestHandler<GetKnowRelationQuery, Result<KnowRelationPayload, Exception>>
{
    private readonly IExistsKnowRelation _existsKnowRelation;
    private readonly IKnowRelationReader _knowRelationReader;
    private readonly ILogger _logger;
    private readonly ITransactionFactory _transactionFactory;

    public GetKnowRelationQueryHandler(
        ITransactionFactory transactionFactory,
        IExistsKnowRelation existsKnowRelation,
        IKnowRelationReader knowRelationReader,
        ILogger logger)
    {
        _transactionFactory = transactionFactory;
        _existsKnowRelation = existsKnowRelation;
        _knowRelationReader = knowRelationReader;
        _logger = logger;
    }

    public async ValueTask<Result<KnowRelationPayload, Exception>> InvokeAsync(
        GetKnowRelationQuery request,
        CancellationToken cancellationToken = new())
    {
        await using var transaction = await _transactionFactory.CreateAsync();

        try
        {
            var fromCharacterId = request.FromCharacterId;
            var toCharacterId = request.ToCharacterId;

            var exists = await _existsKnowRelation.KnowRelationExistsAsync(
                transaction,
                request.BoardId,
                fromCharacterId,
                toCharacterId);

            if (!exists.Exists)
            {
                _logger.Error(
                    "Get know relation fails for not existing relation: {FromCharacterId} knows {ToCharacterId}",
                    request.FromCharacterId,
                    request.ToCharacterId);
                return new NotFoundException(Entities.KnowRelation);
            }

            var knowRelation = await _knowRelationReader.GetKnowRelationAsync(
                transaction,
                request.BoardId,
                fromCharacterId,
                toCharacterId);

            _logger.Information(
                "Know relation found: {FromCharacterId} knows {ToCharacterId}",
                request.FromCharacterId,
                request.ToCharacterId);

            return new KnowRelationPayload(
                knowRelation.FromCharacterId,
                knowRelation.ToCharacterId,
                knowRelation.Description,
                knowRelation.IsStrongRelation,
                knowRelation.Version);
        }
        catch (Exception exception)
        {
            _logger.Error(
                exception,
                "Error getting know relation: {FromCharacterId} knows {ToCharacterId}",
                request.FromCharacterId,
                request.ToCharacterId);
            return exception;
        }
    }
}
