using Neo4j.Driver;

using NSubstitute;
using NSubstitute.ExceptionExtensions;

using LoreWeave.Application.Models;
using LoreWeave.Application.Queries;
using LoreWeave.Application.Queries.QueryHandlers;
using LoreWeave.Domain.Entities.Facts;
using LoreWeave.Domain.Entities.Facts.Commands;
using LoreWeave.Domain.Exceptions;
using LoreWeave.Domain.Factories;
using LoreWeave.Domain.Models;
using LoreWeave.Domain.Repositories;

using Serilog;

using Shouldly;

namespace LoreWeave.Application.Test.Queries.QueryHandlers;

public class GetFactByIdQueryHandlerTest
{
    private readonly ICharacterRepository _characterRepository;
    private readonly GetFactByIdQueryHandler _sut;

    private static readonly Guid _factGuid = Guid.NewGuid();

    public GetFactByIdQueryHandlerTest()
    {
        _characterRepository = Substitute.For<ICharacterRepository>();
        var logger = Substitute.For<ILogger>();
        var transaction = Substitute.For<IAsyncTransaction>();
        var transactionFactory = Substitute.For<ITransactionFactory<IAsyncTransaction>>();
        transactionFactory.CreateAsync().Returns(transaction);

        _sut = new GetFactByIdQueryHandler(transactionFactory, _characterRepository, logger);
    }

    [Fact]
    [Trait(Constants.TraitName, Constants.TestTitle)]
    public async Task InvokeAsync_WhenFactExists_ReturnsFactPayload()
    {
        // Arrange
        var query = new GetFactByIdQuery(_factGuid);
        var fact = new Fact(new CreateFact(_factGuid, "TestTitle", "TestContent"), version: 2);
        _characterRepository
            .FactExistsAsync(Arg.Any<IAsyncTransaction>(), _factGuid)
            .Returns(new EntityExistence(true, fact.Version));
        _characterRepository
            .GetFactAsync(Arg.Any<IAsyncTransaction>(), _factGuid)
            .Returns(fact);

        // Act
        var result = await _sut.InvokeAsync(query);

        // Assert
        result.IsSuccess.ShouldBeTrue("Query should succeed when fact exists");
        result.Value.ShouldBeOfType<FactPayload>("Result value should be FactPayload");
        result.Value.Id.ShouldBe(_factGuid, "Returned Id should match the requested Id");
        result.Value.Title.ShouldBe("TestTitle", "Returned Title should match the fact title");
        result.Value.Content.ShouldBe("TestContent", "Returned Content should match the fact content");
        result.Value.Version.ShouldBe((ushort)2, "Returned Version should match the fact version");
        result.Value.Etag.ShouldBe("\"2\"", "Etag should wrap the version");
    }

    [Fact]
    [Trait(Constants.TraitName, Constants.TestTitle)]
    public async Task InvokeAsync_WhenFactDoesNotExist_ReturnsNotFoundException()
    {
        // Arrange
        var query = new GetFactByIdQuery(_factGuid);
        _characterRepository
            .FactExistsAsync(Arg.Any<IAsyncTransaction>(), _factGuid)
            .Returns(new EntityExistence(false, 0));

        // Act
        var result = await _sut.InvokeAsync(query);

        // Assert
        result.IsSuccess.ShouldBeFalse("Query should fail when fact does not exist");
        result.Error.ShouldBeOfType<NotFoundException>("Error should be NotFoundException");
    }

    [Fact]
    [Trait(Constants.TraitName, Constants.TestTitle)]
    public async Task InvokeAsync_WhenRepositoryThrows_ReturnsException()
    {
        // Arrange
        var query = new GetFactByIdQuery(_factGuid);
        var expectedException = new Exception("DB error");
        _characterRepository
            .FactExistsAsync(Arg.Any<IAsyncTransaction>(), _factGuid)
            .Returns(new EntityExistence(true, 1));
        _characterRepository
            .GetFactAsync(Arg.Any<IAsyncTransaction>(), _factGuid)
            .ThrowsAsync(expectedException);

        // Act
        var result = await _sut.InvokeAsync(query);

        // Assert
        result.IsSuccess.ShouldBeFalse("Result should be failure when repository throws");
        result.Error.ShouldBe(expectedException, "Error should be the thrown exception");
    }
}