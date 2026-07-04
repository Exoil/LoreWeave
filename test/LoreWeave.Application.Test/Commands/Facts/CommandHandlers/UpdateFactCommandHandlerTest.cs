using NSubstitute;
using NSubstitute.ExceptionExtensions;

using LoreWeave.Application.Commands.Facts;
using LoreWeave.Application.Commands.Facts.CommandHandlers;
using LoreWeave.Domain.Entities.Facts.Commands;
using LoreWeave.Domain.Exceptions;
using LoreWeave.Domain.Transactions;
using LoreWeave.Domain.Models;
using LoreWeave.Domain.Repositories.Facts;

using Serilog;

using Shouldly;

namespace LoreWeave.Application.Test.Commands.Facts.CommandHandlers;

public class UpdateFactCommandHandlerTest
{
    private readonly IExistsFact _existsFact;
    private readonly IFactWriter _factWriter;
    private readonly ITransaction _transaction;
    private readonly UpdateFactCommandHandler _sut;

    private static readonly Guid FactId = Guid.NewGuid();
    private const int CurrentVersion = 1;

    public UpdateFactCommandHandlerTest()
    {
        _existsFact = Substitute.For<IExistsFact>();
        _factWriter = Substitute.For<IFactWriter>();
        var logger = Substitute.For<ILogger>();
        _transaction = Substitute.For<ITransaction>();
        var transactionFactory = Substitute.For<ITransactionFactory>();
        transactionFactory.CreateAsync().Returns(_transaction);

        _sut = new UpdateFactCommandHandler(transactionFactory, _existsFact, _factWriter, logger);
    }

    [Fact]
    [Trait(Constants.TraitName, Constants.TestTitle)]
    public async Task InvokeAsync_WhenFactExistsAndVersionMatches_ReturnsSuccess()
    {
        // Arrange
        var command = new UpdateFactCommand(FactId, "UpdatedTitle", "UpdatedContent", CurrentVersion);
        _existsFact
            .FactExistsAsync(Arg.Any<ITransaction>(), Arg.Any<Guid>())
            .Returns(new EntityExistence(true, CurrentVersion));

        // Act
        var result = await _sut.InvokeAsync(command);

        // Assert
        result.IsSuccess.ShouldBeTrue("Update should succeed when fact exists and version matches");
        await _transaction.Received(1).CommitAsync();
    }

    [Fact]
    [Trait(Constants.TraitName, Constants.TestTitle)]
    public async Task InvokeAsync_WhenFactDoesNotExist_ReturnsNotFoundException()
    {
        // Arrange
        var command = new UpdateFactCommand(FactId, "UpdatedTitle", "UpdatedContent", CurrentVersion);
        _existsFact
            .FactExistsAsync(Arg.Any<ITransaction>(), Arg.Any<Guid>())
            .Returns(new EntityExistence(false, 0));

        // Act
        var result = await _sut.InvokeAsync(command);

        // Assert
        result.IsSuccess.ShouldBeFalse("Update should fail when fact does not exist");
        result.Error.ShouldBeOfType<NotFoundException>("Error should be NotFoundException");
    }

    [Fact]
    [Trait(Constants.TraitName, Constants.TestTitle)]
    public async Task InvokeAsync_WhenVersionDoesNotMatch_ReturnsPreconditionException()
    {
        // Arrange
        var command = new UpdateFactCommand(FactId, "UpdatedTitle", "UpdatedContent", CurrentVersion);
        _existsFact
            .FactExistsAsync(Arg.Any<ITransaction>(), Arg.Any<Guid>())
            .Returns(new EntityExistence(true, CurrentVersion + 1));

        // Act
        var result = await _sut.InvokeAsync(command);

        // Assert
        result.IsSuccess.ShouldBeFalse("Update should fail when version does not match");
        result.Error.ShouldBeOfType<PreconditionException>("Error should be PreconditionException");
    }

    [Fact]
    [Trait(Constants.TraitName, Constants.TestTitle)]
    public async Task InvokeAsync_WhenRepositoryThrows_ReturnsExceptionAndRollsBack()
    {
        // Arrange
        var command = new UpdateFactCommand(FactId, "UpdatedTitle", "UpdatedContent", CurrentVersion);
        var expectedException = new Exception("DB error");
        _existsFact
            .FactExistsAsync(Arg.Any<ITransaction>(), Arg.Any<Guid>())
            .Returns(new EntityExistence(true, CurrentVersion));
        _factWriter
            .UpdateAsync(Arg.Any<ITransaction>(), Arg.Any<UpdateFact>())
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
