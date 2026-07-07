using NSubstitute;
using NSubstitute.ExceptionExtensions;

using LoreWeave.Application.Commands.Facts;
using LoreWeave.Application.Commands.Facts.CommandHandlers;
using LoreWeave.Domain.Exceptions;
using LoreWeave.Domain.Transactions;
using LoreWeave.Domain.Models;
using LoreWeave.Domain.Repositories.Characters;
using LoreWeave.Domain.Repositories.Facts;

using Serilog;

using Shouldly;

namespace LoreWeave.Application.Test.Commands.Facts.CommandHandlers;

public class ConnectFactToCharacterCommandHandlerTest
{
    private static readonly Guid BoardId = Guid.NewGuid();

    private readonly IExistsCharacter _existsCharacter;
    private readonly IExistsFact _existsFact;
    private readonly IFactConnection _factConnection;
    private readonly ITransaction _transaction;
    private readonly ConnectFactToCharacterCommandHandler _sut;

    private static readonly Guid CharacterId = Guid.NewGuid();
    private static readonly Guid FactId = Guid.NewGuid();

    public ConnectFactToCharacterCommandHandlerTest()
    {
        _existsCharacter = Substitute.For<IExistsCharacter>();
        _existsFact = Substitute.For<IExistsFact>();
        _factConnection = Substitute.For<IFactConnection>();
        var logger = Substitute.For<ILogger>();
        _transaction = Substitute.For<ITransaction>();
        var transactionFactory = Substitute.For<ITransactionFactory>();
        transactionFactory.CreateAsync().Returns(_transaction);

        _sut = new ConnectFactToCharacterCommandHandler(transactionFactory, _existsCharacter, _existsFact, _factConnection, logger);
    }

    [Fact]
    [Trait(Constants.TraitName, Constants.TestTitle)]
    public async Task InvokeAsync_WhenCharacterAndFactExistAndNotConnected_ReturnsSuccess()
    {
        // Arrange
        var command = new ConnectFactToCharacterCommand(BoardId, CharacterId, FactId);
        _existsCharacter
            .CharacterExistsAsync(Arg.Any<ITransaction>(), BoardId, CharacterId)
            .Returns(new EntityExistence(true, 1));
        _existsFact
            .FactExistsAsync(Arg.Any<ITransaction>(), BoardId, FactId)
            .Returns(new EntityExistence(true, 1));
        _factConnection
            .FactConnectionExistsAsync(Arg.Any<ITransaction>(), CharacterId, FactId)
            .Returns(false);

        // Act
        var result = await _sut.InvokeAsync(command);

        // Assert
        result.IsSuccess.ShouldBeTrue("Connect should succeed when character and fact exist and are not connected");
        await _factConnection.Received(1)
            .ConnectFactToCharacterAsync(Arg.Any<ITransaction>(), CharacterId, FactId);
        await _transaction.Received(1).CommitAsync();
    }

    [Fact]
    [Trait(Constants.TraitName, Constants.TestTitle)]
    public async Task InvokeAsync_WhenConnectionAlreadyExists_ReturnsConflictException()
    {
        // Arrange
        var command = new ConnectFactToCharacterCommand(BoardId, CharacterId, FactId);
        _existsCharacter
            .CharacterExistsAsync(Arg.Any<ITransaction>(), BoardId, CharacterId)
            .Returns(new EntityExistence(true, 1));
        _existsFact
            .FactExistsAsync(Arg.Any<ITransaction>(), BoardId, FactId)
            .Returns(new EntityExistence(true, 1));
        _factConnection
            .FactConnectionExistsAsync(Arg.Any<ITransaction>(), CharacterId, FactId)
            .Returns(true);

        // Act
        var result = await _sut.InvokeAsync(command);

        // Assert
        result.IsSuccess.ShouldBeFalse("Connect should fail when the pair is already connected");
        result.Error.ShouldBeOfType<ConflictException>("Error should be ConflictException");
        await _factConnection.DidNotReceive()
            .ConnectFactToCharacterAsync(Arg.Any<ITransaction>(), Arg.Any<Guid>(), Arg.Any<Guid>());
        await _transaction.DidNotReceive().CommitAsync();
    }

    [Fact]
    [Trait(Constants.TraitName, Constants.TestTitle)]
    public async Task InvokeAsync_WhenCharacterDoesNotExist_ReturnsNotFoundException()
    {
        // Arrange
        var command = new ConnectFactToCharacterCommand(BoardId, CharacterId, FactId);
        _existsCharacter
            .CharacterExistsAsync(Arg.Any<ITransaction>(), BoardId, CharacterId)
            .Returns(new EntityExistence(false, 0));

        // Act
        var result = await _sut.InvokeAsync(command);

        // Assert
        result.IsSuccess.ShouldBeFalse("Connect should fail when character does not exist");
        result.Error.ShouldBeOfType<NotFoundException>("Error should be NotFoundException");
        await _transaction.DidNotReceive().CommitAsync();
    }

    [Fact]
    [Trait(Constants.TraitName, Constants.TestTitle)]
    public async Task InvokeAsync_WhenFactDoesNotExist_ReturnsNotFoundException()
    {
        // Arrange
        var command = new ConnectFactToCharacterCommand(BoardId, CharacterId, FactId);
        _existsCharacter
            .CharacterExistsAsync(Arg.Any<ITransaction>(), BoardId, CharacterId)
            .Returns(new EntityExistence(true, 1));
        _existsFact
            .FactExistsAsync(Arg.Any<ITransaction>(), BoardId, FactId)
            .Returns(new EntityExistence(false, 0));

        // Act
        var result = await _sut.InvokeAsync(command);

        // Assert
        result.IsSuccess.ShouldBeFalse("Connect should fail when fact does not exist");
        result.Error.ShouldBeOfType<NotFoundException>("Error should be NotFoundException");
        await _transaction.DidNotReceive().CommitAsync();
    }

    [Fact]
    [Trait(Constants.TraitName, Constants.TestTitle)]
    public async Task InvokeAsync_WhenRepositoryThrows_ReturnsExceptionAndRollsBack()
    {
        // Arrange
        var command = new ConnectFactToCharacterCommand(BoardId, CharacterId, FactId);
        var expectedException = new Exception("DB error");
        _existsCharacter
            .CharacterExistsAsync(Arg.Any<ITransaction>(), BoardId, CharacterId)
            .Returns(new EntityExistence(true, 1));
        _existsFact
            .FactExistsAsync(Arg.Any<ITransaction>(), BoardId, FactId)
            .Returns(new EntityExistence(true, 1));
        _factConnection
            .FactConnectionExistsAsync(Arg.Any<ITransaction>(), CharacterId, FactId)
            .Returns(false);
        _factConnection
            .ConnectFactToCharacterAsync(Arg.Any<ITransaction>(), CharacterId, FactId)
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