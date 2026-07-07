using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;

using LoreWeave.Api.Constants;
using LoreWeave.Api.Dtos.Boards;
using LoreWeave.Api.Dtos.Maps;
using LoreWeave.Api.ResultResolvers;
using LoreWeave.Application.Commands.Boards;
using LoreWeave.Application.Models;
using LoreWeave.Application.Queries.Boards;

namespace LoreWeave.Api.Endpoints;

public static class BoardEndpoints
{
    extension(WebApplication webApplication)
    {
        public void MapBoardEndpoints() =>
            webApplication
                .MapGroup("v1/boards")
                .MapBoardEndpoints();
    }

    extension(RouteGroupBuilder endpointGroup)
    {
        private void MapBoardEndpoints()
        {
            endpointGroup
                .MapGet(
                    "",
                    async (
                            [FromServices] ResultsToHttpResponses responseResolver,
                            CancellationToken cancellationToken = default) =>
                        await responseResolver.GetResult<GetBoardsQuery, IReadOnlyCollection<BoardPayload>>(
                            new GetBoardsQuery(),
                            data => Results.Ok(
                                data
                                    .Select(board => board.ToBoardDto())
                                    .ToList()),
                            cancellationToken));

            endpointGroup
                .MapPost(
                    "/",
                    async (
                            [FromServices] ResultsToHttpResponses responseResolver,
                            [FromBody] CreateBoardDto createBoard,
                            CancellationToken cancellationToken = default) =>
                        await responseResolver.GetResult<CreateBoardCommand, Guid>(
                            createBoard.ToCommand(),
                            data => Results.Created(string.Empty, data),
                            cancellationToken));

            endpointGroup
                .MapGet(
                    "/{boardId:guid}",
                    async (
                            [FromServices] IHttpContextAccessor httpContextAccessor,
                            [FromServices] ResultsToHttpResponses responseResolver,
                            [FromRoute] Guid boardId,
                            CancellationToken cancellationToken = default) =>
                        await responseResolver.GetResult<GetBoardByIdQuery, BoardPayload>(
                            new GetBoardByIdQuery(boardId),
                            data =>
                            {
                                httpContextAccessor.HttpContext!.Response.Headers.ETag = new StringValues(data.Etag);

                                return Results.Ok(data.ToBoardDto());
                            },
                            cancellationToken));

            endpointGroup
                .MapPut(
                    "/{boardId:guid}",
                    async (
                            [FromServices] ResultsToHttpResponses responseResolver,
                            [FromRoute] Guid boardId,
                            [FromHeader(Name = HeadersConstants.IfMatch)]
                            string version,
                            [FromBody] UpdateBoardDto updateBoard,
                            CancellationToken cancellationToken = default) =>
                        await responseResolver.GetResult<UpdateBoardCommand>(
                            updateBoard.ToCommand(boardId, version),
                            Results.NoContent,
                            cancellationToken));

            endpointGroup
                .MapDelete(
                    "/{boardId:guid}",
                    async (
                            [FromServices] ResultsToHttpResponses responseResolver,
                            [FromRoute] Guid boardId,
                            CancellationToken cancellationToken = default) =>
                        await responseResolver.GetResult<DeleteBoardCommand>(
                            new DeleteBoardCommand(boardId),
                            Results.NoContent,
                            cancellationToken));
        }
    }
}
