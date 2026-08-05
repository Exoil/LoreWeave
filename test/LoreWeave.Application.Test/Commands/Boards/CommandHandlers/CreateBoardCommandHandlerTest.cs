using NSubstitute;
using NSubstitute.ExceptionExtensions;

using LoreWeave.Application.Commands.Boards;
using LoreWeave.Application.Commands.Boards.CommandHandlers;
using LoreWeave.Domain.Entities.Boards;
using LoreWeave.Domain.Entities.Boards.Commands;
using LoreWeave.Domain.Exceptions;
using LoreWeave.Domain.Repositories.Boards;
using LoreWeave.Domain.Transactions;

using Serilog;

using Shouldly;

namespace LoreWeave.Application.Test.Commands.Boards.CommandHandlers;

public class CreateBoardCommandHandlerTest
{
    private readonly IBoardWriter _boardWriter;
    private readonly ITransaction _transaction;
    private readonly CreateBoardCommandHandler _sut;

    public CreateBoardCommandHandlerTest()
    {
        _boardWriter = Substitute.For<IBoardWriter>();
        var logger = Substitute.For<ILogger>();
        _transaction = Substitute.For<ITransaction>();
        var transactionFactory = Substitute.For<ITransactionFactory>();
        transactionFactory.CreateAsync().Returns(_transaction);

        _sut = new CreateBoardCommandHandler(transactionFactory, _boardWriter, logger);
    }

    [Fact]
    [Trait(Constants.TraitName, Constants.TestTitle)]
    public async Task InvokeAsync_WhenBoardIsCreated_ReturnsGuid()
    {
        // Arrange
        var command = new CreateBoardCommand(Guid.CreateVersion7(), "Curse of Strahd");

        // Act
        var result = await _sut.InvokeAsync(command);

        // Assert
        result.IsSuccess.ShouldBeTrue("Board creation should succeed");
        result.Value.ShouldBe(command.Id, "Returned Guid should match the command Id");
        await _transaction.Received(1).CommitAsync();
    }

    [Fact]
    [Trait(Constants.TraitName, Constants.TestTitle)]
    public async Task InvokeAsync_WhenBoardIsCreated_UsesDefaultConfiguration()
    {
        // Arrange
        var command = new CreateBoardCommand(Guid.CreateVersion7(), "Curse of Strahd");

        // Act
        await _sut.InvokeAsync(command);

        // Assert
        await _boardWriter.Received(1).CreateAsync(
            Arg.Any<ITransaction>(),
            Arg.Is<CreateBoard>(create => create!.Id == command.Id && create.Name == command.Name),
            BoardConfiguration.Default);
    }

    [Fact]
    [Trait(Constants.TraitName, Constants.TestTitle)]
    public async Task InvokeAsync_WhenNameIsInvalid_ReturnsValueObjectException()
    {
        // Arrange
        var command = new CreateBoardCommand(Guid.CreateVersion7(), new string('*', 51));

        // Act
        var result = await _sut.InvokeAsync(command);

        // Assert
        result.IsSuccess.ShouldBeFalse("Create should fail for a name over 50 characters");
        result.Error.ShouldBeOfType<ValueObjectException>("Error should be ValueObjectException");
        await _transaction.DidNotReceive().CommitAsync();
    }

    [Fact]
    [Trait(Constants.TraitName, Constants.TestTitle)]
    public async Task InvokeAsync_WhenRepositoryThrows_ReturnsExceptionAndRollsBack()
    {
        // Arrange
        var command = new CreateBoardCommand(Guid.CreateVersion7(), "Curse of Strahd");
        var expectedException = new Exception("DB error");
        _boardWriter
            .CreateAsync(Arg.Any<ITransaction>(), Arg.Any<CreateBoard>(), Arg.Any<BoardConfiguration>())
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
