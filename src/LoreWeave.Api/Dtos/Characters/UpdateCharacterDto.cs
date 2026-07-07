using LoreWeave.Application.Commands.Characters;

using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace LoreWeave.Api.Dtos.Characters;

public record UpdateCharacterDto(
    [property: JsonPropertyName("name")]
    [StringLength(50, MinimumLength = 1, ErrorMessage = "Value for {0} must be between {1} and {2} characters.")]
    string Name)
{
    public UpdateCharacterCommand ToCommand(Guid boardId, Guid id, string version) => new(
        boardId,
        id,
        Name,
        ushort.Parse(
            version
                .Replace(
                    "\"",
                    string.Empty)));
}
