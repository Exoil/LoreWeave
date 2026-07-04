using MessagePipe;

using Neo4j.Driver;

using LoreWeave.Application.Models;
using LoreWeave.Domain.Entities.Facts.Commands;
using LoreWeave.Domain.Exceptions;
using LoreWeave.Domain.Exceptions.Enums;
using LoreWeave.Domain.Factories;
using LoreWeave.Domain.Repositories;

using ILogger = Serilog.ILogger;

namespace LoreWeave.Application.Commands.CommandHandlers;

public class DeleteFactCommandHandler : IAsyncRequestHandler<DeleteFactCommand, Result<Exception>>
{
    private readonly IFactRepository _factRepository;
    private readonly ILogger _logger;
    private readonly ITransactionFactory<IAsyncTransaction> _transactionFactory;

    public DeleteFactCommandHandler(
        ITransactionFactory<IAsyncTransaction> transactionFactory,
        ICharacterRepository characterRepository,
        ILogger logger)
    {
        _transactionFactory = transactionFactory;
        _factRepository = characterRepository;
        _logger = logger;
    }

    public async ValueTask<Result<Exception>> InvokeAsync(
        DeleteFactCommand request,
        CancellationToken cancellationToken = new())
    {
        await using var transaction = await _transactionFactory.CreateAsync();

        try
        {
            var exists = await _factRepository.FactExistsAsync(transaction, request.Id);

            if (!exists.Exists)
            {
                _logger.Error("Delete fact fails for not existing fact: {Id}", request.Id);
                return new NotFoundException(Entities.Fact);
            }

            var deleteFact = new DeleteFact(request.Id);
            await _factRepository.DeleteAsync(transaction, deleteFact);
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