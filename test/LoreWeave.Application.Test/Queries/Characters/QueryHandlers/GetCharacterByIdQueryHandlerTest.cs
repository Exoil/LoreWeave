using NSubstitute;
using NSubstitute.ExceptionExtensions;

using LoreWeave.Application.Models;
using LoreWeave.Application.Queries.Characters;
using LoreWeave.Application.Queries.Characters.QueryHandlers;
using LoreWeave.Domain.Entities.Characters;
using LoreWeave.Domain.Entities.Characters.Commands;
using LoreWeave.Domain.Exceptions;
using LoreWeave.Domain.Extensions;
using LoreWeave.Domain.Transactions;
using LoreWeave.Domain.Models;
using LoreWeave.Domain.Repositories.Characters;

using Serilog;

using Shouldly;

namespace LoreWeave.Application.Test.Queries.Characters.QueryHandlers;

public class GetCharacterByIdQueryHandlerTest
{
    private readonly IExistsCharacter _existsCharacter;
    private readonly ICharacterReader _characterReader;
    private readonly GetCharacterByIdQueryHandler _sut;

    private static readonly Guid _characterGuid = Guid.NewGuid();

    public GetCharacterByIdQueryHandlerTest()
    {
        _existsCharacter = Substitute.For<IExistsCharacter>();
        _characterReader = Substitute.For<ICharacterReader>();
        var logger = Substitute.For<ILogger>();
        var transaction = Substitute.For<ITransaction>();
        var transactionFactory = Substitute.For<ITransactionFactory>();
        transactionFactory.CreateAsync().Returns(transaction);

        _sut = new GetCharacterByIdQueryHandler(transactionFactory, _existsCharacter, _characterReader, logger);
    }

    [Fact]
    [Trait(Constants.TraitName, Constants.TestTitle)]
    public async Task InvokeAsync_WhenCharacterExists_ReturnsCharacterPayload()
    {
        // Arrange
        var query = new GetCharacterByIdQuery(_characterGuid);
        var character = new Character(new CreateCharacter(_characterGuid, "TestCharacter"), version: 2);
        _existsCharacter
            .CharacterExistsAsync(Arg.Any<ITransaction>(), _characterGuid)
            .Returns(new EntityExistence(true, character.Version));
        _characterReader
            .GetAsync(Arg.Any<ITransaction>(), _characterGuid)
            .Returns(character);

        // Act
        var result = await _sut.InvokeAsync(query);

        // Assert
        result.IsSuccess.ShouldBeTrue("Query should succeed when character exists");
        result.Value.ShouldBeOfType<CharacterPayload>("Result value should be CharacterPayload");
        result.Value.Id.ShouldBe(_characterGuid, "Returned Id should match the requested Id");
        result.Value.Name.ShouldBe("TestCharacter", "Returned Name should match the character name");
        result.Value.Version.ShouldBe((ushort)2, "Returned Version should match the character version");
    }

    [Fact]
    [Trait(Constants.TraitName, Constants.TestTitle)]
    public async Task InvokeAsync_WhenCharacterDoesNotExist_ReturnsNotFoundException()
    {
        // Arrange
        var query = new GetCharacterByIdQuery(_characterGuid);
        _existsCharacter
            .CharacterExistsAsync(Arg.Any<ITransaction>(), _characterGuid)
            .Returns(new EntityExistence(false, 0));

        // Act
        var result = await _sut.InvokeAsync(query);

        // Assert
        result.IsSuccess.ShouldBeFalse("Query should fail when character does not exist");
        result.Error.ShouldBeOfType<NotFoundException>("Error should be NotFoundException");
    }

    [Fact]
    [Trait(Constants.TraitName, Constants.TestTitle)]
    public async Task InvokeAsync_WhenRepositoryThrows_ReturnsException()
    {
        // Arrange
        var query = new GetCharacterByIdQuery(_characterGuid);
        var expectedException = new Exception("DB error");
        _existsCharacter
            .CharacterExistsAsync(Arg.Any<ITransaction>(), _characterGuid)
            .Returns(new EntityExistence(true, 1));
        _characterReader
            .GetAsync(Arg.Any<ITransaction>(), _characterGuid)
            .ThrowsAsync(expectedException);

        // Act
        var result = await _sut.InvokeAsync(query);

        // Assert
        result.IsSuccess.ShouldBeFalse("Result should be failure when repository throws");
        result.Error.ShouldBe(expectedException, "Error should be the thrown exception");
    }
}
