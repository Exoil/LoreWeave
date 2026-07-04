using NSubstitute;
using NSubstitute.ExceptionExtensions;

using LoreWeave.Application.Models;
using LoreWeave.Application.Queries.Characters;
using LoreWeave.Application.Queries.Characters.QueryHandlers;
using LoreWeave.Domain.Entities.Characters.Queries;
using LoreWeave.Domain.Extensions;
using LoreWeave.Domain.Transactions;
using LoreWeave.Domain.Models;
using LoreWeave.Domain.Repositories.Characters;

using Serilog;

using Shouldly;

namespace LoreWeave.Application.Test.Queries.Characters.QueryHandlers;

public class GetCharacterPageQueryHandlerTest
{
    private readonly ICharacterReader _characterReader;
    private readonly ILogger _logger;
    private readonly ITransaction _transaction;
    private readonly ITransactionFactory _transactionFactory;
    private readonly GetCharacterPageQueryHandler _sut;

    public GetCharacterPageQueryHandlerTest()
    {
        _characterReader = Substitute.For<ICharacterReader>();
        _logger = Substitute.For<ILogger>();
        _transaction = Substitute.For<ITransaction>();
        _transactionFactory = Substitute.For<ITransactionFactory>();
        _transactionFactory.CreateAsync().Returns(_transaction);

        _sut = new GetCharacterPageQueryHandler(_transactionFactory, _characterReader, _logger);
    }

    [Fact]
    [Trait(Constants.TraitName, Constants.TestTitle)]
    public async Task InvokeAsync_WhenCharactersExist_ReturnsPageWithRelations()
    {
        // Arrange
        var query = new GetCharacterPageQuery(1, 10, "Name", "Asc", null);
        var knownCharacterId = Guid.CreateVersion7();
        var characters = new List<CharacterWithKnowRelation>
        {
            new(Guid.CreateVersion7(), "CharacterA",
                new List<KnowRelationDetail>
                {
                    new(knownCharacterId, "Childhood friends", true)
                }.AsReadOnly()),
            new(Guid.CreateVersion7(), "CharacterB", new List<KnowRelationDetail>().AsReadOnly())
        }.AsReadOnly();

        _characterReader
            .GetPageAsync(Arg.Any<ITransaction>(), Arg.Any<GetCharacterPage>(), Arg.Any<CharacterSearchFilter>())
            .Returns(characters);

        // Act
        var result = await _sut.InvokeAsync(query);

        // Assert
        result.IsSuccess.ShouldBeTrue("Query should succeed when characters are found");
        result.Value.ShouldBeAssignableTo<IReadOnlyCollection<CharacterPayloadWithRelations>>("Result should be a read-only collection");
        result.Value.Count.ShouldBe(2, "Result should contain 2 characters");
        result.Value.First().Name.ShouldBe("CharacterA", "First character name should match");
        result.Value.First().KnowCharacters.Count.ShouldBe(1, "First character should have 1 relation");

        var relation = result.Value.First().KnowCharacters.First();
        relation.CharacterId.ShouldBe(knownCharacterId, "Relation should point to the known character");
        relation.Description.ShouldBe("Childhood friends", "Relation description should match");
        relation.IsStrongRelation.ShouldBeTrue("Relation should be marked as strong");

        result.Value.Last().KnowCharacters.ShouldBeEmpty("Second character should have no relations");
    }

    [Fact]
    [Trait(Constants.TraitName, Constants.TestTitle)]
    public async Task InvokeAsync_WhenNoCharactersExist_ReturnsEmptyCollection()
    {
        // Arrange
        var query = new GetCharacterPageQuery(1, 10, "Name", "Asc", null);
        _characterReader
            .GetPageAsync(Arg.Any<ITransaction>(), Arg.Any<GetCharacterPage>(), Arg.Any<CharacterSearchFilter>())
            .Returns(new List<CharacterWithKnowRelation>().AsReadOnly());

        // Act
        var result = await _sut.InvokeAsync(query);

        // Assert
        result.IsSuccess.ShouldBeTrue("Query should succeed even when no characters are found");
        result.Value.ShouldBeEmpty("Result should be empty when no characters exist");
    }

    [Fact]
    [Trait(Constants.TraitName, Constants.TestTitle)]
    public async Task InvokeAsync_WhenRepositoryThrows_ReturnsException()
    {
        // Arrange
        var query = new GetCharacterPageQuery(1, 10, "Name", "Asc", null);
        var expectedException = new Exception("DB error");
        _characterReader
            .GetPageAsync(Arg.Any<ITransaction>(), Arg.Any<GetCharacterPage>(), Arg.Any<CharacterSearchFilter>())
            .ThrowsAsync(expectedException);

        // Act
        var result = await _sut.InvokeAsync(query);

        // Assert
        result.IsSuccess.ShouldBeFalse("Result should be failure when repository throws");
        result.Error.ShouldBe(expectedException, "Error should be the thrown exception");
    }
}
