using MessagePipe;

using LoreWeave.Application.Models;
using LoreWeave.Domain.Entities.Boards;
using LoreWeave.Domain.Entities.Boards.Commands;
using LoreWeave.Domain.Repositories.Boards;
using LoreWeave.Domain.Transactions;

using ILogger = Serilog.ILogger;

namespace LoreWeave.Application.Commands.Boards.CommandHandlers;

public class CreateBoardCommandHandler : IAsyncRequestHandler<CreateBoardCommand, Result<Guid, Exception>>
{
    private readonly IBoardWriter _boardWriter;
    private readonly ILogger _logger;
    private readonly ITransactionFactory _transactionFactory;

    public CreateBoardCommandHandler(
        ITransactionFactory transactionFactory,
        IBoardWriter boardWriter,
        ILogger logger)
    {
        _transactionFactory = transactionFactory;
        _boardWriter = boardWriter;
        _logger = logger;
    }

    public async ValueTask<Result<Guid, Exception>> InvokeAsync(
        CreateBoardCommand request,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = await _transactionFactory.CreateAsync();

        try
        {
            var createBoard = new CreateBoard(request.Id, request.Name);

            // The client only sends the name — the server assigns the default
            // configuration; customisation happens through updateBoard.
            await _boardWriter.CreateAsync(transaction, createBoard, BoardConfiguration.Default);
            await transaction.CommitAsync();
            _logger.Information("Board created: {Name}", request.Name);
        }
        catch (Exception exception)
        {
            await transaction.RollbackAsync();
            _logger.Error(exception, "Error creating board: {Name}", request.Name);

            return exception;
        }

        return request.Id;
    }
}
