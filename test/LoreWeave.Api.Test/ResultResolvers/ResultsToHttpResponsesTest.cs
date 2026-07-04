using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

using MessagePipe;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;

using NSubstitute;

using LoreWeave.Api.ResultResolvers;
using LoreWeave.Application.Models;
using LoreWeave.Domain.Exceptions;

using Shouldly;

namespace LoreWeave.Api.Test.ResultResolvers;

public class ResultsToHttpResponsesTest
{
    private readonly Endpoint _mockEndpoint = new(null, null, "TestEndpoint");
    private readonly IHttpContextAccessor _mockHttpContextAccessor;

    public ResultsToHttpResponsesTest()
    {
        _mockHttpContextAccessor = Substitute.For<IHttpContextAccessor>();
        var mockHttpContext = Substitute.For<HttpContext>();
        mockHttpContext.GetEndpoint().Returns(_mockEndpoint);
        _mockHttpContextAccessor.HttpContext.Returns(mockHttpContext);
    }

    [Fact]
    [Trait(Constants.TraitName, Constants.TestTitle)]
    public async Task ExecuteVoidRequest_Return_Expected_ObjectResult()
    {
        // Arrange
        var serviceProvider = Substitute.For<IServiceProvider>();
        var handler = Substitute.For<IAsyncRequestHandler<VoidRequest, Result<Exception>>>();
        handler.InvokeAsync(Arg.Any<VoidRequest>(), Arg.Any<CancellationToken>())
            .Returns(_ => new Result<Exception>());
        serviceProvider.GetService(typeof(IAsyncRequestHandler<VoidRequest, Result<Exception>>))
            .Returns(handler);

        var httpResolver = new ResultsToHttpResponses(serviceProvider, _mockHttpContextAccessor);

        // Act
        var result = await httpResolver.GetResult(
            new VoidRequest(),
            () => Results.Ok(),
            CancellationToken.None);

        // Assert
        result.ShouldNotBeNull();
        result.ShouldBeOfType<Ok>();
    }

    [Theory]
    [Trait(Constants.TraitName, Constants.TestTitle)]
    [MemberData(nameof(GetExceptionsWithResponses))]
    public async Task ExecuteVoid_Request_Return_Over_400_HttpStatusCode(DomainException exception,
        int expectedStatusCode)
    {
        // Arrange
        var serviceProvider = Substitute.For<IServiceProvider>();
        var handler = Substitute.For<IAsyncRequestHandler<VoidRequest, Result<Exception>>>();
        handler.InvokeAsync(Arg.Any<VoidRequest>(), Arg.Any<CancellationToken>())
            .Returns(_ => exception);
        serviceProvider.GetService(typeof(IAsyncRequestHandler<VoidRequest, Result<Exception>>))
            .Returns(handler);

        var httpResolver = new ResultsToHttpResponses(serviceProvider, _mockHttpContextAccessor);

        // Act
        var result = await httpResolver.GetResult(
            new VoidRequest(),
            () => Results.Ok(),
            CancellationToken.None);

        // Assert
        result.ShouldBeOfType<ProblemHttpResult>();
        var problemHttpResult = result as ProblemHttpResult;

        problemHttpResult!.StatusCode.ShouldBe(expectedStatusCode);
        problemHttpResult.ProblemDetails.Detail.ShouldBe(exception.Message);
        problemHttpResult.ProblemDetails.Title.ShouldBe(exception.Title);
        problemHttpResult.ProblemDetails.Instance.ShouldBe(_mockEndpoint.DisplayName);
    }

    [Fact]
    [Trait(Constants.TraitName, Constants.TestTitle)]
    public async Task ExecuteVoid_Request_Return_GeneralException_And_500_HttpStatusCode()
    {
        // Arrange
        var serviceProvider = Substitute.For<IServiceProvider>();
        var handler = Substitute.For<IAsyncRequestHandler<VoidRequest, Result<Exception>>>();
        handler.InvokeAsync(Arg.Any<VoidRequest>(), Arg.Any<CancellationToken>())
            .Returns(_ => new Exception("Test"));
        serviceProvider.GetService(typeof(IAsyncRequestHandler<VoidRequest, Result<Exception>>))
            .Returns(handler);

        var httpResolver = new ResultsToHttpResponses(serviceProvider, _mockHttpContextAccessor);

        // Act
        var result = await httpResolver.GetResult(
            new VoidRequest(),
            () => Results.Ok(),
            CancellationToken.None);

        // Assert
        result.ShouldBeOfType<ProblemHttpResult>();
        var problemHttpResult = result as ProblemHttpResult;
        problemHttpResult!.StatusCode.ShouldBe((int)HttpStatusCode.InternalServerError);
    }

    [Fact]
    [Trait(Constants.TraitName, Constants.TestTitle)]
    public async Task ExecuteIntRequest_Return_Expected_ObjectResult()
    {
        // Arrange
        var serviceProvider = Substitute.For<IServiceProvider>();
        var handler = Substitute.For<IAsyncRequestHandler<int, Result<int, Exception>>>();
        handler.InvokeAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(ci => (int)ci[0]); // implicit to Result<int, Exception>
        serviceProvider.GetService(typeof(IAsyncRequestHandler<int, Result<int, Exception>>))
            .Returns(handler);

        var httpResolver = new ResultsToHttpResponses(serviceProvider, _mockHttpContextAccessor);

        // Act
        var result = await httpResolver.GetResult<int, int>(
            1,
            data => Results.Ok(data),
            CancellationToken.None);

        // Assert
        result.ShouldNotBeNull();
        result.ShouldBeOfType<Ok<int>>();
    }

    [Theory]
    [Trait(Constants.TraitName, Constants.TestTitle)]
    [MemberData(nameof(GetExceptionsWithResponses))]
    public async Task ExecuteInt_Request_Return_Over_400_HttpStatusCode(DomainException exception,
        int expectedStatusCode)
    {
        // Arrange
        var serviceProvider = Substitute.For<IServiceProvider>();
        var handler = Substitute.For<IAsyncRequestHandler<int, Result<int, Exception>>>();
        handler.InvokeAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(_ => exception); // implicit to Result<int, Exception>
        serviceProvider.GetService(typeof(IAsyncRequestHandler<int, Result<int, Exception>>))
            .Returns(handler);

        var httpResolver = new ResultsToHttpResponses(serviceProvider, _mockHttpContextAccessor);

        // Act
        var result = await httpResolver.GetResult(
            42,
            (int data) => Results.Ok(data),
            CancellationToken.None);

        // Assert
        result.ShouldBeOfType<ProblemHttpResult>();
        var problemHttpResult = result as ProblemHttpResult;
        problemHttpResult!.StatusCode.ShouldBe(expectedStatusCode);
        problemHttpResult.ProblemDetails.Detail.ShouldBe(exception.Message);
        problemHttpResult.ProblemDetails.Title.ShouldBe(exception.Title);
        problemHttpResult.ProblemDetails.Instance.ShouldBe(_mockEndpoint.DisplayName);
    }

    [Fact]
    [Trait(Constants.TraitName, Constants.TestTitle)]
    public async Task ExecuteInt_Request_Return_GeneralException_And_500_HttpStatusCode()
    {
        // Arrange
        var serviceProvider = Substitute.For<IServiceProvider>();
        var handler = Substitute.For<IAsyncRequestHandler<int, Result<int, Exception>>>();
        handler.InvokeAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(_ => new Exception("SomeException")); // implicit to Result<int, Exception>
        serviceProvider.GetService(typeof(IAsyncRequestHandler<int, Result<int, Exception>>))
            .Returns(handler);

        var httpResolver = new ResultsToHttpResponses(serviceProvider, _mockHttpContextAccessor);

        // Act
        var result = await httpResolver.GetResult(
            7,
            (int data) => Results.Ok(data),
            CancellationToken.None);

        // Assert
        result.ShouldBeOfType<ProblemHttpResult>();
        var problemHttpResult = result as ProblemHttpResult;
        problemHttpResult!.StatusCode.ShouldBe((int)HttpStatusCode.InternalServerError);
    }


    public static IEnumerable<object[]> GetExceptionsWithResponses()
    {
        yield return new object[]
        {
            new ValueObjectException(
                "Text Validation Exception",
                "errorCode",
                new List<ValidationMessage>()),
            (int)HttpStatusCode.BadRequest
        };

        yield return new object[]
        {
            new ConflictException("Character is already connected to the fact."),
            (int)HttpStatusCode.Conflict
        };
    }

    // Marker request type for void-style requests must be public for proxy generation
    public readonly struct VoidRequest
    {
    }
}
