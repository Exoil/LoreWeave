using NSubstitute;
using NSubstitute.ExceptionExtensions;

using LoreWeave.Application.Commands.Boards;
using LoreWeave.Application.Commands.Boards.CommandHandlers;
using LoreWeave.Application.Models;
using LoreWeave.Domain.Entities.Boards.Commands;
using LoreWeave.Domain.Exceptions;
using LoreWeave.Domain.Models;
using LoreWeave.Domain.Repositories.Boards;
using LoreWeave.Domain.Transactions;

using Serilog;

using Shouldly;

namespace LoreWeave.Application.Test.Commands.Boards.CommandHandlers;

public class UpdateBoardCommandHandlerTest
{
    private readonly IExistsBoard _existsBoard;
    private readonly IBoardWriter _boardWriter;
    private readonly ITransaction _transaction;
    private readonly UpdateBoardCommandHandler _sut;

    private static readonly Guid BoardId = Guid.NewGuid();
    private const int CurrentVersion = 1;

    private static readonly BoardConfigurationPayload Configuration = new(
        "#166534",
        "#9f1239",
        "#64748b",
        "#f59e0b",
        "#7c3aed",
        20,
        4,
        false,
        false,
        true);

    public UpdateBoardCommandHandlerTest()
    {
        _existsBoard = Substitute.For<IExistsBoard>();
        _boardWriter = Substitute.For<IBoardWriter>();
        var logger = Substitute.For<ILogger>();
        _transaction = Substitute.For<ITransaction>();
        var transactionFactory = Substitute.For<ITransactionFactory>();
        transactionFactory.CreateAsync().Returns(_transaction);

        _sut = new UpdateBoardCommandHandler(transactionFactory, _existsBoard, _boardWriter, logger);
    }

    [Fact]
    [Trait(Constants.TraitName, Constants.TestTitle)]
    public async Task InvokeAsync_WhenBoardExistsAndVersionMatches_ReturnsSuccess()
    {
        // Arrange
        var command = new UpdateBoardCommand(BoardId, "UpdatedName", Configuration, CurrentVersion);
        _existsBoard
            .BoardExistsAsync(Arg.Any<ITransaction>(), Arg.Any<Guid>())
            .Returns(new EntityExistence(true, CurrentVersion));

        // Act
        var result = await _sut.InvokeAsync(command);

        // Assert
        result.IsSuccess.ShouldBeTrue("Update should succeed when board exists and version matches");
        await _boardWriter.Received(1).UpdateAsync(
            Arg.Any<ITransaction>(),
            BoardId,
            Arg.Is<UpdateBoard>(update =>
                update!.Name == command.Name &&
                update.Configuration.CharacterNodeColor == Configuration.CharacterNodeColor &&
                update.Configuration.NodeRadius == Configuration.NodeRadius));
        await _transaction.Received(1).CommitAsync();
    }

    [Fact]
    [Trait(Constants.TraitName, Constants.TestTitle)]
    public async Task InvokeAsync_WhenBoardDoesNotExist_ReturnsNotFoundException()
    {
        // Arrange
        var command = new UpdateBoardCommand(BoardId, "UpdatedName", Configuration, CurrentVersion);
        _existsBoard
            .BoardExistsAsync(Arg.Any<ITransaction>(), Arg.Any<Guid>())
            .Returns(new EntityExistence(false, 0));

        // Act
        var result = await _sut.InvokeAsync(command);

        // Assert
        result.IsSuccess.ShouldBeFalse("Update should fail when board does not exist");
        result.Error.ShouldBeOfType<NotFoundException>("Error should be NotFoundException");
    }

    [Fact]
    [Trait(Constants.TraitName, Constants.TestTitle)]
    public async Task InvokeAsync_WhenVersionDoesNotMatch_ReturnsPreconditionException()
    {
        // Arrange
        var command = new UpdateBoardCommand(BoardId, "UpdatedName", Configuration, CurrentVersion);
        _existsBoard
            .BoardExistsAsync(Arg.Any<ITransaction>(), Arg.Any<Guid>())
            .Returns(new EntityExistence(true, CurrentVersion + 1));

        // Act
        var result = await _sut.InvokeAsync(command);

        // Assert
        result.IsSuccess.ShouldBeFalse("Update should fail when version does not match");
        result.Error.ShouldBeOfType<PreconditionException>("Error should be PreconditionException");
    }

    [Fact]
    [Trait(Constants.TraitName, Constants.TestTitle)]
    public async Task InvokeAsync_WhenConfigurationIsInvalid_ReturnsValueObjectException()
    {
        // Arrange
        var invalidConfiguration = Configuration with { CharacterNodeColor = "not-a-colour" };
        var command = new UpdateBoardCommand(BoardId, "UpdatedName", invalidConfiguration, CurrentVersion);
        _existsBoard
            .BoardExistsAsync(Arg.Any<ITransaction>(), Arg.Any<Guid>())
            .Returns(new EntityExistence(true, CurrentVersion));

        // Act
        var result = await _sut.InvokeAsync(command);

        // Assert
        result.IsSuccess.ShouldBeFalse("Update should fail for an invalid hex colour");
        result.Error.ShouldBeOfType<ValueObjectException>("Error should be ValueObjectException");
        await _transaction.DidNotReceive().CommitAsync();
    }

    [Fact]
    [Trait(Constants.TraitName, Constants.TestTitle)]
    public async Task InvokeAsync_WhenRepositoryThrows_ReturnsExceptionAndRollsBack()
    {
        // Arrange
        var command = new UpdateBoardCommand(BoardId, "UpdatedName", Configuration, CurrentVersion);
        var expectedException = new Exception("DB error");
        _existsBoard
            .BoardExistsAsync(Arg.Any<ITransaction>(), Arg.Any<Guid>())
            .Returns(new EntityExistence(true, CurrentVersion));
        _boardWriter
            .UpdateAsync(Arg.Any<ITransaction>(), Arg.Any<Guid>(), Arg.Any<UpdateBoard>())
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
