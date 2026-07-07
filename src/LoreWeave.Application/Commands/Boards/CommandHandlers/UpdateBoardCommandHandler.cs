using MessagePipe;

using LoreWeave.Application.Models;
using LoreWeave.Domain.Entities.Boards.Commands;
using LoreWeave.Domain.Exceptions;
using LoreWeave.Domain.Exceptions.Enums;
using LoreWeave.Domain.Repositories.Boards;
using LoreWeave.Domain.Transactions;

using ILogger = Serilog.ILogger;

namespace LoreWeave.Application.Commands.Boards.CommandHandlers;

public class UpdateBoardCommandHandler : IAsyncRequestHandler<UpdateBoardCommand, Result<Exception>>
{
    private readonly IExistsBoard _existsBoard;
    private readonly IBoardWriter _boardWriter;
    private readonly ILogger _logger;
    private readonly ITransactionFactory _transactionFactory;

    public UpdateBoardCommandHandler(
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
        UpdateBoardCommand request,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = await _transactionFactory.CreateAsync();
        var id = request.Id;

        try
        {
            var exists = await _existsBoard.BoardExistsAsync(transaction, id);

            if (!exists.Exists)
            {
                _logger.Error("Update board fails for not existing board: {Id}", request.Id);
                return new NotFoundException(Entities.Board);
            }

            if (exists.Version != request.Version)
            {
                _logger.Error("Update board fails for optimistic concurrency failure: {Id}", request.Id);
                return new PreconditionException();
            }

            var updateBoard = new UpdateBoard(request.Name, request.Configuration.ToBoardConfiguration());

            await _boardWriter.UpdateAsync(transaction, id, updateBoard);
            await transaction.CommitAsync();
            _logger.Information("Board updated: {Id}", request.Id);
        }
        catch (Exception exception)
        {
            await transaction.RollbackAsync();
            _logger.Error(exception, "Error updating board: {Id}", request.Id);
            return exception;
        }

        return new Result<Exception>();
    }
}
