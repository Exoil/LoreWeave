using MessagePipe;

using LoreWeave.Application.Models;
using LoreWeave.Domain.Entities.Characters.Commands;
using LoreWeave.Domain.Exceptions;
using LoreWeave.Domain.Exceptions.Enums;
using LoreWeave.Domain.Repositories.Boards;
using LoreWeave.Domain.Repositories.Characters;
using LoreWeave.Domain.Transactions;

using ILogger = Serilog.ILogger;

namespace LoreWeave.Application.Commands.Characters.CommandHandlers;

public class CreateCharacterCommandHandler : IAsyncRequestHandler<CreateCharacterCommand, Result<Guid, Exception>>
{
    private readonly IExistsBoard _existsBoard;
    private readonly ICharacterWriter _characterWriter;
    private readonly ILogger _logger;
    private readonly ITransactionFactory _transactionFactory;

    public CreateCharacterCommandHandler(
        ITransactionFactory transactionFactory,
        IExistsBoard existsBoard,
        ICharacterWriter characterWriter,
        ILogger logger)
    {
        _transactionFactory = transactionFactory;
        _existsBoard = existsBoard;
        _characterWriter = characterWriter;
        _logger = logger;
    }

    public async ValueTask<Result<Guid, Exception>> InvokeAsync(
        CreateCharacterCommand request,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = await _transactionFactory.CreateAsync();

        try
        {
            var boardExists = await _existsBoard.BoardExistsAsync(transaction, request.BoardId);

            if (!boardExists.Exists)
            {
                _logger.Error("Create character fails for not existing board: {BoardId}", request.BoardId);
                return new NotFoundException(Entities.Board);
            }

            var createCharacter = new CreateCharacter(request.Id, request.Name);
            await _characterWriter.CreateAsync(transaction, request.BoardId, createCharacter);
            await transaction.CommitAsync();
            _logger.Information("Character created: {Name}", request.Name);
        }
        catch (Exception exception)
        {
            await transaction.RollbackAsync();
            _logger.Error(exception, "Error creating character: {Name}", request.Name);

            return exception;
        }

        return request.Id;
    }
}
