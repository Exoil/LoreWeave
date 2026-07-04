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

public class DeleteFactCommandHandlerTest
{
    private readonly ICharacterRepository _characterRepository;
    private readonly IAsyncTransaction _transaction;
    private readonly DeleteFactCommandHandler _sut;

    private static readonly Guid FactId = Guid.NewGuid();

    public DeleteFactCommandHandlerTest()
    {
        _characterRepository = Substitute.For<ICharacterRepository>();
        var logger = Substitute.For<ILogger>();
        _transaction = Substitute.For<IAsyncTransaction>();
        var transactionFactory = Substitute.For<ITransactionFactory<IAsyncTransaction>>();
        transactionFactory.CreateAsync().Returns(_transaction);

        _sut = new DeleteFactCommandHandler(transactionFactory, _characterRepository, logger);
    }

    [Fact]
    [Trait(Constants.TraitName, Constants.TestTitle)]
    public async Task InvokeAsync_WhenFactExists_ReturnsSuccess()
    {
        // Arrange
        var command = new DeleteFactCommand(FactId);
        _characterRepository
            .FactExistsAsync(Arg.Any<IAsyncTransaction>(), FactId)
            .Returns(new EntityExistence(true, 1));

        // Act
        var result = await _sut.InvokeAsync(command);

        // Assert
        result.IsSuccess.ShouldBeTrue("Delete should succeed when fact exists");
        await _characterRepository.Received(1)
            .DeleteAsync(Arg.Any<IAsyncTransaction>(), Arg.Is<DeleteFact>(deleteFact => deleteFact.Id == FactId));
        await _transaction.Received(1).CommitAsync();
    }

    [Fact]
    [Trait(Constants.TraitName, Constants.TestTitle)]
    public async Task InvokeAsync_WhenFactDoesNotExist_ReturnsNotFoundException()
    {
        // Arrange
        var command = new DeleteFactCommand(FactId);
        _characterRepository
            .FactExistsAsync(Arg.Any<IAsyncTransaction>(), FactId)
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
        var command = new DeleteFactCommand(FactId);
        var expectedException = new Exception("DB error");
        _characterRepository
            .FactExistsAsync(Arg.Any<IAsyncTransaction>(), FactId)
            .Returns(new EntityExistence(true, 1));
        _characterRepository
            .DeleteAsync(Arg.Any<IAsyncTransaction>(), Arg.Any<DeleteFact>())
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