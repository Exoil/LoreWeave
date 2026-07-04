using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Neo4j.Driver;

using NSubstitute;
using NSubstitute.ExceptionExtensions;

using LoreWeave.Application.Models;
using LoreWeave.Application.Queries;
using LoreWeave.Application.Queries.QueryHandlers;
using LoreWeave.Domain.Exceptions;
using LoreWeave.Domain.Extensions;
using LoreWeave.Domain.Factories;
using LoreWeave.Domain.Models;
using LoreWeave.Domain.Repositories;

using Shouldly;

using ILogger = Serilog.ILogger;

namespace LoreWeave.Application.Test.Queries.QueryHandlers;

public class FindRelationBetweenCharacterQueryHandlerTest
{
    private readonly ICharacterRepository _characterRepository;
    private readonly FindRelationBetweenCharacterQueryHandler _sut;

    private static readonly Guid _fromCharacterGuid = Guid.NewGuid();
    private static readonly Guid _toCharacterGuid = Guid.NewGuid();

    public FindRelationBetweenCharacterQueryHandlerTest()
    {
        _characterRepository = Substitute.For<ICharacterRepository>();
        var logger = Substitute.For<ILogger>();
        var transaction = Substitute.For<IAsyncTransaction>();
        var transactionFactory = Substitute.For<ITransactionFactory<IAsyncTransaction>>();
        transactionFactory.CreateAsync().Returns(transaction);

        _sut = new FindRelationBetweenCharacterQueryHandler(_characterRepository, logger, transactionFactory);
    }

    [Fact]
    [Trait(Constants.TraitName, Constants.TestTitle)]
    public async Task InvokeAsync_WhenPathExists_ReturnsRelationPathPayload()
    {
        // Arrange
        var middleGuid = Guid.CreateVersion7();
        var query = new FindRelationBetweenCharacterQuery(_fromCharacterGuid, _toCharacterGuid, 10);

        _characterRepository
            .CharacterExistsAsync(Arg.Any<IAsyncTransaction>(), _fromCharacterGuid)
            .Returns(new EntityExistence(true, 1));
        _characterRepository
            .CharacterExistsAsync(Arg.Any<IAsyncTransaction>(), _toCharacterGuid)
            .Returns(new EntityExistence(true, 1));
        _characterRepository
            .FindPathBetweenCharactersAsync(
                Arg.Any<IAsyncTransaction>(), _fromCharacterGuid, _toCharacterGuid, 10)
            .Returns(new List<Guid> { _fromCharacterGuid, middleGuid, _toCharacterGuid }.AsReadOnly());

        // Act
        var result = await _sut.InvokeAsync(query);

        // Assert
        result.IsSuccess.ShouldBeTrue("Query should succeed when path exists");
        result.Value.ShouldBeOfType<RelationPathPayload>("Result value should be RelationPathPayload");
        result.Value.CharacterIds.Count.ShouldBe(3, "Path should contain 3 characters");
        result.Value.Hops.ShouldBe(2, "Path should have 2 hops");
        result.Value.CharacterIds.ShouldContain(_fromCharacterGuid, "Path should contain from character");
        result.Value.CharacterIds.ShouldContain(_toCharacterGuid, "Path should contain to character");
    }

    [Fact]
    [Trait(Constants.TraitName, Constants.TestTitle)]
    public async Task InvokeAsync_WhenNoPathExists_ReturnsEmptyPayload()
    {
        // Arrange
        var query = new FindRelationBetweenCharacterQuery(_fromCharacterGuid, _toCharacterGuid, 10);

        _characterRepository
            .CharacterExistsAsync(Arg.Any<IAsyncTransaction>(), _fromCharacterGuid)
            .Returns(new EntityExistence(true, 1));
        _characterRepository
            .CharacterExistsAsync(Arg.Any<IAsyncTransaction>(), _toCharacterGuid)
            .Returns(new EntityExistence(true, 1));
        _characterRepository
            .FindPathBetweenCharactersAsync(
                Arg.Any<IAsyncTransaction>(), _fromCharacterGuid, _toCharacterGuid, 10)
            .Returns(Array.Empty<Guid>().AsReadOnly());

        // Act
        var result = await _sut.InvokeAsync(query);

        // Assert
        result.IsSuccess.ShouldBeTrue("Query should succeed even when no path found");
        result.Value.CharacterIds.Count.ShouldBe(0, "Path should be empty when no connection exists");
        result.Value.Hops.ShouldBe(0, "Hops should be 0 when no connection exists");
    }

    [Fact]
    [Trait(Constants.TraitName, Constants.TestTitle)]
    public async Task InvokeAsync_WhenFromCharacterDoesNotExist_ReturnsNotFoundException()
    {
        // Arrange
        var query = new FindRelationBetweenCharacterQuery(_fromCharacterGuid, _toCharacterGuid, 10);

        _characterRepository
            .CharacterExistsAsync(Arg.Any<IAsyncTransaction>(), _fromCharacterGuid)
            .Returns(new EntityExistence(false, 0));

        // Act
        var result = await _sut.InvokeAsync(query);

        // Assert
        result.IsSuccess.ShouldBeFalse("Query should fail when from character does not exist");
        result.Error.ShouldBeOfType<NotFoundException>("Error should be NotFoundException");
    }

    [Fact]
    [Trait(Constants.TraitName, Constants.TestTitle)]
    public async Task InvokeAsync_WhenToCharacterDoesNotExist_ReturnsNotFoundException()
    {
        // Arrange
        var query = new FindRelationBetweenCharacterQuery(_fromCharacterGuid, _toCharacterGuid, 10);

        _characterRepository
            .CharacterExistsAsync(Arg.Any<IAsyncTransaction>(), _fromCharacterGuid)
            .Returns(new EntityExistence(true, 1));
        _characterRepository
            .CharacterExistsAsync(Arg.Any<IAsyncTransaction>(), _toCharacterGuid)
            .Returns(new EntityExistence(false, 0));

        // Act
        var result = await _sut.InvokeAsync(query);

        // Assert
        result.IsSuccess.ShouldBeFalse("Query should fail when to character does not exist");
        result.Error.ShouldBeOfType<NotFoundException>("Error should be NotFoundException");
    }

    [Fact]
    [Trait(Constants.TraitName, Constants.TestTitle)]
    public async Task InvokeAsync_WhenRepositoryThrows_ReturnsException()
    {
        // Arrange
        var query = new FindRelationBetweenCharacterQuery(_fromCharacterGuid, _toCharacterGuid, 10);
        var expectedException = new Exception("DB error");

        _characterRepository
            .CharacterExistsAsync(Arg.Any<IAsyncTransaction>(), _fromCharacterGuid)
            .Returns(new EntityExistence(true, 1));
        _characterRepository
            .CharacterExistsAsync(Arg.Any<IAsyncTransaction>(), _toCharacterGuid)
            .Returns(new EntityExistence(true, 1));
        _characterRepository
            .FindPathBetweenCharactersAsync(
                Arg.Any<IAsyncTransaction>(), _fromCharacterGuid, _toCharacterGuid, 10)
            .ThrowsAsync(expectedException);

        // Act
        var result = await _sut.InvokeAsync(query);

        // Assert
        result.IsSuccess.ShouldBeFalse("Result should be failure when repository throws");
        result.Error.ShouldBe(expectedException, "Error should be the thrown exception");
    }

    [Fact]
    [Trait(Constants.TraitName, Constants.TestTitle)]
    public async Task InvokeAsync_PassesMaxHopsToRepository()
    {
        // Arrange
        const int maxHops = 5;
        var query = new FindRelationBetweenCharacterQuery(_fromCharacterGuid, _toCharacterGuid, maxHops);

        _characterRepository
            .CharacterExistsAsync(Arg.Any<IAsyncTransaction>(), _fromCharacterGuid)
            .Returns(new EntityExistence(true, 1));
        _characterRepository
            .CharacterExistsAsync(Arg.Any<IAsyncTransaction>(), _toCharacterGuid)
            .Returns(new EntityExistence(true, 1));
        _characterRepository
            .FindPathBetweenCharactersAsync(
                Arg.Any<IAsyncTransaction>(), _fromCharacterGuid, _toCharacterGuid, maxHops)
            .Returns(new List<Guid> { _fromCharacterGuid, _toCharacterGuid }.AsReadOnly());

        // Act
        await _sut.InvokeAsync(query);

        // Assert
        await _characterRepository
            .Received(1)
            .FindPathBetweenCharactersAsync(
                Arg.Any<IAsyncTransaction>(),
                _fromCharacterGuid,
                _toCharacterGuid,
                maxHops);
    }
}