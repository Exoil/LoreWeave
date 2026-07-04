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

public class DisconnectFactFromCharacterCommandHandlerTest
{
    private readonly ICharacterRepository _characterRepository;
    private readonly IAsyncTransaction _transaction;
    private readonly DisconnectFactFromCharacterCommandHandler _sut;

    private static readonly Guid CharacterId = Guid.NewGuid();
    private static readonly Guid FactId = Guid.NewGuid();

    public DisconnectFactFromCharacterCommandHandlerTest()
    {
        _characterRepository = Substitute.For<ICharacterRepository>();
        var logger = Substitute.For<ILogger>();
        _transaction = Substitute.For<IAsyncTransaction>();
        var transactionFactory = Substitute.For<ITransactionFactory<IAsyncTransaction>>();
        transactionFactory.CreateAsync().Returns(_transaction);

        _sut = new DisconnectFactFromCharacterCommandHandler(transactionFactory, _characterRepository, logger);
    }

    [Fact]
    [Trait(Constants.TraitName, Constants.TestTitle)]
    public async Task InvokeAsync_WhenConnectionExists_ReturnsSuccess()
    {
        // Arrange
        var command = new DisconnectFactFromCharacterCommand(CharacterId, FactId);
        _characterRepository
            .CharacterExistsAsync(Arg.Any<IAsyncTransaction>(), CharacterId)
            .Returns(new EntityExistence(true, 1));
        _characterRepository
            .FactExistsAsync(Arg.Any<IAsyncTransaction>(), FactId)
            .Returns(new EntityExistence(true, 1));
        _characterRepository
            .FactConnectionExistsAsync(Arg.Any<IAsyncTransaction>(), CharacterId, FactId)
            .Returns(true);

        // Act
        var result = await _sut.InvokeAsync(command);

        // Assert
        result.IsSuccess.ShouldBeTrue("Disconnect should succeed when the connection exists");
        await _characterRepository.Received(1)
            .DisconnectFactFromCharacterAsync(Arg.Any<IAsyncTransaction>(), CharacterId, FactId);
        await _transaction.Received(1).CommitAsync();
    }

    [Fact]
    [Trait(Constants.TraitName, Constants.TestTitle)]
    public async Task InvokeAsync_WhenCharacterDoesNotExist_ReturnsNotFoundException()
    {
        // Arrange
        var command = new DisconnectFactFromCharacterCommand(CharacterId, FactId);
        _characterRepository
            .CharacterExistsAsync(Arg.Any<IAsyncTransaction>(), CharacterId)
            .Returns(new EntityExistence(false, 0));

        // Act
        var result = await _sut.InvokeAsync(command);

        // Assert
        result.IsSuccess.ShouldBeFalse("Disconnect should fail when character does not exist");
        result.Error.ShouldBeOfType<NotFoundException>("Error should be NotFoundException");
        await _transaction.DidNotReceive().CommitAsync();
    }

    [Fact]
    [Trait(Constants.TraitName, Constants.TestTitle)]
    public async Task InvokeAsync_WhenFactDoesNotExist_ReturnsNotFoundException()
    {
        // Arrange
        var command = new DisconnectFactFromCharacterCommand(CharacterId, FactId);
        _characterRepository
            .CharacterExistsAsync(Arg.Any<IAsyncTransaction>(), CharacterId)
            .Returns(new EntityExistence(true, 1));
        _characterRepository
            .FactExistsAsync(Arg.Any<IAsyncTransaction>(), FactId)
            .Returns(new EntityExistence(false, 0));

        // Act
        var result = await _sut.InvokeAsync(command);

        // Assert
        result.IsSuccess.ShouldBeFalse("Disconnect should fail when fact does not exist");
        result.Error.ShouldBeOfType<NotFoundException>("Error should be NotFoundException");
        await _transaction.DidNotReceive().CommitAsync();
    }

    [Fact]
    [Trait(Constants.TraitName, Constants.TestTitle)]
    public async Task InvokeAsync_WhenConnectionDoesNotExist_ReturnsNotFoundException()
    {
        // Arrange
        var command = new DisconnectFactFromCharacterCommand(CharacterId, FactId);
        _characterRepository
            .CharacterExistsAsync(Arg.Any<IAsyncTransaction>(), CharacterId)
            .Returns(new EntityExistence(true, 1));
        _characterRepository
            .FactExistsAsync(Arg.Any<IAsyncTransaction>(), FactId)
            .Returns(new EntityExistence(true, 1));
        _characterRepository
            .FactConnectionExistsAsync(Arg.Any<IAsyncTransaction>(), CharacterId, FactId)
            .Returns(false);

        // Act
        var result = await _sut.InvokeAsync(command);

        // Assert
        result.IsSuccess.ShouldBeFalse("Disconnect should fail when the connection does not exist");
        result.Error.ShouldBeOfType<NotFoundException>("Error should be NotFoundException");
        await _characterRepository.DidNotReceive()
            .DisconnectFactFromCharacterAsync(Arg.Any<IAsyncTransaction>(), Arg.Any<Guid>(), Arg.Any<Guid>());
        await _transaction.DidNotReceive().CommitAsync();
    }

    [Fact]
    [Trait(Constants.TraitName, Constants.TestTitle)]
    public async Task InvokeAsync_WhenRepositoryThrows_ReturnsExceptionAndRollsBack()
    {
        // Arrange
        var command = new DisconnectFactFromCharacterCommand(CharacterId, FactId);
        var expectedException = new Exception("DB error");
        _characterRepository
            .CharacterExistsAsync(Arg.Any<IAsyncTransaction>(), CharacterId)
            .Returns(new EntityExistence(true, 1));
        _characterRepository
            .FactExistsAsync(Arg.Any<IAsyncTransaction>(), FactId)
            .Returns(new EntityExistence(true, 1));
        _characterRepository
            .FactConnectionExistsAsync(Arg.Any<IAsyncTransaction>(), CharacterId, FactId)
            .Returns(true);
        _characterRepository
            .DisconnectFactFromCharacterAsync(Arg.Any<IAsyncTransaction>(), CharacterId, FactId)
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
