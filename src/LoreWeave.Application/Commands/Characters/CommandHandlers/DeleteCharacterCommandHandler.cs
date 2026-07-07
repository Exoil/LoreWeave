using MessagePipe;

using LoreWeave.Application.Models;
using LoreWeave.Domain.Entities.Characters.Commands;
using LoreWeave.Domain.Exceptions;
using LoreWeave.Domain.Exceptions.Enums;
using LoreWeave.Domain.Repositories.Characters;
using LoreWeave.Domain.Transactions;

using ILogger = Serilog.ILogger;

namespace LoreWeave.Application.Commands.Characters.CommandHandlers;

public class DeleteCharacterCommandHandler : IAsyncRequestHandler<DeleteCharacterCommand, Result<Exception>>
{
    private readonly IExistsCharacter _existsCharacter;
    private readonly ICharacterWriter _characterWriter;
    private readonly ILogger _logger;
    private readonly ITransactionFactory _transactionFactory;

    public DeleteCharacterCommandHandler(
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
        DeleteCharacterCommand request,
        CancellationToken cancellationToken = new())
    {
        await using var transaction = await _transactionFactory.CreateAsync();

        try
        {
            var userId = request.Id;
            var exists = await _existsCharacter.CharacterExistsAsync(transaction, request.BoardId, userId);

            if (!exists.Exists)
            {
                _logger.Error("Delete character fails for not existing character: {Id}", request.Id);
                return new NotFoundException(Entities.Character);
            }

            var deleteCharacter = new DeleteCharacter(request.Id);
            await _characterWriter.DeleteAsync(transaction, deleteCharacter);
            await transaction.CommitAsync();
            _logger.Information("Character deleted: {Id}", request.Id);
        }
        catch (Exception exception)
        {
            await transaction.RollbackAsync();
            _logger.Error(exception, "Error deleting character: {Id}", request.Id);

            return exception;
        }

        return new Result<Exception>();
    }
}
