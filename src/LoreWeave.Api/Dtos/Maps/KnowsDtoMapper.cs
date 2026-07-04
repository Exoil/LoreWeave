using LoreWeave.Api.Dtos.Knows;
using LoreWeave.Application.Models;

namespace LoreWeave.Api.Dtos.Maps;

public static class KnowsDtoMapper
{
    public static KnowsDto ToKnowsDto(this KnowRelationPayload payload) =>
        new(
            payload.FromCharacterId,
            payload.ToCharacterId,
            payload.Description,
            payload.IsStrongRelation);
}
