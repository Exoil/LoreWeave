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

public class CreateFactCommandHandlerTest
{
    private readonly ICharacterRepository _characterRepository;
    private readonly IAsyncTransaction _transaction;
    private readonly CreateFactCommandHandler _sut;

    private static readonly Guid _characterId = Guid.CreateVersion7();

    public CreateFactCommandHandlerTest()
    {
        _characterRepository = Substitute.For<ICharacterRepository>();
        var logger = Substitute.For<ILogger>();
        _transaction = Substitute.For<IAsyncTransaction>();
        var transactionFactory = Substitute.For<ITransactionFactory<IAsyncTransaction>>();
        transactionFactory.CreateAsync().Returns(_transaction);

        _sut = new CreateFactCommandHandler(_characterRepository, logger, transactionFactory);
    }

    [Fact]
    [Trait(Constants.TraitName, Constants.TestTitle)]
    public async Task InvokeAsync_WhenCharacterExists_ReturnsGuidAndCommits()
    {
        // Arrange
        const string title = "The Broken Crown";
        const string content = "A relic lost in the northern wastes.";
        var command = new CreateFactCommand(_characterId, title, content);
        _characterRepository
            .CharacterExistsAsync(Arg.Any<IAsyncTransaction>(), _characterId)
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
        var command = new CreateFactCommand(_characterId, title, content);
        _characterRepository
            .CharacterExistsAsync(Arg.Any<IAsyncTransaction>(), _characterId)
            .Returns(new EntityExistence(true, 1));

        // Act
        var result = await _sut.InvokeAsync(command);

        // Assert
        result.IsSuccess.ShouldBeTrue("Fact creation should succeed when the character exists");
        await _characterRepository
            .Received(1)
            .CreateAsync(
                Arg.Any<IAsyncTransaction>(),
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
        var command = new CreateFactCommand(_characterId, "Title", "Content");
        _characterRepository
            .CharacterExistsAsync(Arg.Any<IAsyncTransaction>(), _characterId)
            .Returns(new EntityExistence(false, -1));

        // Act
        var result = await _sut.InvokeAsync(command);

        // Assert
        result.IsSuccess.ShouldBeFalse("Fact creation should fail when the character does not exist");
        result.Error.ShouldBeOfType<NotFoundException>("Error should be NotFoundException");
        await _characterRepository
            .DidNotReceive()
            .CreateAsync(
                Arg.Any<IAsyncTransaction>(),
                Arg.Any<Guid>(),
                Arg.Any<LoreWeave.Domain.Entities.Facts.Commands.CreateFact>());
        await _transaction.DidNotReceive().CommitAsync();
    }

    [Fact]
    [Trait(Constants.TraitName, Constants.TestTitle)]
    public async Task InvokeAsync_WhenRepositoryThrows_ReturnsExceptionAndRollsBack()
    {
        // Arrange
        var command = new CreateFactCommand(_characterId, "Title", "Content");
        var expectedException = new Exception("DB error");
        _characterRepository
            .CharacterExistsAsync(Arg.Any<IAsyncTransaction>(), _characterId)
            .Returns(new EntityExistence(true, 1));
        _characterRepository
            .CreateAsync(
                Arg.Any<IAsyncTransaction>(),
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
        var command = new CreateFactCommand(_characterId, new string('*', 101), "Content");
        _characterRepository
            .CharacterExistsAsync(Arg.Any<IAsyncTransaction>(), _characterId)
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