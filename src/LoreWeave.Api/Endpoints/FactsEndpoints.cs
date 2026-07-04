using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;

using LoreWeave.Api.Constants;
using LoreWeave.Api.Dtos.Facts;
using LoreWeave.Api.Dtos.Maps;
using LoreWeave.Api.ResultResolvers;
using LoreWeave.Application.Commands.Facts;
using LoreWeave.Application.Models;
using LoreWeave.Application.Queries.Facts;

namespace LoreWeave.Api.Endpoints;

public static class FactsEndpoints
{
    extension(WebApplication webApplication)
    {
        public void MapFactsEndpoints() =>
            webApplication
                .MapGroup("v1/facts")
                .MapFactsEndpoints();
    }

    extension(RouteGroupBuilder endpointGroup)
    {
        private void MapFactsEndpoints()
        {
            endpointGroup
                .MapGet(
                    "/{id:guid}",
                    async (
                            [FromServices] IHttpContextAccessor httpContextAccessor,
                            [FromServices] ResultsToHttpResponses responseResolver,
                            [FromRoute] Guid id,
                            CancellationToken cancellationToken = default) =>
                        await responseResolver.GetResult<GetFactByIdQuery, FactPayload>(
                            new GetFactByIdQuery(id),
                            data =>
                            {
                                httpContextAccessor.HttpContext!.Response.Headers.ETag = new StringValues(data.Etag);

                                return Results.Ok(data.ToFactDto());
                            },
                            cancellationToken));

            endpointGroup
                .MapPut(
                    "/{id:guid}",
                    async (
                            [FromServices] ResultsToHttpResponses responseResolver,
                            [FromRoute] Guid id,
                            [FromHeader(Name = HeadersConstants.IfMatch)]
                            string version,
                            [FromBody] UpdateFactDto updateFact,
                            CancellationToken cancellationToken = default) =>
                        await responseResolver.GetResult<UpdateFactCommand>(
                            updateFact.ToCommand(id, version),
                            Results.NoContent,
                            cancellationToken));

            endpointGroup
                .MapDelete(
                    "/{id:guid}",
                    async (
                            [FromServices] ResultsToHttpResponses responseResolver,
                            [FromRoute] Guid id,
                            CancellationToken cancellationToken = default) =>
                        await responseResolver.GetResult<DeleteFactCommand>(
                            new DeleteFactCommand(id),
                            Results.NoContent,
                            cancellationToken));
        }
    }
}