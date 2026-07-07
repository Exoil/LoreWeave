using MessagePipe;

using LoreWeave.Application.Models;
using LoreWeave.Domain.Exceptions;
using LoreWeave.Domain.Exceptions.Enums;
using LoreWeave.Domain.Repositories.Boards;
using LoreWeave.Domain.Transactions;

using ILogger = Serilog.ILogger;

namespace LoreWeave.Application.Queries.Boards.QueryHandlers;

public class GetBoardByIdQueryHandler : IAsyncRequestHandler<GetBoardByIdQuery, Result<BoardPayload, Exception>>
{
    private readonly IExistsBoard _existsBoard;
    private readonly IBoardReader _boardReader;
    private readonly ILogger _logger;
    private readonly ITransactionFactory _transactionFactory;

    public GetBoardByIdQueryHandler(
        ITransactionFactory transactionFactory,
        IExistsBoard existsBoard,
        IBoardReader boardReader,
        ILogger logger)
    {
        _transactionFactory = transactionFactory;
        _existsBoard = existsBoard;
        _boardReader = boardReader;
        _logger = logger;
    }

    public async ValueTask<Result<BoardPayload, Exception>> InvokeAsync(
        GetBoardByIdQuery request,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = await _transactionFactory.CreateAsync();

        try
        {
            var exists = await _existsBoard.BoardExistsAsync(transaction, request.Id);

            if (!exists.Exists)
            {
                _logger.Error("Get board by id fails for not existing board: {Id}", request.Id);
                return new NotFoundException(Entities.Board);
            }

            var board = await _boardReader.GetAsync(transaction, request.Id);
            _logger.Information("Board found: {Id}", request.Id);

            return board.ToBoardPayload();
        }
        catch (Exception exception)
        {
            _logger.Error(exception, "Error getting board: {Id}", request.Id);
            return exception;
        }
    }
}
