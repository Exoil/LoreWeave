using LoreWeave.Api.Constants;
using LoreWeave.Api.Dtos.Facts;
using LoreWeave.Api.ResultResolvers;

using Microsoft.AspNetCore.Mvc;

namespace LoreWeave.Api.Endpoints;

public static class FactsEndpoints
{
    extension(WebApplication webApplication)
    {
        public void MapCharacterEndpoints() =>
            webApplication
                .MapGroup("v1/facts")
                .MapFactsEndpoints();
    }

    extension(RouteGroupBuilder endpointGroup)
    {
        private void MapFactsEndpoints()
        {
            endpointGroup
                .MapPut(
                    "/{id:guid}",
                    async (
                            [FromServices] ResultsToHttpResponses responseResolver,
                            [FromRoute] Guid id,
                            [FromHeader(Name = HeadersConstants.IfMatch)]
                            string version,
                            [FromBody] UpdateFactDto updateCharacter,
                            CancellationToken cancellationToken = default) =>
                        throw new NotImplementedException());
        }
    }
}
