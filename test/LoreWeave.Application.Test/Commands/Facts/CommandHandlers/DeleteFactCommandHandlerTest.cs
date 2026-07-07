using NSubstitute;
using NSubstitute.ExceptionExtensions;

using LoreWeave.Application.Commands.Facts;
using LoreWeave.Application.Commands.Facts.CommandHandlers;
using LoreWeave.Domain.Entities.Facts.Commands;
using LoreWeave.Domain.Exceptions;
using LoreWeave.Domain.Transactions;
using LoreWeave.Domain.Models;
using LoreWeave.Domain.Repositories.Facts;

using Serilog;

using Shouldly;

namespace LoreWeave.Application.Test.Commands.Facts.CommandHandlers;

public class DeleteFactCommandHandlerTest
{
    private static readonly Guid BoardId = Guid.NewGuid();

    private readonly IExistsFact _existsFact;
    private readonly IFactWriter _factWriter;
    private readonly ITransaction _transaction;
    private readonly DeleteFactCommandHandler _sut;

    private static readonly Guid FactId = Guid.NewGuid();

    public DeleteFactCommandHandlerTest()
    {
        _existsFact = Substitute.For<IExistsFact>();
        _factWriter = Substitute.For<IFactWriter>();
        var logger = Substitute.For<ILogger>();
        _transaction = Substitute.For<ITransaction>();
        var transactionFactory = Substitute.For<ITransactionFactory>();
        transactionFactory.CreateAsync().Returns(_transaction);

        _sut = new DeleteFactCommandHandler(transactionFactory, _existsFact, _factWriter, logger);
    }

    [Fact]
    [Trait(Constants.TraitName, Constants.TestTitle)]
    public async Task InvokeAsync_WhenFactExists_ReturnsSuccess()
    {
        // Arrange
        var command = new DeleteFactCommand(BoardId, FactId);
        _existsFact
            .FactExistsAsync(Arg.Any<ITransaction>(), BoardId, FactId)
            .Returns(new EntityExistence(true, 1));

        // Act
        var result = await _sut.InvokeAsync(command);

        // Assert
        result.IsSuccess.ShouldBeTrue("Delete should succeed when fact exists");
        await _factWriter.Received(1)
            .DeleteAsync(Arg.Any<ITransaction>(), Arg.Is<DeleteFact>(deleteFact => deleteFact.Id == FactId));
        await _transaction.Received(1).CommitAsync();
    }

    [Fact]
    [Trait(Constants.TraitName, Constants.TestTitle)]
    public async Task InvokeAsync_WhenFactDoesNotExist_ReturnsNotFoundException()
    {
        // Arrange
        var command = new DeleteFactCommand(BoardId, FactId);
        _existsFact
            .FactExistsAsync(Arg.Any<ITransaction>(), BoardId, FactId)
            .Returns(new EntityExistence(false, 0));

        // Act
        var result = await _sut.InvokeAsync(command);

        // Assert
        result.IsSuccess.ShouldBeFalse("Delete should fail when fact does not exist");
        result.Error.ShouldBeOfType<NotFoundException>("Error should be NotFoundException");
        await _transaction.DidNotReceive().CommitAsync();
    }

    [Fact]
    [Trait(Constants.TraitName, Constants.TestTitle)]
    public async Task InvokeAsync_WhenRepositoryThrows_ReturnsExceptionAndRollsBack()
    {
        // Arrange
        var command = new DeleteFactCommand(BoardId, FactId);
        var expectedException = new Exception("DB error");
        _existsFact
            .FactExistsAsync(Arg.Any<ITransaction>(), BoardId, FactId)
            .Returns(new EntityExistence(true, 1));
        _factWriter
            .DeleteAsync(Arg.Any<ITransaction>(), Arg.Any<DeleteFact>())
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