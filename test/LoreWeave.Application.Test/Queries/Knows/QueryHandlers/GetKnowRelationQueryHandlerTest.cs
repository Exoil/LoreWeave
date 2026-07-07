using NSubstitute;
using NSubstitute.ExceptionExtensions;

using LoreWeave.Application.Models;
using LoreWeave.Application.Queries.Knows;
using LoreWeave.Application.Queries.Knows.QueryHandlers;
using LoreWeave.Domain.Entities.Knows;
using LoreWeave.Domain.Exceptions;
using LoreWeave.Domain.Extensions;
using LoreWeave.Domain.Transactions;
using LoreWeave.Domain.Models;
using LoreWeave.Domain.Repositories.Knows;

using Serilog;

using Shouldly;

namespace LoreWeave.Application.Test.Queries.Knows.QueryHandlers;

public class GetKnowRelationQueryHandlerTest
{
    private static readonly Guid BoardId = Guid.NewGuid();

    private readonly IExistsKnowRelation _existsKnowRelation;
    private readonly IKnowRelationReader _knowRelationReader;
    private readonly GetKnowRelationQueryHandler _sut;

    private static readonly Guid _fromGuid = Guid.NewGuid();
    private static readonly Guid _toGuid = Guid.NewGuid();

    public GetKnowRelationQueryHandlerTest()
    {
        _existsKnowRelation = Substitute.For<IExistsKnowRelation>();
        _knowRelationReader = Substitute.For<IKnowRelationReader>();
        var logger = Substitute.For<ILogger>();
        var transaction = Substitute.For<ITransaction>();
        var transactionFactory = Substitute.For<ITransactionFactory>();
        transactionFactory.CreateAsync().Returns(transaction);

        _sut = new GetKnowRelationQueryHandler(transactionFactory, _existsKnowRelation, _knowRelationReader, logger);
    }

    [Fact]
    [Trait(Constants.TraitName, Constants.TestTitle)]
    public async Task InvokeAsync_WhenRelationExists_ReturnsKnowRelationPayload()
    {
        // Arrange
        var query = new GetKnowRelationQuery(BoardId, _fromGuid, _toGuid);
        var knowRelation = new KnowRelation(
            Guid.CreateVersion7(),
            "Knows well",
            isStrongRelation: true,
            _fromGuid,
            _toGuid,
            version: 3);

        _existsKnowRelation
            .KnowRelationExistsAsync(Arg.Any<ITransaction>(), BoardId, _fromGuid, _toGuid)
            .Returns(new EntityExistence(true, knowRelation.Version));
        _knowRelationReader
            .GetKnowRelationAsync(Arg.Any<ITransaction>(), BoardId, _fromGuid, _toGuid)
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
        var query = new GetKnowRelationQuery(BoardId, _fromGuid, _toGuid);
        _existsKnowRelation
            .KnowRelationExistsAsync(Arg.Any<ITransaction>(), BoardId, _fromGuid, _toGuid)
            .Returns(new EntityExistence(false, 0));

        // Act
        var result = await _sut.InvokeAsync(query);

        // Assert
        result.IsSuccess.ShouldBeFalse("Query should fail when relation does not exist");
        result.Error.ShouldBeOfType<NotFoundException>("Error should be NotFoundException");

        await _knowRelationReader
            .DidNotReceive()
            .GetKnowRelationAsync(Arg.Any<ITransaction>(), Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<Guid>());
    }

    [Fact]
    [Trait(Constants.TraitName, Constants.TestTitle)]
    public async Task InvokeAsync_WhenRepositoryThrows_ReturnsException()
    {
        // Arrange
        var query = new GetKnowRelationQuery(BoardId, _fromGuid, _toGuid);
        var expectedException = new Exception("DB error");
        _existsKnowRelation
            .KnowRelationExistsAsync(Arg.Any<ITransaction>(), BoardId, _fromGuid, _toGuid)
            .Returns(new EntityExistence(true, 1));
        _knowRelationReader
            .GetKnowRelationAsync(Arg.Any<ITransaction>(), BoardId, _fromGuid, _toGuid)
            .ThrowsAsync(expectedException);

        // Act
        var result = await _sut.InvokeAsync(query);

        // Assert
        result.IsSuccess.ShouldBeFalse("Result should be failure when repository throws");
        result.Error.ShouldBe(expectedException, "Error should be the thrown exception");
    }
}
