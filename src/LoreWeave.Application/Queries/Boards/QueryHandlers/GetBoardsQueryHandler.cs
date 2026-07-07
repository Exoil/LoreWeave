using MessagePipe;

using LoreWeave.Application.Models;
using LoreWeave.Domain.Repositories.Boards;
using LoreWeave.Domain.Transactions;

using ILogger = Serilog.ILogger;

namespace LoreWeave.Application.Queries.Boards.QueryHandlers;

public class GetBoardsQueryHandler
    : IAsyncRequestHandler<GetBoardsQuery, Result<IReadOnlyCollection<BoardPayload>, Exception>>
{
    private readonly IBoardReader _boardReader;
    private readonly ILogger _logger;
    private readonly ITransactionFactory _transactionFactory;

    public GetBoardsQueryHandler(
        ITransactionFactory transactionFactory,
        IBoardReader boardReader,
        ILogger logger)
    {
        _transactionFactory = transactionFactory;
        _boardReader = boardReader;
        _logger = logger;
    }

    public async ValueTask<Result<IReadOnlyCollection<BoardPayload>, Exception>> InvokeAsync(
        GetBoardsQuery request,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = await _transactionFactory.CreateAsync();

        try
        {
            var boards = await _boardReader.GetAllAsync(transaction);

            _logger.Information("Boards found: {Count}", boards.Count);

            return boards
                .Select(board => board.ToBoardPayload())
                .ToList()
                .AsReadOnly();
        }
        catch (Exception exception)
        {
            _logger.Error(exception, "Error getting boards");
            return exception;
        }
    }
}
