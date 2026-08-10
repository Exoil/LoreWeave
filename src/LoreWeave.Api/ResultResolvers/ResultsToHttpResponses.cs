using System.Net;

using MessagePipe;

using LoreWeave.Application.Models;
using LoreWeave.Domain.Exceptions;

namespace LoreWeave.Api.ResultResolvers;

public class ResultsToHttpResponses
{
    private readonly string _endpointDisplayName;
    private readonly IServiceProvider _serviceProvider;

    /// <summary>
    ///     Constructor for the HttpResponseResolver class. Initializes the class with a sender (mediator) and HTTP context.
    /// </summary>
    public ResultsToHttpResponses(
        IServiceProvider serviceProvider,
        IHttpContextAccessor httpContextAccessorAccessor)
    {
        _serviceProvider = serviceProvider;
        _endpointDisplayName = (
            httpContextAccessorAccessor.HttpContext is not null &&
            httpContextAccessorAccessor.HttpContext.GetEndpoint() is not null &&
            !string.IsNullOrEmpty(httpContextAccessorAccessor.HttpContext.GetEndpoint()?.DisplayName)
                ? httpContextAccessorAccessor.HttpContext.GetEndpoint()!.DisplayName
                : string.Empty)!;
    }

    /// <summary>
    ///     This function will take a request, and a function to transform the request’s payload into a result.
    ///     It will then resolve a MessagePipe async request handler and call the provided function on the result to get an
    ///     IResult object.
    /// </summary>
    public async Task<IResult> GetResult<TRequest, TPayload>(
        TRequest request,
        Func<TPayload, IResult> resultFunc,
        CancellationToken cancellationToken = default)
    {
        var handler =
            _serviceProvider.GetRequiredService<IAsyncRequestHandler<TRequest, Result<TPayload, Exception>>>();
        var result = await handler.InvokeAsync(request, cancellationToken);

        return !result.IsSuccess ? GetErrorBadRequest(result.Error!) : resultFunc.Invoke(result.Value);
    }

    /// <summary>
    ///     This function will take a request, and a function to transform the request’s payload into a result.
    ///     It will then resolve a MessagePipe async request handler and call the provided function on the result to get an
    ///     IResult object.
    /// </summary>
    public async Task<IResult> GetResult<TRequest>(
        TRequest request,
        Func<IResult> resultFunc,
        CancellationToken cancellationToken = default)
    {
        var handler = _serviceProvider.GetRequiredService<IAsyncRequestHandler<TRequest, Result<Exception>>>();
        var result = await handler.InvokeAsync(request, cancellationToken);

        return !result.IsSuccess ? GetErrorBadRequest(result.Error!) : resultFunc.Invoke();
    }

    /// <summary>
    ///     This private function takes an Exception as a parameter and returns an IResult according to the type of exception.
    ///     This function essentially maps specific exception types to a particular HTTP response.
    /// </summary>
    private IResult GetErrorBadRequest(Exception exception) => exception switch
    {
        ValueObjectException validationException
            => Results.Problem(
                validationException.Message,
                _endpointDisplayName,
                (int)HttpStatusCode.BadRequest,
                validationException.Title,
                validationException.ErrorCode,
                validationException.ValidationErrors),
        NotFoundException notFoundException
            => Results.Problem(
                notFoundException.Message,
                _endpointDisplayName,
                (int)HttpStatusCode.NotFound,
                notFoundException.Title,
                notFoundException.ErrorCode),
        PreconditionException preconditionException
            => Results.Problem(
                preconditionException.Message,
                _endpointDisplayName,
                (int)HttpStatusCode.PreconditionFailed,
                preconditionException.Title,
                preconditionException.ErrorCode),
        ConflictException conflictException
            => Results.Problem(
                conflictException.Message,
                _endpointDisplayName,
                (int)HttpStatusCode.Conflict,
                conflictException.Title,
                conflictException.ErrorCode),
        UnprocessableContentException unprocessableContentException
            => Results.Problem(
                unprocessableContentException.Message,
                _endpointDisplayName,
                (int)HttpStatusCode.UnprocessableEntity,
                unprocessableContentException.Title,
                unprocessableContentException.ErrorCode),
        ArgumentException argumentException
            => Results.Problem(
                argumentException.Message,
                _endpointDisplayName,
                (int)HttpStatusCode.BadRequest,
                $"Invalid {argumentException.ParamName} arguments",
                $"Invalid {argumentException.ParamName} arguments"),
        _ => exception.ToResult(_endpointDisplayName)
    };
}
