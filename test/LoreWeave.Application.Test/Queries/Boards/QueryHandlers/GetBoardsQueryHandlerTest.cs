using NSubstitute;
using NSubstitute.ExceptionExtensions;

using LoreWeave.Application.Models;
using LoreWeave.Application.Queries.Boards;
using LoreWeave.Application.Queries.Boards.QueryHandlers;
using LoreWeave.Domain.Entities.Boards;
using LoreWeave.Domain.Entities.Boards.Commands;
using LoreWeave.Domain.Repositories.Boards;
using LoreWeave.Domain.Transactions;

using Serilog;

using Shouldly;

namespace LoreWeave.Application.Test.Queries.Boards.QueryHandlers;

public class GetBoardsQueryHandlerTest
{
    private readonly IBoardReader _boardReader;
    private readonly GetBoardsQueryHandler _sut;

    public GetBoardsQueryHandlerTest()
    {
        _boardReader = Substitute.For<IBoardReader>();
        var logger = Substitute.For<ILogger>();
        var transaction = Substitute.For<ITransaction>();
        var transactionFactory = Substitute.For<ITransactionFactory>();
        transactionFactory.CreateAsync().Returns(transaction);

        _sut = new GetBoardsQueryHandler(transactionFactory, _boardReader, logger);
    }

    [Fact]
    [Trait(Constants.TraitName, Constants.TestTitle)]
    public async Task InvokeAsync_WhenBoardsExist_ReturnsAllBoards()
    {
        // Arrange
        var boards = new List<Board>
        {
            new(new CreateBoard(Guid.CreateVersion7(), "Curse of Strahd"), BoardConfiguration.Default, 1),
            new(new CreateBoard(Guid.CreateVersion7(), "Waterdeep"), BoardConfiguration.Default, 2)
        }.AsReadOnly();

        _boardReader
            .GetAllAsync(Arg.Any<ITransaction>())
            .Returns(boards);

        // Act
        var result = await _sut.InvokeAsync(new GetBoardsQuery());

        // Assert
        result.IsSuccess.ShouldBeTrue("Query should succeed when boards are found");
        result.Value.ShouldBeAssignableTo<IReadOnlyCollection<BoardPayload>>("Result should be a read-only collection");
        result.Value.Count.ShouldBe(2, "Result should contain 2 boards");
        result.Value.First().Name.ShouldBe("Curse of Strahd", "First board name should match");
        result.Value.Last().Name.ShouldBe("Waterdeep", "Second board name should match");
    }

    [Fact]
    [Trait(Constants.TraitName, Constants.TestTitle)]
    public async Task InvokeAsync_WhenNoBoardsExist_ReturnsEmptyCollection()
    {
        // Arrange
        _boardReader
            .GetAllAsync(Arg.Any<ITransaction>())
            .Returns(new List<Board>().AsReadOnly());

        // Act
        var result = await _sut.InvokeAsync(new GetBoardsQuery());

        // Assert
        result.IsSuccess.ShouldBeTrue("Query should succeed even when no boards are found");
        result.Value.ShouldBeEmpty("Result should be empty when no boards exist");
    }

    [Fact]
    [Trait(Constants.TraitName, Constants.TestTitle)]
    public async Task InvokeAsync_WhenRepositoryThrows_ReturnsException()
    {
        // Arrange
        var expectedException = new Exception("DB error");
        _boardReader
            .GetAllAsync(Arg.Any<ITransaction>())
            .ThrowsAsync(expectedException);

        // Act
        var result = await _sut.InvokeAsync(new GetBoardsQuery());

        // Assert
        result.IsSuccess.ShouldBeFalse("Result should be failure when repository throws");
        result.Error.ShouldBe(expectedException, "Error should be the thrown exception");
    }
}
