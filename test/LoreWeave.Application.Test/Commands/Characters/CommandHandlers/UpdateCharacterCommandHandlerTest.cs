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

public class UpdateCharacterCommandHandlerTest
{
    private readonly IExistsCharacter _existsCharacter;
    private readonly ICharacterWriter _characterWriter;
    private readonly ITransaction _transaction;
    private readonly UpdateCharacterCommandHandler _sut;

    private static readonly Guid CharacterId = Guid.NewGuid();
    private const int CurrentVersion = 1;

    public UpdateCharacterCommandHandlerTest()
    {
        _existsCharacter = Substitute.For<IExistsCharacter>();
        _characterWriter = Substitute.For<ICharacterWriter>();
        var logger = Substitute.For<ILogger>();
        _transaction = Substitute.For<ITransaction>();
        var transactionFactory = Substitute.For<ITransactionFactory>();
        transactionFactory.CreateAsync().Returns(_transaction);

        _sut = new UpdateCharacterCommandHandler(transactionFactory, _existsCharacter, _characterWriter, logger);
    }

    [Fact]
    [Trait(Constants.TraitName, Constants.TestTitle)]
    public async Task InvokeAsync_WhenCharacterExistsAndVersionMatches_ReturnsSuccess()
    {
        // Arrange
        var command = new UpdateCharacterCommand(CharacterId, "UpdatedName", CurrentVersion);
        _existsCharacter
            .CharacterExistsAsync(Arg.Any<ITransaction>(), Arg.Any<Guid>())
            .Returns(new EntityExistence(true, CurrentVersion));

        // Act
        var result = await _sut.InvokeAsync(command);

        // Assert
        result.IsSuccess.ShouldBeTrue("Update should succeed when character exists and version matches");
        await _transaction.Received(1).CommitAsync();
    }

    [Fact]
    [Trait(Constants.TraitName, Constants.TestTitle)]
    public async Task InvokeAsync_WhenCharacterDoesNotExist_ReturnsNotFoundException()
    {
        // Arrange
        var command = new UpdateCharacterCommand(CharacterId, "UpdatedName", CurrentVersion);
        _existsCharacter
            .CharacterExistsAsync(Arg.Any<ITransaction>(), Arg.Any<Guid>())
            .Returns(new EntityExistence(false, 0));

        // Act
        var result = await _sut.InvokeAsync(command);

        // Assert
        result.IsSuccess.ShouldBeFalse("Update should fail when character does not exist");
        result.Error.ShouldBeOfType<NotFoundException>("Error should be NotFoundException");
    }

    [Fact]
    [Trait(Constants.TraitName, Constants.TestTitle)]
    public async Task InvokeAsync_WhenVersionDoesNotMatch_ReturnsPreconditionException()
    {
        // Arrange
        var command = new UpdateCharacterCommand(CharacterId, "UpdatedName", CurrentVersion);
        _existsCharacter
            .CharacterExistsAsync(Arg.Any<ITransaction>(), Arg.Any<Guid>())
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
        var command = new UpdateCharacterCommand(CharacterId, "UpdatedName", CurrentVersion);
        var expectedException = new Exception("DB error");
        _existsCharacter
            .CharacterExistsAsync(Arg.Any<ITransaction>(), Arg.Any<Guid>())
            .Returns(new EntityExistence(true, CurrentVersion));
        _characterWriter
            .UpdateAsync(Arg.Any<ITransaction>(), Arg.Any<Guid>(), Arg.Any<LoreWeave.Domain.Entities.Characters.Commands.UpdateCharacter>())
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
