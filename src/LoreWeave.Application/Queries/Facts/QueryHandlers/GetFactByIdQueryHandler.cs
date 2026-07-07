using MessagePipe;

using LoreWeave.Application.Models;
using LoreWeave.Domain.Exceptions;
using LoreWeave.Domain.Exceptions.Enums;
using LoreWeave.Domain.Repositories.Facts;
using LoreWeave.Domain.Transactions;

using ILogger = Serilog.ILogger;

namespace LoreWeave.Application.Queries.Facts.QueryHandlers;

public class GetFactByIdQueryHandler : IAsyncRequestHandler<GetFactByIdQuery, Result<FactPayload, Exception>>
{
    private readonly IExistsFact _existsFact;
    private readonly IFactReader _factReader;
    private readonly ILogger _logger;
    private readonly ITransactionFactory _transactionFactory;

    public GetFactByIdQueryHandler(
        ITransactionFactory transactionFactory,
        IExistsFact existsFact,
        IFactReader factReader,
        ILogger logger)
    {
        _transactionFactory = transactionFactory;
        _existsFact = existsFact;
        _factReader = factReader;
        _logger = logger;
    }

    public async ValueTask<Result<FactPayload, Exception>> InvokeAsync(
        GetFactByIdQuery request,
        CancellationToken cancellationToken = new())
    {
        await using var transaction = await _transactionFactory.CreateAsync();

        try
        {
            var exists = await _existsFact.FactExistsAsync(transaction, request.BoardId, request.Id);

            if (!exists.Exists)
            {
                _logger.Error("Get fact by id fails for not existing fact: {Id}", request.Id);
                return new NotFoundException(Entities.Fact);
            }

            var fact = await _factReader.GetFactAsync(transaction, request.BoardId, request.Id);
            _logger.Information("Fact found: {Id}", request.Id);

            return new FactPayload(fact.Id, fact.Title, fact.Content, fact.Version);
        }
        catch (Exception exception)
        {
            _logger.Error(exception, "Error getting fact: {Id}", request.Id);
            return exception;
        }
    }
}
