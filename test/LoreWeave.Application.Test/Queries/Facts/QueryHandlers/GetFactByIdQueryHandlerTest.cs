using NSubstitute;
using NSubstitute.ExceptionExtensions;

using LoreWeave.Application.Models;
using LoreWeave.Application.Queries.Facts;
using LoreWeave.Application.Queries.Facts.QueryHandlers;
using LoreWeave.Domain.Entities.Facts;
using LoreWeave.Domain.Entities.Facts.Commands;
using LoreWeave.Domain.Exceptions;
using LoreWeave.Domain.Transactions;
using LoreWeave.Domain.Models;
using LoreWeave.Domain.Repositories.Facts;

using Serilog;

using Shouldly;

namespace LoreWeave.Application.Test.Queries.Facts.QueryHandlers;

public class GetFactByIdQueryHandlerTest
{
    private static readonly Guid BoardId = Guid.NewGuid();

    private readonly IExistsFact _existsFact;
    private readonly IFactReader _factReader;
    private readonly GetFactByIdQueryHandler _sut;

    private static readonly Guid _factGuid = Guid.NewGuid();

    public GetFactByIdQueryHandlerTest()
    {
        _existsFact = Substitute.For<IExistsFact>();
        _factReader = Substitute.For<IFactReader>();
        var logger = Substitute.For<ILogger>();
        var transaction = Substitute.For<ITransaction>();
        var transactionFactory = Substitute.For<ITransactionFactory>();
        transactionFactory.CreateAsync().Returns(transaction);

        _sut = new GetFactByIdQueryHandler(transactionFactory, _existsFact, _factReader, logger);
    }

    [Fact]
    [Trait(Constants.TraitName, Constants.TestTitle)]
    public async Task InvokeAsync_WhenFactExists_ReturnsFactPayload()
    {
        // Arrange
        var query = new GetFactByIdQuery(BoardId, _factGuid);
        var fact = new Fact(new CreateFact(_factGuid, "TestTitle", "TestContent"), version: 2);
        _existsFact
            .FactExistsAsync(Arg.Any<ITransaction>(), BoardId, _factGuid)
            .Returns(new EntityExistence(true, fact.Version));
        _factReader
            .GetFactAsync(Arg.Any<ITransaction>(), BoardId, _factGuid)
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
        var query = new GetFactByIdQuery(BoardId, _factGuid);
        _existsFact
            .FactExistsAsync(Arg.Any<ITransaction>(), BoardId, _factGuid)
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
        var query = new GetFactByIdQuery(BoardId, _factGuid);
        var expectedException = new Exception("DB error");
        _existsFact
            .FactExistsAsync(Arg.Any<ITransaction>(), BoardId, _factGuid)
            .Returns(new EntityExistence(true, 1));
        _factReader
            .GetFactAsync(Arg.Any<ITransaction>(), BoardId, _factGuid)
            .ThrowsAsync(expectedException);

        // Act
        var result = await _sut.InvokeAsync(query);

        // Assert
        result.IsSuccess.ShouldBeFalse("Result should be failure when repository throws");
        result.Error.ShouldBe(expectedException, "Error should be the thrown exception");
    }
}