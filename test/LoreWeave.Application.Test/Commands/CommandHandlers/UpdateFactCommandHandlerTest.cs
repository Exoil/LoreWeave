using Neo4j.Driver;

using NSubstitute;
using NSubstitute.ExceptionExtensions;

using LoreWeave.Application.Commands;
using LoreWeave.Application.Commands.CommandHandlers;
using LoreWeave.Domain.Entities.Facts.Commands;
using LoreWeave.Domain.Exceptions;
using LoreWeave.Domain.Factories;
using LoreWeave.Domain.Models;
using LoreWeave.Domain.Repositories;

using Serilog;

using Shouldly;

namespace LoreWeave.Application.Test.Commands.CommandHandlers;

public class UpdateFactCommandHandlerTest
{
    private readonly ICharacterRepository _characterRepository;
    private readonly IAsyncTransaction _transaction;
    private readonly UpdateFactCommandHandler _sut;

    private static readonly Guid FactId = Guid.NewGuid();
    private const int CurrentVersion = 1;

    public UpdateFactCommandHandlerTest()
    {
        _characterRepository = Substitute.For<ICharacterRepository>();
        var logger = Substitute.For<ILogger>();
        _transaction = Substitute.For<IAsyncTransaction>();
        var transactionFactory = Substitute.For<ITransactionFactory<IAsyncTransaction>>();
        transactionFactory.CreateAsync().Returns(_transaction);

        _sut = new UpdateFactCommandHandler(transactionFactory, _characterRepository, logger);
    }

    [Fact]
    [Trait(Constants.TraitName, Constants.TestTitle)]
    public async Task InvokeAsync_WhenFactExistsAndVersionMatches_ReturnsSuccess()
    {
        // Arrange
        var command = new UpdateFactCommand(FactId, "UpdatedTitle", "UpdatedContent", CurrentVersion);
        _characterRepository
            .FactExistsAsync(Arg.Any<IAsyncTransaction>(), Arg.Any<Guid>())
            .Returns(new EntityExistence(true, CurrentVersion));

        // Act
        var result = await _sut.InvokeAsync(command);

        // Assert
        result.IsSuccess.ShouldBeTrue("Update should succeed when fact exists and version matches");
        await _transaction.Received(1).CommitAsync();
    }

    [Fact]
    [Trait(Constants.TraitName, Constants.TestTitle)]
    public async Task InvokeAsync_WhenFactDoesNotExist_ReturnsNotFoundException()
    {
        // Arrange
        var command = new UpdateFactCommand(FactId, "UpdatedTitle", "UpdatedContent", CurrentVersion);
        _characterRepository
            .FactExistsAsync(Arg.Any<IAsyncTransaction>(), Arg.Any<Guid>())
            .Returns(new EntityExistence(false, 0));

        // Act
        var result = await _sut.InvokeAsync(command);

        // Assert
        result.IsSuccess.ShouldBeFalse("Update should fail when fact does not exist");
        result.Error.ShouldBeOfType<NotFoundException>("Error should be NotFoundException");
    }

    [Fact]
    [Trait(Constants.TraitName, Constants.TestTitle)]
    public async Task InvokeAsync_WhenVersionDoesNotMatch_ReturnsPreconditionException()
    {
        // Arrange
        var command = new UpdateFactCommand(FactId, "UpdatedTitle", "UpdatedContent", CurrentVersion);
        _characterRepository
            .FactExistsAsync(Arg.Any<IAsyncTransaction>(), Arg.Any<Guid>())
            .Returns(new EntityExistence(true, CurrentVersion + 1));

        // Act
        var result = await _sut.InvokeAsync(command);

        // Assert
        result.IsSuccess.ShouldBeFalse("Update should fail when version does not match");
        result.Error.ShouldBeOfType<PreconditionException>("Error should be PreconditionException");
    }

    [Fact]
    [Trait(Constants.TraitName, Constants.TestTitle)]
    public async Task InvokeAsync_WhenRepositoryThrows_ReturnsExceptionAndRollsBack()
    {
        // Arrange
        var command = new UpdateFactCommand(FactId, "UpdatedTitle", "UpdatedContent", CurrentVersion);
        var expectedException = new Exception("DB error");
        _characterRepository
            .FactExistsAsync(Arg.Any<IAsyncTransaction>(), Arg.Any<Guid>())
            .Returns(new EntityExistence(true, CurrentVersion));
        _characterRepository
            .UpdateAsync(Arg.Any<IAsyncTransaction>(), Arg.Any<UpdateFact>())
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
