using NSubstitute;
using NSubstitute.ExceptionExtensions;

using LoreWeave.Application.Commands.Characters;
using LoreWeave.Application.Commands.Characters.CommandHandlers;
using LoreWeave.Domain.Exceptions;
using LoreWeave.Domain.Transactions;
using LoreWeave.Domain.Models;
using LoreWeave.Domain.Repositories.Characters;

using Serilog;

using Shouldly;

namespace LoreWeave.Application.Test.Commands.Characters.CommandHandlers;

public class DeleteCharacterCommandHandlerTest
{
    private static readonly Guid BoardId = Guid.NewGuid();

    private readonly IExistsCharacter _existsCharacter;
    private readonly ICharacterWriter _characterWriter;
    private readonly ITransaction _transaction;
    private readonly DeleteCharacterCommandHandler _sut;

    private static readonly Guid _characterId = Guid.NewGuid();

    public DeleteCharacterCommandHandlerTest()
    {
        _existsCharacter = Substitute.For<IExistsCharacter>();
        _characterWriter = Substitute.For<ICharacterWriter>();
        var logger = Substitute.For<ILogger>();
        _transaction = Substitute.For<ITransaction>();
        var transactionFactory = Substitute.For<ITransactionFactory>();
        transactionFactory.CreateAsync().Returns(_transaction);

        _sut = new DeleteCharacterCommandHandler(transactionFactory, _existsCharacter, _characterWriter, logger);
    }

    [Fact]
    [Trait(Constants.TraitName, Constants.TestTitle)]
    public async Task InvokeAsync_WhenCharacterExists_ReturnsSuccess()
    {
        // Arrange
        var command = new DeleteCharacterCommand(BoardId, _characterId);
        _existsCharacter
            .CharacterExistsAsync(Arg.Any<ITransaction>(), Arg.Any<Guid>(), Arg.Any<Guid>())
            .Returns(new EntityExistence(true, 1));

        // Act
        var result = await _sut.InvokeAsync(command);

        // Assert
        result.IsSuccess.ShouldBeTrue("Delete should succeed when character exists");
        await _transaction.Received(1).CommitAsync();
    }

    [Fact]
    [Trait(Constants.TraitName, Constants.TestTitle)]
    public async Task InvokeAsync_WhenCharacterDoesNotExist_ReturnsNotFoundException()
    {
        // Arrange
        var command = new DeleteCharacterCommand(BoardId, _characterId);
        _existsCharacter
            .CharacterExistsAsync(Arg.Any<ITransaction>(), Arg.Any<Guid>(), Arg.Any<Guid>())
            .Returns(new EntityExistence(false, 0));

        // Act
        var result = await _sut.InvokeAsync(command);

        // Assert
        result.IsSuccess.ShouldBeFalse("Delete should fail when character does not exist");
        result.Error.ShouldBeOfType<NotFoundException>("Error should be NotFoundException");
    }

    [Fact]
    [Trait(Constants.TraitName, Constants.TestTitle)]
    public async Task InvokeAsync_WhenRepositoryThrows_ReturnsExceptionAndRollsBack()
    {
        // Arrange
        var command = new DeleteCharacterCommand(BoardId, _characterId);
        var expectedException = new Exception("DB error");
        _existsCharacter
            .CharacterExistsAsync(Arg.Any<ITransaction>(), Arg.Any<Guid>(), Arg.Any<Guid>())
            .Returns(new EntityExistence(true, 1));
        _characterWriter
            .DeleteAsync(Arg.Any<ITransaction>(), Arg.Any<LoreWeave.Domain.Entities.Characters.Commands.DeleteCharacter>())
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
