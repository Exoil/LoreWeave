using NSubstitute;
using NSubstitute.ExceptionExtensions;

using LoreWeave.Application.Commands.Knows;
using LoreWeave.Application.Commands.Knows.CommandHandlers;
using LoreWeave.Domain.Transactions;
using LoreWeave.Domain.Repositories.Knows;

using Serilog;

using Shouldly;

namespace LoreWeave.Application.Test.Commands.Knows.CommandHandlers;

public class DeleteKnowRelationCommandHandlerTest
{
    private readonly IKnowRelationWriter _knowRelationWriter;
    private readonly ITransaction _transaction;
    private readonly DeleteKnowRelationCommandHandler _sut;

    private static readonly Guid _fromCharacterId = Guid.CreateVersion7();
    private static readonly Guid _toCharacterId = Guid.CreateVersion7();

    public DeleteKnowRelationCommandHandlerTest()
    {
        _knowRelationWriter = Substitute.For<IKnowRelationWriter>();
        var logger = Substitute.For<ILogger>();
        _transaction = Substitute.For<ITransaction>();
        var transactionFactory = Substitute.For<ITransactionFactory>();
        transactionFactory.CreateAsync().Returns(_transaction);

        _sut = new DeleteKnowRelationCommandHandler(transactionFactory, _knowRelationWriter, logger);
    }

    [Fact]
    [Trait(Constants.TraitName, Constants.TestTitle)]
    public async Task InvokeAsync_WhenRelationExists_ReturnsSuccess()
    {
        // Arrange
        var command = new DeleteKnowRelationCommand(_fromCharacterId, _toCharacterId);

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
        var command = new DeleteKnowRelationCommand(_fromCharacterId, _toCharacterId);
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
}
