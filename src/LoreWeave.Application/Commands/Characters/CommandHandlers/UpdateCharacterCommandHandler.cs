using MessagePipe;

using LoreWeave.Application.Models;
using LoreWeave.Domain.Entities.Characters.Commands;
using LoreWeave.Domain.Exceptions;
using LoreWeave.Domain.Exceptions.Enums;
using LoreWeave.Domain.Repositories.Characters;
using LoreWeave.Domain.Transactions;

using ILogger = Serilog.ILogger;

namespace LoreWeave.Application.Commands.Characters.CommandHandlers;

public class UpdateCharacterCommandHandler : IAsyncRequestHandler<UpdateCharacterCommand, Result<Exception>>
{
    private readonly IExistsCharacter _existsCharacter;
    private readonly ICharacterWriter _characterWriter;
    private readonly ILogger _logger;
    private readonly ITransactionFactory _transactionFactory;

    public UpdateCharacterCommandHandler(
        ITransactionFactory transactionFactory,
        IExistsCharacter existsCharacter,
        ICharacterWriter characterWriter,
        ILogger logger)
    {
        _transactionFactory = transactionFactory;
        _existsCharacter = existsCharacter;
        _characterWriter = characterWriter;
        _logger = logger;
    }

    public async ValueTask<Result<Exception>> InvokeAsync(
        UpdateCharacterCommand request,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = await _transactionFactory.CreateAsync();
        var id = request.Id;

        try
        {
            var exists = await _existsCharacter.CharacterExistsAsync(transaction, id);

            if (!exists.Exists)
            {
                _logger.Error("Update character fails for not existing character: {Id}", request.Id);
                return new NotFoundException(Entities.Character);
            }

            if (exists.Version != request.Version)
            {
                _logger.Error("Update character fails for optimistic concurrency failure: {Id}", request.Id);
                return new PreconditionException();
            }

            var updateCharacter = new UpdateCharacter(request.Name);

            await _characterWriter.UpdateAsync(transaction, id, updateCharacter);
            await transaction.CommitAsync();
            _logger.Information("Character updated: {Id}", request.Id);
        }
        catch (Exception exception)
        {
            await transaction.RollbackAsync();
            _logger.Error(exception, "Error updating character: {Id}", request.Id);
            return exception;
        }

        return new Result<Exception>();
    }
}
