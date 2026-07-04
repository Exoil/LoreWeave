using MessagePipe;

using Neo4j.Driver;

using LoreWeave.Application.Models;
using LoreWeave.Domain.Exceptions;
using LoreWeave.Domain.Exceptions.Enums;
using LoreWeave.Domain.Factories;
using LoreWeave.Domain.Repositories;

using ILogger = Serilog.ILogger;

namespace LoreWeave.Application.Queries.QueryHandlers;

public class GetFactByIdQueryHandler : IAsyncRequestHandler<GetFactByIdQuery, Result<FactPayload, Exception>>
{
    private readonly IFactRepository _factRepository;
    private readonly ILogger _logger;
    private readonly ITransactionFactory<IAsyncTransaction> _transactionFactory;

    public GetFactByIdQueryHandler(
        ITransactionFactory<IAsyncTransaction> transactionFactory,
        ICharacterRepository characterRepository,
        ILogger logger)
    {
        _transactionFactory = transactionFactory;
        _factRepository = characterRepository;
        _logger = logger;
    }

    public async ValueTask<Result<FactPayload, Exception>> InvokeAsync(
        GetFactByIdQuery request,
        CancellationToken cancellationToken = new())
    {
        await using var transaction = await _transactionFactory.CreateAsync();

        try
        {
            var exists = await _factRepository.FactExistsAsync(transaction, request.Id);

            if (!exists.Exists)
            {
                _logger.Error("Get fact by id fails for not existing fact: {Id}", request.Id);
                return new NotFoundException(Entities.Fact);
            }

            var fact = await _factRepository.GetFactAsync(transaction, request.Id);
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