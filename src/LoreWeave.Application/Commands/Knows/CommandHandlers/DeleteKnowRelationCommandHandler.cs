using MessagePipe;

using LoreWeave.Application.Models;
using LoreWeave.Domain.Entities.Knows.Commands;
using LoreWeave.Domain.Repositories.Knows;
using LoreWeave.Domain.Transactions;

using ILogger = Serilog.ILogger;

namespace LoreWeave.Application.Commands.Knows.CommandHandlers;

public class DeleteKnowRelationCommandHandler : IAsyncRequestHandler<DeleteKnowRelationCommand, Result<Exception>>
{
    private readonly IKnowRelationWriter _knowRelationWriter;
    private readonly ILogger _logger;
    private readonly ITransactionFactory _transactionFactory;

    public DeleteKnowRelationCommandHandler(
        ITransactionFactory transactionFactory,
        IKnowRelationWriter knowRelationWriter,
        ILogger logger)
    {
        _transactionFactory = transactionFactory;
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
            await _knowRelationWriter.DeleteKnowRelationAsync(
                transaction,
                new DeleteKnowRelation(
                    request.FromCharacterId,
                    request.ToCharacterId));
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
