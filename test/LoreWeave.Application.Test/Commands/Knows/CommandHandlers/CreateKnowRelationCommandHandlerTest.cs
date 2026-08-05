using NSubstitute;
using NSubstitute.ExceptionExtensions;

using LoreWeave.Application.Commands.Knows;
using LoreWeave.Application.Commands.Knows.CommandHandlers;
using LoreWeave.Domain.Exceptions;
using LoreWeave.Domain.Transactions;
using LoreWeave.Domain.Models;
using LoreWeave.Domain.Repositories.Characters;
using LoreWeave.Domain.Repositories.Knows;

using Serilog;

using Shouldly;

namespace LoreWeave.Application.Test.Commands.Knows.CommandHandlers;

public class CreateKnowRelationCommandHandlerTest
{
    private static readonly Guid BoardId = Guid.NewGuid();

    private readonly IExistsCharacter _existsCharacter;
    private readonly IKnowRelationWriter _knowRelationWriter;
    private readonly ITransaction _transaction;
    private readonly CreateKnowRelationCommandHandler _sut;

    private static readonly Guid _fromCharacterId = Guid.CreateVersion7();
    private static readonly Guid _toCharacterId = Guid.CreateVersion7();

    public CreateKnowRelationCommandHandlerTest()
    {
        _existsCharacter = Substitute.For<IExistsCharacter>();
        _knowRelationWriter = Substitute.For<IKnowRelationWriter>();
        var logger = Substitute.For<ILogger>();
        _transaction = Substitute.For<ITransaction>();
        var transactionFactory = Substitute.For<ITransactionFactory>();
        transactionFactory.CreateAsync().Returns(_transaction);

        _sut = new CreateKnowRelationCommandHandler(transactionFactory, _existsCharacter, _knowRelationWriter, logger);
    }

    [Fact]
    [Trait(Constants.TraitName, Constants.TestTitle)]
    public async Task InvokeAsync_WhenBothCharactersExist_ReturnsGuid()
    {
        // Arrange
        const string description = "They know each other";
        var command = new CreateKnowRelationCommand(BoardId, _fromCharacterId, _toCharacterId, description, true);
        _existsCharacter
            .CharacterExistsAsync(Arg.Any<ITransaction>(), BoardId, _fromCharacterId)
            .Returns(new EntityExistence(true, 1));
        _existsCharacter
            .CharacterExistsAsync(Arg.Any<ITransaction>(), BoardId, _toCharacterId)
            .Returns(new EntityExistence(true, 1));

        // Act
        var result = await _sut.InvokeAsync(command);

        // Assert
        result.IsSuccess.ShouldBeTrue("Know relation creation should succeed when both characters exist");
        result.Value.ShouldNotBe(default(Guid), "Returned Guid should not be empty");
        await _transaction.Received(1).CommitAsync();
        await _knowRelationWriter
            .Received(1)
            .CreateKnowRelationAsync(
                Arg.Any<ITransaction>(),
                Arg.Is<LoreWeave.Domain.Entities.Knows.Commands.CreateKnowRelation>(r =>
                    r!.IsStrongRelation
                    && r.Description == description
                    && r.FromCharacterId == _fromCharacterId
                    && r.ToCharacterId == _toCharacterId));
    }

    [Fact]
    [Trait(Constants.TraitName, Constants.TestTitle)]
    public async Task InvokeAsync_WhenBothCharactersExist_PassesDescriptionToRepository()
    {
        // Arrange
        const string description = "A long-standing friendship";
        var command = new CreateKnowRelationCommand(BoardId, _fromCharacterId, _toCharacterId, description, true);
        _existsCharacter
            .CharacterExistsAsync(Arg.Any<ITransaction>(), BoardId, _fromCharacterId)
            .Returns(new EntityExistence(true, 1));
        _existsCharacter
            .CharacterExistsAsync(Arg.Any<ITransaction>(), BoardId, _toCharacterId)
            .Returns(new EntityExistence(true, 1));

        // Act
        var result = await _sut.InvokeAsync(command);

        // Assert
        result.IsSuccess.ShouldBeTrue("Know relation creation should succeed when both characters exist");
        await _knowRelationWriter
            .Received(1)
            .CreateKnowRelationAsync(
                Arg.Any<ITransaction>(),
                Arg.Is<LoreWeave.Domain.Entities.Knows.Commands.CreateKnowRelation>(r => r!.Description == description));
    }

    [Fact]
    [Trait(Constants.TraitName, Constants.TestTitle)]
    public async Task InvokeAsync_WhenRelationIsNotStrong_PassesIsStrongRelationFalseToRepository()
    {
        // Arrange
        var command = new CreateKnowRelationCommand(BoardId, _fromCharacterId, _toCharacterId, "They know each other", false);
        _existsCharacter
            .CharacterExistsAsync(Arg.Any<ITransaction>(), BoardId, _fromCharacterId)
            .Returns(new EntityExistence(true, 1));
        _existsCharacter
            .CharacterExistsAsync(Arg.Any<ITransaction>(), BoardId, _toCharacterId)
            .Returns(new EntityExistence(true, 1));

        // Act
        var result = await _sut.InvokeAsync(command);

        // Assert
        result.IsSuccess.ShouldBeTrue("Know relation creation should succeed when both characters exist");
        await _knowRelationWriter
            .Received(1)
            .CreateKnowRelationAsync(
                Arg.Any<ITransaction>(),
                Arg.Is<LoreWeave.Domain.Entities.Knows.Commands.CreateKnowRelation>(r => !r!.IsStrongRelation));
    }

    [Fact]
    [Trait(Constants.TraitName, Constants.TestTitle)]
    public async Task InvokeAsync_WhenFromCharacterDoesNotExist_ReturnsUnprocessableContentException()
    {
        // Arrange
        var command = new CreateKnowRelationCommand(BoardId, _fromCharacterId, _toCharacterId, "They know each other", true);
        _existsCharacter
            .CharacterExistsAsync(Arg.Any<ITransaction>(), BoardId, _fromCharacterId)
            .Returns(new EntityExistence(false, 0));

        // Act
        var result = await _sut.InvokeAsync(command);

        // Assert
        result.IsSuccess.ShouldBeFalse("Know relation creation should fail when from character does not exist");
        result.Error.ShouldBeOfType<UnprocessableContentException>("Error should be UnprocessableContentException");
    }

    [Fact]
    [Trait(Constants.TraitName, Constants.TestTitle)]
    public async Task InvokeAsync_WhenToCharacterDoesNotExist_ReturnsUnprocessableContentException()
    {
        // Arrange
        var command = new CreateKnowRelationCommand(BoardId, _fromCharacterId, _toCharacterId, "They know each other", true);
        _existsCharacter
            .CharacterExistsAsync(Arg.Any<ITransaction>(), BoardId, _fromCharacterId)
            .Returns(new EntityExistence(true, 1));
        _existsCharacter
            .CharacterExistsAsync(Arg.Any<ITransaction>(), BoardId, _toCharacterId)
            .Returns(new EntityExistence(false, 0));

        // Act
        var result = await _sut.InvokeAsync(command);

        // Assert
        result.IsSuccess.ShouldBeFalse("Know relation creation should fail when to character does not exist");
        result.Error.ShouldBeOfType<UnprocessableContentException>("Error should be UnprocessableContentException");
    }

    [Fact]
    [Trait(Constants.TraitName, Constants.TestTitle)]
    public async Task InvokeAsync_WhenRepositoryThrows_ReturnsExceptionAndRollsBack()
    {
        // Arrange
        var command = new CreateKnowRelationCommand(BoardId, _fromCharacterId, _toCharacterId, "They know each other", true);
        var expectedException = new Exception("DB error");
        _existsCharacter
            .CharacterExistsAsync(Arg.Any<ITransaction>(), BoardId, _fromCharacterId)
            .Returns(new EntityExistence(true, 1));
        _existsCharacter
            .CharacterExistsAsync(Arg.Any<ITransaction>(), BoardId, _toCharacterId)
            .Returns(new EntityExistence(true, 1));
        _knowRelationWriter
            .CreateKnowRelationAsync(Arg.Any<ITransaction>(), Arg.Any<LoreWeave.Domain.Entities.Knows.Commands.CreateKnowRelation>())
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
