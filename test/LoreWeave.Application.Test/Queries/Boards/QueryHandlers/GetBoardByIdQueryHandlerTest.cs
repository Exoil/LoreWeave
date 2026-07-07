using NSubstitute;
using NSubstitute.ExceptionExtensions;

using LoreWeave.Application.Models;
using LoreWeave.Application.Queries.Boards;
using LoreWeave.Application.Queries.Boards.QueryHandlers;
using LoreWeave.Domain.Entities.Boards;
using LoreWeave.Domain.Entities.Boards.Commands;
using LoreWeave.Domain.Exceptions;
using LoreWeave.Domain.Models;
using LoreWeave.Domain.Repositories.Boards;
using LoreWeave.Domain.Transactions;

using Serilog;

using Shouldly;

namespace LoreWeave.Application.Test.Queries.Boards.QueryHandlers;

public class GetBoardByIdQueryHandlerTest
{
    private readonly IExistsBoard _existsBoard;
    private readonly IBoardReader _boardReader;
    private readonly GetBoardByIdQueryHandler _sut;

    private static readonly Guid BoardId = Guid.NewGuid();

    public GetBoardByIdQueryHandlerTest()
    {
        _existsBoard = Substitute.For<IExistsBoard>();
        _boardReader = Substitute.For<IBoardReader>();
        var logger = Substitute.For<ILogger>();
        var transaction = Substitute.For<ITransaction>();
        var transactionFactory = Substitute.For<ITransactionFactory>();
        transactionFactory.CreateAsync().Returns(transaction);

        _sut = new GetBoardByIdQueryHandler(transactionFactory, _existsBoard, _boardReader, logger);
    }

    [Fact]
    [Trait(Constants.TraitName, Constants.TestTitle)]
    public async Task InvokeAsync_WhenBoardExists_ReturnsPayloadWithConfiguration()
    {
        // Arrange
        const ushort version = 3;
        var query = new GetBoardByIdQuery(BoardId);
        var board = new Board(new CreateBoard(BoardId, "Curse of Strahd"), BoardConfiguration.Default, version);

        _existsBoard
            .BoardExistsAsync(Arg.Any<ITransaction>(), BoardId)
            .Returns(new EntityExistence(true, version));
        _boardReader
            .GetAsync(Arg.Any<ITransaction>(), BoardId)
            .Returns(board);

        // Act
        var result = await _sut.InvokeAsync(query);

        // Assert
        result.IsSuccess.ShouldBeTrue("Query should succeed when the board exists");
        result.Value.Id.ShouldBe(BoardId, "Id should be mapped from the board");
        result.Value.Name.ShouldBe("Curse of Strahd", "Name should be mapped from the board");
        result.Value.Version.ShouldBe(version, "Version should be mapped from the board");
        result.Value.Etag.ShouldBe($"\"{version}\"", "Etag should be the quoted version");
        result.Value.Configuration.CharacterNodeColor.ShouldBe(
            BoardConfiguration.Default.CharacterNodeColor,
            "Configuration should be mapped from the board");
    }

    [Fact]
    [Trait(Constants.TraitName, Constants.TestTitle)]
    public async Task InvokeAsync_WhenBoardDoesNotExist_ReturnsNotFoundException()
    {
        // Arrange
        var query = new GetBoardByIdQuery(BoardId);
        _existsBoard
            .BoardExistsAsync(Arg.Any<ITransaction>(), BoardId)
            .Returns(new EntityExistence(false, 0));

        // Act
        var result = await _sut.InvokeAsync(query);

        // Assert
        result.IsSuccess.ShouldBeFalse("Query should fail when the board does not exist");
        result.Error.ShouldBeOfType<NotFoundException>("Error should be NotFoundException");
    }

    [Fact]
    [Trait(Constants.TraitName, Constants.TestTitle)]
    public async Task InvokeAsync_WhenRepositoryThrows_ReturnsException()
    {
        // Arrange
        var query = new GetBoardByIdQuery(BoardId);
        var expectedException = new Exception("DB error");
        _existsBoard
            .BoardExistsAsync(Arg.Any<ITransaction>(), BoardId)
            .Returns(new EntityExistence(true, 1));
        _boardReader
            .GetAsync(Arg.Any<ITransaction>(), BoardId)
            .ThrowsAsync(expectedException);

        // Act
        var result = await _sut.InvokeAsync(query);

        // Assert
        result.IsSuccess.ShouldBeFalse("Result should be failure when repository throws");
        result.Error.ShouldBe(expectedException, "Error should be the thrown exception");
    }
}
