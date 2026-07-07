using MessagePipe;

using LoreWeave.Application.Models;
using LoreWeave.Domain.Entities.Facts.Commands;
using LoreWeave.Domain.Exceptions;
using LoreWeave.Domain.Exceptions.Enums;
using LoreWeave.Domain.Repositories.Facts;
using LoreWeave.Domain.Transactions;

using ILogger = Serilog.ILogger;

namespace LoreWeave.Application.Commands.Facts.CommandHandlers;

public class UpdateFactCommandHandler : IAsyncRequestHandler<UpdateFactCommand, Result<Exception>>
{
    private readonly IExistsFact _existsFact;
    private readonly IFactWriter _factWriter;
    private readonly ILogger _logger;
    private readonly ITransactionFactory _transactionFactory;

    public UpdateFactCommandHandler(
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
        UpdateFactCommand request,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = await _transactionFactory.CreateAsync();

        try
        {
            var exists = await _existsFact.FactExistsAsync(transaction, request.BoardId, request.Id);

            if (!exists.Exists)
            {
                _logger.Error("Update fact fails for not existing fact: {Id}", request.Id);
                return new NotFoundException(Entities.Fact);
            }

            if (exists.Version != request.Version)
            {
                _logger.Error("Update fact fails for optimistic concurrency failure: {Id}", request.Id);
                return new PreconditionException();
            }

            var updateFact = new UpdateFact(request.Id, request.Title, request.Content, request.Version);

            await _factWriter.UpdateAsync(transaction, updateFact);
            await transaction.CommitAsync();
            _logger.Information("Fact updated: {Id}", request.Id);
        }
        catch (Exception exception)
        {
            await transaction.RollbackAsync();
            _logger.Error(exception, "Error updating fact: {Id}", request.Id);
            return exception;
        }

        return new Result<Exception>();
    }
}
