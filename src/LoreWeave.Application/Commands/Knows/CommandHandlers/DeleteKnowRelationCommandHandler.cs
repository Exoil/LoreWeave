using MessagePipe;

using LoreWeave.Application.Models;
using LoreWeave.Domain.Entities.Knows.Commands;
using LoreWeave.Domain.Exceptions;
using LoreWeave.Domain.Exceptions.Enums;
using LoreWeave.Domain.Repositories.Knows;
using LoreWeave.Domain.Transactions;

using ILogger = Serilog.ILogger;

namespace LoreWeave.Application.Commands.Knows.CommandHandlers;

public class DeleteKnowRelationCommandHandler : IAsyncRequestHandler<DeleteKnowRelationCommand, Result<Exception>>
{
    private readonly IExistsKnowRelation _existsKnowRelation;
    private readonly IKnowRelationWriter _knowRelationWriter;
    private readonly ILogger _logger;
    private readonly ITransactionFactory _transactionFactory;

    public DeleteKnowRelationCommandHandler(
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
        DeleteKnowRelationCommand request,
        CancellationToken cancellationToken = new())
    {
        await using var transaction = await _transactionFactory.CreateAsync();

        try
        {
            // Validates first (from != to) so invalid input stays 400, not 404.
            var deleteKnowRelation = new DeleteKnowRelation(
                request.FromCharacterId,
                request.ToCharacterId);

            var exists = await _existsKnowRelation.KnowRelationExistsAsync(
                transaction,
                request.BoardId,
                request.FromCharacterId,
                request.ToCharacterId);

            if (!exists.Exists)
            {
                _logger.Error(
                    "Delete know relation fails for not existing relation: {FromCharacterId} knows {ToCharacterId}",
                    request.FromCharacterId,
                    request.ToCharacterId);
                return new NotFoundException(Entities.KnowRelation);
            }

            await _knowRelationWriter.DeleteKnowRelationAsync(transaction, deleteKnowRelation);
            await transaction.CommitAsync();
            _logger.Information(
                "Know relation deleted: {FromCharacterId} knows {ToCharacterId}",
                request.FromCharacterId,
                request.ToCharacterId
            );
        }
        catch (Exception exception)
        {
            await transaction.RollbackAsync();
            _logger.Error(
                exception,
                "Error deleting know relation: {FromCharacterId} knows {ToCharacterId}",
                request.FromCharacterId,
                request.ToCharacterId);
            return exception;
        }

        return new Result<Exception>();
    }
}
