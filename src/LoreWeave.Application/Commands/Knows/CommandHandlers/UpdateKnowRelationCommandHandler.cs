using MessagePipe;

using LoreWeave.Application.Models;
using LoreWeave.Domain.Entities.Knows.Commands;
using LoreWeave.Domain.Exceptions;
using LoreWeave.Domain.Exceptions.Enums;
using LoreWeave.Domain.Repositories.Knows;
using LoreWeave.Domain.Transactions;

using ILogger = Serilog.ILogger;

namespace LoreWeave.Application.Commands.Knows.CommandHandlers;

public class UpdateKnowRelationCommandHandler : IAsyncRequestHandler<UpdateKnowRelationCommand, Result<Exception>>
{
    private readonly IExistsKnowRelation _existsKnowRelation;
    private readonly IKnowRelationWriter _knowRelationWriter;
    private readonly ILogger _logger;
    private readonly ITransactionFactory _transactionFactory;

    public UpdateKnowRelationCommandHandler(
        ITransactionFactory transactionFactory,
        IExistsKnowRelation existsKnowRelation,
        IKnowRelationWriter knowRelationWriter,
        ILogger logger)
    {
        _transactionFactory = transactionFactory;
        _existsKnowRelation = existsKnowRelation;
        _knowRelationWriter = knowRelationWriter;
        _logger = logger;
    }

    public async ValueTask<Result<Exception>> InvokeAsync(
        UpdateKnowRelationCommand request,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = await _transactionFactory.CreateAsync();
        var fromCharacterId = request.FromCharacterId;
        var toCharacterId = request.ToCharacterId;

        try
        {
            var updateKnowRelation = new UpdateKnowRelation(
                fromCharacterId,
                toCharacterId,
                request.Description,
                request.IsStrongRelation);

            var exists = await _existsKnowRelation.KnowRelationExistsAsync(
                transaction,
                updateKnowRelation.FromCharacterId,
                updateKnowRelation.ToCharacterId);

            if (!exists.Exists)
            {
                _logger.Error("Update know relation fails for not existing relation: {FromCharacterId} knows {ToCharacterId}",
                    request.FromCharacterId,
                    request.ToCharacterId);
                return new NotFoundException(Entities.KnowRelation);
            }

            if (exists.Version != request.Version)
            {
                _logger.Error("Update know relation fails for optimistic concurrency failure: {FromCharacterId} knows {ToCharacterId}",
                    request.FromCharacterId,
                    request.ToCharacterId);
                return new PreconditionException();
            }

            await _knowRelationWriter.UpdateKnowRelationAsync(transaction, updateKnowRelation);
            await transaction.CommitAsync();
            _logger.Information("Know relation updated: {FromCharacterId} knows {ToCharacterId}",
                request.FromCharacterId,
                request.ToCharacterId);
        }
        catch (Exception exception)
        {
            await transaction.RollbackAsync();
            _logger.Error(exception, "Error updating know relation: {FromCharacterId} knows {ToCharacterId}",
                request.FromCharacterId,
                request.ToCharacterId);
            return exception;
        }

        return new Result<Exception>();
    }
}
