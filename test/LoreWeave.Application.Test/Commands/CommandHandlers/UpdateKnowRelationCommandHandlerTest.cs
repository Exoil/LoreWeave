using Neo4j.Driver;

using NSubstitute;
using NSubstitute.ExceptionExtensions;

using LoreWeave.Application.Commands;
using LoreWeave.Application.Commands.CommandHandlers;
using LoreWeave.Domain.Exceptions;
using LoreWeave.Domain.Factories;
using LoreWeave.Domain.Models;
using LoreWeave.Domain.Repositories;

using Serilog;

using Shouldly;

namespace LoreWeave.Application.Test.Commands.CommandHandlers;

public class UpdateKnowRelationCommandHandlerTest
{
    private readonly ICharacterRepository _characterRepository;
    private readonly IAsyncTransaction _transaction;
    private readonly UpdateKnowRelationCommandHandler _sut;

    private static readonly Guid FromCharacterId = Guid.NewGuid();
    private static readonly Guid ToCharacterId = Guid.NewGuid();
    private const int CurrentVersion = 1;

    public UpdateKnowRelationCommandHandlerTest()
    {
        _characterRepository = Substitute.For<ICharacterRepository>();
        var logger = Substitute.For<ILogger>();
        _transaction = Substitute.For<IAsyncTransaction>();
        var transactionFactory = Substitute.For<ITransactionFactory<IAsyncTransaction>>();
        transactionFactory.CreateAsync().Returns(_transaction);

        _sut = new UpdateKnowRelationCommandHandler(transactionFactory, _characterRepository, logger);
    }

    [Fact]
    [Trait(Constants.TraitName, Constants.TestTitle)]
    public async Task InvokeAsync_WhenRelationExistsAndVersionMatches_ReturnsSuccess()
    {
        // Arrange
        const string description = "Updated description";
        var command = new UpdateKnowRelationCommand(FromCharacterId, ToCharacterId, description, false, CurrentVersion);
        _characterRepository
            .KnowRelationExistsAsync(Arg.Any<IAsyncTransaction>(), Arg.Any<Guid>(), Arg.Any<Guid>())
            .Returns(new EntityExistence(true, CurrentVersion));

        // Act
        var result = await _sut.InvokeAsync(command);

        // Assert
        result.IsSuccess.ShouldBeTrue("Update should succeed when relation exists and version matches");
        await _transaction.Received(1).CommitAsync();
        await _characterRepository
            .Received(1)
            .UpdateKnowRelationAsync(
                Arg.Any<IAsyncTransaction>(),
                Arg.Is<LoreWeave.Domain.Entities.Knows.Commands.UpdateKnowRelation>(r =>
                    r.Description == description && !r.IsStrongRelation));
    }

    [Fact]
    [Trait(Constants.TraitName, Constants.TestTitle)]
    public async Task InvokeAsync_WhenRelationDoesNotExist_ReturnsNotFoundException()
    {
        // Arrange
        var command = new UpdateKnowRelationCommand(FromCharacterId, ToCharacterId, "Updated description", true, CurrentVersion);
        _characterRepository
            .KnowRelationExistsAsync(Arg.Any<IAsyncTransaction>(), Arg.Any<Guid>(), Arg.Any<Guid>())
            .Returns(new EntityExistence(false, 0));

        // Act
        var result = await _sut.InvokeAsync(command);

        // Assert
        result.IsSuccess.ShouldBeFalse("Update should fail when relation does not exist");
        result.Error.ShouldBeOfType<NotFoundException>("Error should be NotFoundException");
        await _characterRepository
            .DidNotReceive()
            .UpdateKnowRelationAsync(Arg.Any<IAsyncTransaction>(), Arg.Any<LoreWeave.Domain.Entities.Knows.Commands.UpdateKnowRelation>());
    }

    [Fact]
    [Trait(Constants.TraitName, Constants.TestTitle)]
    public async Task InvokeAsync_WhenVersionDoesNotMatch_ReturnsPreconditionException()
    {
        // Arrange
        var command = new UpdateKnowRelationCommand(FromCharacterId, ToCharacterId, "Updated description", true, CurrentVersion);
        _characterRepository
            .KnowRelationExistsAsync(Arg.Any<IAsyncTransaction>(), Arg.Any<Guid>(), Arg.Any<Guid>())
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
        var command = new UpdateKnowRelationCommand(FromCharacterId, FromCharacterId, "Updated description", true, CurrentVersion);

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
        var command = new UpdateKnowRelationCommand(FromCharacterId, ToCharacterId, "Updated description", true, CurrentVersion);
        var expectedException = new Exception("DB error");
        _characterRepository
            .KnowRelationExistsAsync(Arg.Any<IAsyncTransaction>(), Arg.Any<Guid>(), Arg.Any<Guid>())
            .Returns(new EntityExistence(true, CurrentVersion));
        _characterRepository
            .UpdateKnowRelationAsync(Arg.Any<IAsyncTransaction>(), Arg.Any<LoreWeave.Domain.Entities.Knows.Commands.UpdateKnowRelation>())
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