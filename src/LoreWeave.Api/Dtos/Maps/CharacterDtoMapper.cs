using LoreWeave.Api.Dtos.Characters;
using LoreWeave.Application.Models;

namespace LoreWeave.Api.Dtos.Maps;

public static class CharacterDtoMapper
{
    public static CharacterDto ToCharacterDto(this CharacterPayload characterPayload) =>
        new(characterPayload.Id, characterPayload.Name);
}
