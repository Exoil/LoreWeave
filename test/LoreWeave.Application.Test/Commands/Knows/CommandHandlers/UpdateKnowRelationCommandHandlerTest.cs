using NSubstitute;
using NSubstitute.ExceptionExtensions;

using LoreWeave.Application.Commands.Knows;
using LoreWeave.Application.Commands.Knows.CommandHandlers;
using LoreWeave.Domain.Exceptions;
using LoreWeave.Domain.Transactions;
using LoreWeave.Domain.Models;
using LoreWeave.Domain.Repositories.Knows;

using Serilog;

using Shouldly;

namespace LoreWeave.Application.Test.Commands.Knows.CommandHandlers;

public class UpdateKnowRelationCommandHandlerTest
{
    private static readonly Guid BoardId = Guid.NewGuid();

    private readonly IExistsKnowRelation _existsKnowRelation;
    private readonly IKnowRelationWriter _knowRelationWriter;
    private readonly ITransaction _transaction;
    private readonly UpdateKnowRelationCommandHandler _sut;

    private static readonly Guid FromCharacterId = Guid.NewGuid();
    private static readonly Guid ToCharacterId = Guid.NewGuid();
    private const int CurrentVersion = 1;

    public UpdateKnowRelationCommandHandlerTest()
    {
        _existsKnowRelation = Substitute.For<IExistsKnowRelation>();
        _knowRelationWriter = Substitute.For<IKnowRelationWriter>();
        var logger = Substitute.For<ILogger>();
        _transaction = Substitute.For<ITransaction>();
        var transactionFactory = Substitute.For<ITransactionFactory>();
        transactionFactory.CreateAsync().Returns(_transaction);

        _sut = new UpdateKnowRelationCommandHandler(transactionFactory, _existsKnowRelation, _knowRelationWriter, logger);
    }

    [Fact]
    [Trait(Constants.TraitName, Constants.TestTitle)]
    public async Task InvokeAsync_WhenRelationExistsAndVersionMatches_ReturnsSuccess()
    {
        // Arrange
        const string description = "Updated description";
        var command = new UpdateKnowRelationCommand(BoardId, FromCharacterId, ToCharacterId, description, false, CurrentVersion);
        _existsKnowRelation
            .KnowRelationExistsAsync(Arg.Any<ITransaction>(), Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<Guid>())
            .Returns(new EntityExistence(true, CurrentVersion));

        // Act
        var result = await _sut.InvokeAsync(command);

        // Assert
        result.IsSuccess.ShouldBeTrue("Update should succeed when relation exists and version matches");
        await _transaction.Received(1).CommitAsync();
        await _knowRelationWriter
            .Received(1)
            .UpdateKnowRelationAsync(
                Arg.Any<ITransaction>(),
                Arg.Is<LoreWeave.Domain.Entities.Knows.Commands.UpdateKnowRelation>(r =>
                    r!.Description == description && !r.IsStrongRelation));
    }

    [Fact]
    [Trait(Constants.TraitName, Constants.TestTitle)]
    public async Task InvokeAsync_WhenRelationDoesNotExist_ReturnsNotFoundException()
    {
        // Arrange
        var command = new UpdateKnowRelationCommand(BoardId, FromCharacterId, ToCharacterId, "Updated description", true, CurrentVersion);
        _existsKnowRelation
            .KnowRelationExistsAsync(Arg.Any<ITransaction>(), Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<Guid>())
            .Returns(new EntityExistence(false, 0));

        // Act
        var result = await _sut.InvokeAsync(command);

        // Assert
        result.IsSuccess.ShouldBeFalse("Update should fail when relation does not exist");
        result.Error.ShouldBeOfType<NotFoundException>("Error should be NotFoundException");
        await _knowRelationWriter
            .DidNotReceive()
            .UpdateKnowRelationAsync(Arg.Any<ITransaction>(), Arg.Any<LoreWeave.Domain.Entities.Knows.Commands.UpdateKnowRelation>());
    }

    [Fact]
    [Trait(Constants.TraitName, Constants.TestTitle)]
    public async Task InvokeAsync_WhenVersionDoesNotMatch_ReturnsPreconditionException()
    {
        // Arrange
        var command = new UpdateKnowRelationCommand(BoardId, FromCharacterId, ToCharacterId, "Updated description", true, CurrentVersion);
        _existsKnowRelation
            .KnowRelationExistsAsync(Arg.Any<ITransaction>(), Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<Guid>())
            .Returns(new EntityExistence(true, CurrentVersion + 1));

        // Act
        var result = await _sut.InvokeAsync(command);

        // Assert
        result.IsSuccess.ShouldBeFalse("Update should fail when version does not match");
        result.Error.ShouldBeOfType<PreconditionException>("Error should be PreconditionException");
        await _transaction.DidNotReceive().CommitAsync();
    }

    [Fact]
    [Trait(Constants.TraitName, Constants.TestTitle)]
    public async Task InvokeAsync_WhenFromAndToAreSame_ReturnsArgumentException()
    {
        // Arrange
        var command = new UpdateKnowRelationCommand(BoardId, FromCharacterId, FromCharacterId, "Updated description", true, CurrentVersion);

        // Act
        var result = await _sut.InvokeAsync(command);

        // Assert
        result.IsSuccess.ShouldBeFalse("Update should fail when from and to are the same character");
        result.Error.ShouldBeOfType<ArgumentException>("Error should be ArgumentException");
        await _transaction.Received(1).RollbackAsync();
    }

    [Fact]
    [Trait(Constants.TraitName, Constants.TestTitle)]
    public async Task InvokeAsync_WhenRepositoryThrows_ReturnsExceptionAndRollsBack()
    {
        // Arrange
        var command = new UpdateKnowRelationCommand(BoardId, FromCharacterId, ToCharacterId, "Updated description", true, CurrentVersion);
        var expectedException = new Exception("DB error");
        _existsKnowRelation
            .KnowRelationExistsAsync(Arg.Any<ITransaction>(), Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<Guid>())
            .Returns(new EntityExistence(true, CurrentVersion));
        _knowRelationWriter
            .UpdateKnowRelationAsync(Arg.Any<ITransaction>(), Arg.Any<LoreWeave.Domain.Entities.Knows.Commands.UpdateKnowRelation>())
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