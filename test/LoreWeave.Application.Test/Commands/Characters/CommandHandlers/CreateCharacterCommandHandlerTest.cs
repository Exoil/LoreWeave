using NSubstitute;
using NSubstitute.ExceptionExtensions;

using LoreWeave.Application.Commands.Characters;
using LoreWeave.Application.Commands.Characters.CommandHandlers;
using LoreWeave.Domain.Transactions;
using LoreWeave.Domain.Repositories.Characters;

using Serilog;

using Shouldly;

namespace LoreWeave.Application.Test.Commands.Characters.CommandHandlers;

public class CreateCharacterCommandHandlerTest
{
    private readonly ICharacterWriter _characterWriter;
    private readonly ITransaction _transaction;
    private readonly CreateCharacterCommandHandler _sut;

    public CreateCharacterCommandHandlerTest()
    {
        _characterWriter = Substitute.For<ICharacterWriter>();
        var logger = Substitute.For<ILogger>();
        _transaction = Substitute.For<ITransaction>();
        var transactionFactory = Substitute.For<ITransactionFactory>();
        transactionFactory.CreateAsync().Returns(_transaction);

        _sut = new CreateCharacterCommandHandler(transactionFactory, _characterWriter, logger);
    }

    [Fact]
    [Trait(Constants.TraitName, Constants.TestTitle)]
    public async Task InvokeAsync_WhenCharacterIsCreated_ReturnsGuid()
    {
        // Arrange
        var command = new CreateCharacterCommand(Guid.CreateVersion7(), "TestCharacter");

        // Act
        var result = await _sut.InvokeAsync(command);

        // Assert
        result.IsSuccess.ShouldBeTrue("Character creation should succeed");
        result.Value.ShouldBe(command.Id, "Returned Guid should match the command Id");
        await _transaction.Received(1).CommitAsync();
    }

    [Fact]
    [Trait(Constants.TraitName, Constants.TestTitle)]
    public async Task InvokeAsync_WhenRepositoryThrows_ReturnsExceptionAndRollsBack()
    {
        // Arrange
        var command = new CreateCharacterCommand(Guid.CreateVersion7(), "TestCharacter");
        var expectedException = new Exception("DB error");
        _characterWriter
            .CreateAsync(Arg.Any<ITransaction>(), Arg.Any<LoreWeave.Domain.Entities.Characters.Commands.CreateCharacter>())
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
