using NSubstitute;
using NSubstitute.ExceptionExtensions;

using LoreWeave.Application.Commands.Boards;
using LoreWeave.Application.Commands.Boards.CommandHandlers;
using LoreWeave.Domain.Entities.Boards.Commands;
using LoreWeave.Domain.Exceptions;
using LoreWeave.Domain.Models;
using LoreWeave.Domain.Repositories.Boards;
using LoreWeave.Domain.Transactions;

using Serilog;

using Shouldly;

namespace LoreWeave.Application.Test.Commands.Boards.CommandHandlers;

public class DeleteBoardCommandHandlerTest
{
    private readonly IExistsBoard _existsBoard;
    private readonly IBoardWriter _boardWriter;
    private readonly ITransaction _transaction;
    private readonly DeleteBoardCommandHandler _sut;

    private static readonly Guid BoardId = Guid.NewGuid();

    public DeleteBoardCommandHandlerTest()
    {
        _existsBoard = Substitute.For<IExistsBoard>();
        _boardWriter = Substitute.For<IBoardWriter>();
        var logger = Substitute.For<ILogger>();
        _transaction = Substitute.For<ITransaction>();
        var transactionFactory = Substitute.For<ITransactionFactory>();
        transactionFactory.CreateAsync().Returns(_transaction);

        _sut = new DeleteBoardCommandHandler(transactionFactory, _existsBoard, _boardWriter, logger);
    }

    [Fact]
    [Trait(Constants.TraitName, Constants.TestTitle)]
    public async Task InvokeAsync_WhenBoardExists_ReturnsSuccess()
    {
        // Arrange
        var command = new DeleteBoardCommand(BoardId);
        _existsBoard
            .BoardExistsAsync(Arg.Any<ITransaction>(), Arg.Any<Guid>())
            .Returns(new EntityExistence(true, 1));

        // Act
        var result = await _sut.InvokeAsync(command);

        // Assert
        result.IsSuccess.ShouldBeTrue("Board deletion should succeed when the board exists");
        await _boardWriter.Received(1).DeleteAsync(
            Arg.Any<ITransaction>(),
            Arg.Is<DeleteBoard>(delete => delete.Id == BoardId));
        await _transaction.Received(1).CommitAsync();
    }

    [Fact]
    [Trait(Constants.TraitName, Constants.TestTitle)]
    public async Task InvokeAsync_WhenBoardDoesNotExist_ReturnsNotFoundException()
    {
        // Arrange
        var command = new DeleteBoardCommand(BoardId);
        _existsBoard
            .BoardExistsAsync(Arg.Any<ITransaction>(), Arg.Any<Guid>())
            .Returns(new EntityExistence(false, 0));

        // Act
        var result = await _sut.InvokeAsync(command);

        // Assert
        result.IsSuccess.ShouldBeFalse("Delete should fail when board does not exist");
        result.Error.ShouldBeOfType<NotFoundException>("Error should be NotFoundException");
        await _transaction.DidNotReceive().CommitAsync();
    }

    [Fact]
    [Trait(Constants.TraitName, Constants.TestTitle)]
    public async Task InvokeAsync_WhenRepositoryThrows_ReturnsExceptionAndRollsBack()
    {
        // Arrange
        var command = new DeleteBoardCommand(BoardId);
        var expectedException = new Exception("DB error");
        _existsBoard
            .BoardExistsAsync(Arg.Any<ITransaction>(), Arg.Any<Guid>())
            .Returns(new EntityExistence(true, 1));
        _boardWriter
            .DeleteAsync(Arg.Any<ITransaction>(), Arg.Any<DeleteBoard>())
            .ThrowsAsync(expectedException);

        // Act
        var result = await _sut.InvokeAsync(command);

        // Assert
        result.IsSuccess.ShouldBeFalse("Result should be failure when repository throws");
        result.Error.ShouldBe(expectedException, "Error should be the thrown exception");
        await _transaction.Received(1).RollbackAsync();
        await _transaction.DidNotReceive().CommitAsync();
    }
}
