using MessagePipe;

using LoreWeave.Application.Models;
using LoreWeave.Domain.Entities.Facts.Commands;
using LoreWeave.Domain.Exceptions;
using LoreWeave.Domain.Exceptions.Enums;
using LoreWeave.Domain.Repositories.Facts;
using LoreWeave.Domain.Transactions;

using ILogger = Serilog.ILogger;

namespace LoreWeave.Application.Commands.Facts.CommandHandlers;

public class DeleteFactCommandHandler : IAsyncRequestHandler<DeleteFactCommand, Result<Exception>>
{
    private readonly IExistsFact _existsFact;
    private readonly IFactWriter _factWriter;
    private readonly ILogger _logger;
    private readonly ITransactionFactory _transactionFactory;

    public DeleteFactCommandHandler(
        ITransactionFactory transactionFactory,
        IExistsFact existsFact,
        IFactWriter factWriter,
        ILogger logger)
    {
        _transactionFactory = transactionFactory;
        _existsFact = existsFact;
        _factWriter = factWriter;
        _logger = logger;
    }

    public async ValueTask<Result<Exception>> InvokeAsync(
        DeleteFactCommand request,
        CancellationToken cancellationToken = new())
    {
        await using var transaction = await _transactionFactory.CreateAsync();

        try
        {
            var exists = await _existsFact.FactExistsAsync(transaction, request.BoardId, request.Id);

            if (!exists.Exists)
            {
                _logger.Error("Delete fact fails for not existing fact: {Id}", request.Id);
                return new NotFoundException(Entities.Fact);
            }

            var deleteFact = new DeleteFact(request.Id);
            await _factWriter.DeleteAsync(transaction, deleteFact);
            await transaction.CommitAsync();
            _logger.Information("Fact deleted: {Id}", request.Id);
        }
        catch (Exception exception)
        {
            await transaction.RollbackAsync();
            _logger.Error(exception, "Error deleting fact: {Id}", request.Id);

            return exception;
        }

        return new Result<Exception>();
    }
}
