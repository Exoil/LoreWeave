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

public class CreateFactCommandHandlerTest
{
    private static readonly Guid BoardId = Guid.NewGuid();

    private readonly IExistsCharacter _existsCharacter;
    private readonly IFactWriter _factWriter;
    private readonly ITransaction _transaction;
    private readonly CreateFactCommandHandler _sut;

    private static readonly Guid _characterId = Guid.CreateVersion7();

    public CreateFactCommandHandlerTest()
    {
        _existsCharacter = Substitute.For<IExistsCharacter>();
        _factWriter = Substitute.For<IFactWriter>();
        var logger = Substitute.For<ILogger>();
        _transaction = Substitute.For<ITransaction>();
        var transactionFactory = Substitute.For<ITransactionFactory>();
        transactionFactory.CreateAsync().Returns(_transaction);

        _sut = new CreateFactCommandHandler(transactionFactory, _existsCharacter, _factWriter, logger);
    }

    [Fact]
    [Trait(Constants.TraitName, Constants.TestTitle)]
    public async Task InvokeAsync_WhenCharacterExists_ReturnsGuidAndCommits()
    {
        // Arrange
        const string title = "The Broken Crown";
        const string content = "A relic lost in the northern wastes.";
        var command = new CreateFactCommand(BoardId, _characterId, title, content);
        _existsCharacter
            .CharacterExistsAsync(Arg.Any<ITransaction>(), BoardId, _characterId)
            .Returns(new EntityExistence(true, 1));

        // Act
        var result = await _sut.InvokeAsync(command);

        // Assert
        result.IsSuccess.ShouldBeTrue("Fact creation should succeed when the character exists");
        result.Value.ShouldNotBe(default(Guid), "Returned Guid should not be empty");
        await _transaction.Received(1).CommitAsync();
    }

    [Fact]
    [Trait(Constants.TraitName, Constants.TestTitle)]
    public async Task InvokeAsync_WhenCharacterExists_PassesTitleAndContentToRepository()
    {
        // Arrange
        const string title = "The Broken Crown";
        const string content = "A relic lost in the northern wastes.";
        var command = new CreateFactCommand(BoardId, _characterId, title, content);
        _existsCharacter
            .CharacterExistsAsync(Arg.Any<ITransaction>(), BoardId, _characterId)
            .Returns(new EntityExistence(true, 1));

        // Act
        var result = await _sut.InvokeAsync(command);

        // Assert
        result.IsSuccess.ShouldBeTrue("Fact creation should succeed when the character exists");
        await _factWriter
            .Received(1)
            .CreateAsync(
                Arg.Any<ITransaction>(),
                _characterId,
                Arg.Is<LoreWeave.Domain.Entities.Facts.Commands.CreateFact>(f =>
                    f.Title == title
                    && f.Content == content
                    && f.Id == result.Value));
    }

    [Fact]
    [Trait(Constants.TraitName, Constants.TestTitle)]
    public async Task InvokeAsync_WhenCharacterDoesNotExist_ReturnsNotFoundExceptionAndDoesNotPersist()
    {
        // Arrange
        var command = new CreateFactCommand(BoardId, _characterId, "Title", "Content");
        _existsCharacter
            .CharacterExistsAsync(Arg.Any<ITransaction>(), BoardId, _characterId)
            .Returns(new EntityExistence(false, 0));

        // Act
        var result = await _sut.InvokeAsync(command);

        // Assert
        result.IsSuccess.ShouldBeFalse("Fact creation should fail when the character does not exist");
        result.Error.ShouldBeOfType<NotFoundException>("Error should be NotFoundException");
        await _factWriter
            .DidNotReceive()
            .CreateAsync(
                Arg.Any<ITransaction>(),
                Arg.Any<Guid>(),
                Arg.Any<LoreWeave.Domain.Entities.Facts.Commands.CreateFact>());
        await _transaction.DidNotReceive().CommitAsync();
    }

    [Fact]
    [Trait(Constants.TraitName, Constants.TestTitle)]
    public async Task InvokeAsync_WhenRepositoryThrows_ReturnsExceptionAndRollsBack()
    {
        // Arrange
        var command = new CreateFactCommand(BoardId, _characterId, "Title", "Content");
        var expectedException = new Exception("DB error");
        _existsCharacter
            .CharacterExistsAsync(Arg.Any<ITransaction>(), BoardId, _characterId)
            .Returns(new EntityExistence(true, 1));
        _factWriter
            .CreateAsync(
                Arg.Any<ITransaction>(),
                _characterId,
                Arg.Any<LoreWeave.Domain.Entities.Facts.Commands.CreateFact>())
            .ThrowsAsync(expectedException);

        // Act
        var result = await _sut.InvokeAsync(command);

        // Assert
        result.IsSuccess.ShouldBeFalse("Result should be failure when repository throws");
        result.Error.ShouldBe(expectedException, "Error should be the thrown exception");
        await _transaction.Received(1).RollbackAsync();
        await _transaction.DidNotReceive().CommitAsync();
    }

    [Fact]
    [Trait(Constants.TraitName, Constants.TestTitle)]
    public async Task InvokeAsync_WhenTitleIsTooLong_ReturnsValueObjectExceptionAndRollsBack()
    {
        // Arrange
        var command = new CreateFactCommand(BoardId, _characterId, new string('*', 101), "Content");
        _existsCharacter
            .CharacterExistsAsync(Arg.Any<ITransaction>(), BoardId, _characterId)
            .Returns(new EntityExistence(true, 1));

        // Act
        var result = await _sut.InvokeAsync(command);

        // Assert
        result.IsSuccess.ShouldBeFalse("Fact creation should fail when the title is too long");
        result.Error.ShouldBeOfType<ValueObjectException>("Error should be ValueObjectException");
        await _transaction.Received(1).RollbackAsync();
        await _transaction.DidNotReceive().CommitAsync();
    }
}