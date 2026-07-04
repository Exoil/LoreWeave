using LoreWeave.Api.Dtos.Facts;
using LoreWeave.Application.Models;

namespace LoreWeave.Api.Dtos.Maps;

public static class FactDtoMapper
{
    public static FactDto ToFactDto(this FactPayload factPayload) =>
        new(factPayload.Id, factPayload.Title, factPayload.Content);
}