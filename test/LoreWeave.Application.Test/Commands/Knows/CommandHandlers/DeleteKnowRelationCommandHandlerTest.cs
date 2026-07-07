using NSubstitute;
using NSubstitute.ExceptionExtensions;

using LoreWeave.Application.Commands.Knows;
using LoreWeave.Application.Commands.Knows.CommandHandlers;
using LoreWeave.Domain.Exceptions;
using LoreWeave.Domain.Models;
using LoreWeave.Domain.Repositories.Knows;
using LoreWeave.Domain.Transactions;

using Serilog;

using Shouldly;

namespace LoreWeave.Application.Test.Commands.Knows.CommandHandlers;

public class DeleteKnowRelationCommandHandlerTest
{
    private readonly IExistsKnowRelation _existsKnowRelation;
    private readonly IKnowRelationWriter _knowRelationWriter;
    private readonly ITransaction _transaction;
    private readonly DeleteKnowRelationCommandHandler _sut;

    private static readonly Guid BoardId = Guid.NewGuid();
    private static readonly Guid _fromCharacterId = Guid.CreateVersion7();
    private static readonly Guid _toCharacterId = Guid.CreateVersion7();

    public DeleteKnowRelationCommandHandlerTest()
    {
        _existsKnowRelation = Substitute.For<IExistsKnowRelation>();
        _existsKnowRelation
            .KnowRelationExistsAsync(Arg.Any<ITransaction>(), Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<Guid>())
            .Returns(new EntityExistence(true, 1));
        _knowRelationWriter = Substitute.For<IKnowRelationWriter>();
        var logger = Substitute.For<ILogger>();
        _transaction = Substitute.For<ITransaction>();
        var transactionFactory = Substitute.For<ITransactionFactory>();
        transactionFactory.CreateAsync().Returns(_transaction);

        _sut = new DeleteKnowRelationCommandHandler(transactionFactory, _existsKnowRelation, _knowRelationWriter, logger);
    }

    [Fact]
    [Trait(Constants.TraitName, Constants.TestTitle)]
    public async Task InvokeAsync_WhenRelationExists_ReturnsSuccess()
    {
        // Arrange
        var command = new DeleteKnowRelationCommand(BoardId, _fromCharacterId, _toCharacterId);

        // Act
        var result = await _sut.InvokeAsync(command);

        // Assert
        result.IsSuccess.ShouldBeTrue("Know relation deletion should succeed");
        await _transaction.Received(1).CommitAsync();
    }

    [Fact]
    [Trait(Constants.TraitName, Constants.TestTitle)]
    public async Task InvokeAsync_WhenRepositoryThrows_ReturnsExceptionAndRollsBack()
    {
        // Arrange
        var command = new DeleteKnowRelationCommand(BoardId, _fromCharacterId, _toCharacterId);
        var expectedException = new Exception("DB error");
        _knowRelationWriter
            .DeleteKnowRelationAsync(Arg.Any<ITransaction>(), Arg.Any<LoreWeave.Domain.Entities.Knows.Commands.DeleteKnowRelation>())
            .ThrowsAsync(expectedException);

        // Act
        var result = await _sut.InvokeAsync(command);

        // Assert
        result.IsSuccess.ShouldBeFalse("Result should be failure when repository throws");
        result.Error.ShouldBe(expectedException, "Error should be the thrown exception");
        await _transaction.Received(1).RollbackAsync();
        await _transaction.DidNotReceive().CommitAsync();
    }

    [Fact]
    [Trait(Constants.TraitName, Constants.TestTitle)]
    public async Task InvokeAsync_WhenRelationDoesNotExist_ReturnsNotFoundException()
    {
        // Arrange
        var command = new DeleteKnowRelationCommand(BoardId, _fromCharacterId, _toCharacterId);
        _existsKnowRelation
            .KnowRelationExistsAsync(Arg.Any<ITransaction>(), Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<Guid>())
            .Returns(new EntityExistence(false, 0));

        // Act
        var result = await _sut.InvokeAsync(command);

        // Assert
        result.IsSuccess.ShouldBeFalse("Delete should fail when the relation does not exist");
        result.Error.ShouldBeOfType<NotFoundException>("Error should be NotFoundException");
        await _transaction.DidNotReceive().CommitAsync();
    }
}
