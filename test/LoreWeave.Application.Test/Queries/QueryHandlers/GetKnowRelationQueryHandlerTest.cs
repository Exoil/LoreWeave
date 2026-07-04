using Neo4j.Driver;

using NSubstitute;
using NSubstitute.ExceptionExtensions;

using LoreWeave.Application.Models;
using LoreWeave.Application.Queries;
using LoreWeave.Application.Queries.QueryHandlers;
using LoreWeave.Domain.Entities.Knows;
using LoreWeave.Domain.Exceptions;
using LoreWeave.Domain.Extensions;
using LoreWeave.Domain.Factories;
using LoreWeave.Domain.Models;
using LoreWeave.Domain.Repositories;

using Serilog;

using Shouldly;

namespace LoreWeave.Application.Test.Queries.QueryHandlers;

public class GetKnowRelationQueryHandlerTest
{
    private readonly ICharacterRepository _characterRepository;
    private readonly GetKnowRelationQueryHandler _sut;

    private static readonly Guid _fromGuid = Guid.NewGuid();
    private static readonly Guid _toGuid = Guid.NewGuid();

    public GetKnowRelationQueryHandlerTest()
    {
        _characterRepository = Substitute.For<ICharacterRepository>();
        var logger = Substitute.For<ILogger>();
        var transaction = Substitute.For<IAsyncTransaction>();
        var transactionFactory = Substitute.For<ITransactionFactory<IAsyncTransaction>>();
        transactionFactory.CreateAsync().Returns(transaction);

        _sut = new GetKnowRelationQueryHandler(transactionFactory, _characterRepository, logger);
    }

    [Fact]
    [Trait(Constants.TraitName, Constants.TestTitle)]
    public async Task InvokeAsync_WhenRelationExists_ReturnsKnowRelationPayload()
    {
        // Arrange
        var query = new GetKnowRelationQuery(_fromGuid, _toGuid);
        var knowRelation = new KnowRelation(
            Guid.CreateVersion7(),
            "Knows well",
            isStrongRelation: true,
            _fromGuid,
            _toGuid,
            version: 3);

        _characterRepository
            .KnowRelationExistsAsync(Arg.Any<IAsyncTransaction>(), _fromGuid, _toGuid)
            .Returns(new EntityExistence(true, knowRelation.Version));
        _characterRepository
            .GetKnowRelationAsync(Arg.Any<IAsyncTransaction>(), _fromGuid, _toGuid)
            .Returns(knowRelation);

        // Act
        var result = await _sut.InvokeAsync(query);

        // Assert
        result.IsSuccess.ShouldBeTrue("Query should succeed when relation exists");
        result.Value.ShouldBeOfType<KnowRelationPayload>("Result value should be KnowRelationPayload");
        result.Value.FromCharacterId.ShouldBe(_fromGuid, "Returned FromCharacterId should match the request");
        result.Value.ToCharacterId.ShouldBe(_toGuid, "Returned ToCharacterId should match the request");
        result.Value.Description.ShouldBe("Knows well", "Returned Description should match the relation");
        result.Value.IsStrongRelation.ShouldBeTrue("Returned IsStrongRelation should match the relation");
        result.Value.Version.ShouldBe((ushort)3, "Returned Version should match the relation version");
        result.Value.Etag.ShouldBe("\"3\"", "Etag should wrap the version");
    }

    [Fact]
    [Trait(Constants.TraitName, Constants.TestTitle)]
    public async Task InvokeAsync_WhenRelationDoesNotExist_ReturnsNotFoundException()
    {
        // Arrange
        var query = new GetKnowRelationQuery(_fromGuid, _toGuid);
        _characterRepository
            .KnowRelationExistsAsync(Arg.Any<IAsyncTransaction>(), _fromGuid, _toGuid)
            .Returns(new EntityExistence(false, 0));

        // Act
        var result = await _sut.InvokeAsync(query);

        // Assert
        result.IsSuccess.ShouldBeFalse("Query should fail when relation does not exist");
        result.Error.ShouldBeOfType<NotFoundException>("Error should be NotFoundException");

        await _characterRepository
            .DidNotReceive()
            .GetKnowRelationAsync(Arg.Any<IAsyncTransaction>(), Arg.Any<Guid>(), Arg.Any<Guid>());
    }

    [Fact]
    [Trait(Constants.TraitName, Constants.TestTitle)]
    public async Task InvokeAsync_WhenRepositoryThrows_ReturnsException()
    {
        // Arrange
        var query = new GetKnowRelationQuery(_fromGuid, _toGuid);
        var expectedException = new Exception("DB error");
        _characterRepository
            .KnowRelationExistsAsync(Arg.Any<IAsyncTransaction>(), _fromGuid, _toGuid)
            .Returns(new EntityExistence(true, 1));
        _characterRepository
            .GetKnowRelationAsync(Arg.Any<IAsyncTransaction>(), _fromGuid, _toGuid)
            .ThrowsAsync(expectedException);

        // Act
        var result = await _sut.InvokeAsync(query);

        // Assert
        result.IsSuccess.ShouldBeFalse("Result should be failure when repository throws");
        result.Error.ShouldBe(expectedException, "Error should be the thrown exception");
    }
}
