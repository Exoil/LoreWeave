using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using NSubstitute;
using NSubstitute.ExceptionExtensions;

using LoreWeave.Application.Models;
using LoreWeave.Application.Queries.Knows;
using LoreWeave.Application.Queries.Knows.QueryHandlers;
using LoreWeave.Domain.Exceptions;
using LoreWeave.Domain.Extensions;
using LoreWeave.Domain.Transactions;
using LoreWeave.Domain.Models;
using LoreWeave.Domain.Repositories.Characters;
using LoreWeave.Domain.Repositories.Knows;

using Shouldly;

using ILogger = Serilog.ILogger;

namespace LoreWeave.Application.Test.Queries.Knows.QueryHandlers;

public class FindRelationBetweenCharacterQueryHandlerTest
{
    private readonly IExistsCharacter _existsCharacter;
    private readonly IKnowRelationReader _knowRelationReader;
    private readonly FindRelationBetweenCharacterQueryHandler _sut;

    private static readonly Guid _fromCharacterGuid = Guid.NewGuid();
    private static readonly Guid _toCharacterGuid = Guid.NewGuid();

    public FindRelationBetweenCharacterQueryHandlerTest()
    {
        _existsCharacter = Substitute.For<IExistsCharacter>();
        _knowRelationReader = Substitute.For<IKnowRelationReader>();
        var logger = Substitute.For<ILogger>();
        var transaction = Substitute.For<ITransaction>();
        var transactionFactory = Substitute.For<ITransactionFactory>();
        transactionFactory.CreateAsync().Returns(transaction);

        _sut = new FindRelationBetweenCharacterQueryHandler(transactionFactory, _existsCharacter, _knowRelationReader, logger);
    }

    [Fact]
    [Trait(Constants.TraitName, Constants.TestTitle)]
    public async Task InvokeAsync_WhenPathExists_ReturnsRelationPathPayload()
    {
        // Arrange
        var middleGuid = Guid.CreateVersion7();
        var query = new FindRelationBetweenCharacterQuery(_fromCharacterGuid, _toCharacterGuid, 10);

        _existsCharacter
            .CharacterExistsAsync(Arg.Any<ITransaction>(), _fromCharacterGuid)
            .Returns(new EntityExistence(true, 1));
        _existsCharacter
            .CharacterExistsAsync(Arg.Any<ITransaction>(), _toCharacterGuid)
            .Returns(new EntityExistence(true, 1));
        _knowRelationReader
            .FindPathBetweenCharactersAsync(
                Arg.Any<ITransaction>(), _fromCharacterGuid, _toCharacterGuid, 10)
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

        _existsCharacter
            .CharacterExistsAsync(Arg.Any<ITransaction>(), _fromCharacterGuid)
            .Returns(new EntityExistence(true, 1));
        _existsCharacter
            .CharacterExistsAsync(Arg.Any<ITransaction>(), _toCharacterGuid)
            .Returns(new EntityExistence(true, 1));
        _knowRelationReader
            .FindPathBetweenCharactersAsync(
                Arg.Any<ITransaction>(), _fromCharacterGuid, _toCharacterGuid, 10)
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

        _existsCharacter
            .CharacterExistsAsync(Arg.Any<ITransaction>(), _fromCharacterGuid)
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

        _existsCharacter
            .CharacterExistsAsync(Arg.Any<ITransaction>(), _fromCharacterGuid)
            .Returns(new EntityExistence(true, 1));
        _existsCharacter
            .CharacterExistsAsync(Arg.Any<ITransaction>(), _toCharacterGuid)
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

        _existsCharacter
            .CharacterExistsAsync(Arg.Any<ITransaction>(), _fromCharacterGuid)
            .Returns(new EntityExistence(true, 1));
        _existsCharacter
            .CharacterExistsAsync(Arg.Any<ITransaction>(), _toCharacterGuid)
            .Returns(new EntityExistence(true, 1));
        _knowRelationReader
            .FindPathBetweenCharactersAsync(
                Arg.Any<ITransaction>(), _fromCharacterGuid, _toCharacterGuid, 10)
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

        _existsCharacter
            .CharacterExistsAsync(Arg.Any<ITransaction>(), _fromCharacterGuid)
            .Returns(new EntityExistence(true, 1));
        _existsCharacter
            .CharacterExistsAsync(Arg.Any<ITransaction>(), _toCharacterGuid)
            .Returns(new EntityExistence(true, 1));
        _knowRelationReader
            .FindPathBetweenCharactersAsync(
                Arg.Any<ITransaction>(), _fromCharacterGuid, _toCharacterGuid, maxHops)
            .Returns(new List<Guid> { _fromCharacterGuid, _toCharacterGuid }.AsReadOnly());

        // Act
        await _sut.InvokeAsync(query);

        // Assert
        await _knowRelationReader
            .Received(1)
            .FindPathBetweenCharactersAsync(
                Arg.Any<ITransaction>(),
                _fromCharacterGuid,
                _toCharacterGuid,
                maxHops);
    }
}