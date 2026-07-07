using MessagePipe;

using LoreWeave.Application.Models;
using LoreWeave.Domain.Entities.Boards.Commands;
using LoreWeave.Domain.Exceptions;
using LoreWeave.Domain.Exceptions.Enums;
using LoreWeave.Domain.Repositories.Boards;
using LoreWeave.Domain.Transactions;

using ILogger = Serilog.ILogger;

namespace LoreWeave.Application.Commands.Boards.CommandHandlers;

public class DeleteBoardCommandHandler : IAsyncRequestHandler<DeleteBoardCommand, Result<Exception>>
{
    private readonly IExistsBoard _existsBoard;
    private readonly IBoardWriter _boardWriter;
    private readonly ILogger _logger;
    private readonly ITransactionFactory _transactionFactory;

    public DeleteBoardCommandHandler(
        ITransactionFactory transactionFactory,
        IExistsBoard existsBoard,
        IBoardWriter boardWriter,
        ILogger logger)
    {
        _transactionFactory = transactionFactory;
        _existsBoard = existsBoard;
        _boardWriter = boardWriter;
        _logger = logger;
    }

    public async ValueTask<Result<Exception>> InvokeAsync(
        DeleteBoardCommand request,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = await _transactionFactory.CreateAsync();

        try
        {
            var exists = await _existsBoard.BoardExistsAsync(transaction, request.Id);

            if (!exists.Exists)
            {
                _logger.Error("Delete board fails for not existing board: {Id}", request.Id);
                return new NotFoundException(Entities.Board);
            }

            var deleteBoard = new DeleteBoard(request.Id);

            // Cascades: removes every character, KNOWS relation and fact
            // (including orphaned facts) belonging to the board.
            await _boardWriter.DeleteAsync(transaction, deleteBoard);
            await transaction.CommitAsync();
            _logger.Information("Board deleted: {Id}", request.Id);
        }
        catch (Exception exception)
        {
            await transaction.RollbackAsync();
            _logger.Error(exception, "Error deleting board: {Id}", request.Id);

            return exception;
        }

        return new Result<Exception>();
    }
}
