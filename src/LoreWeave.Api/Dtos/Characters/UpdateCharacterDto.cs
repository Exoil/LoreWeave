using LoreWeave.Application.Commands;

using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace LoreWeave.Api.Dtos.Characters;

public record UpdateCharacterDto(
    [property: JsonPropertyName("name")]
    [StringLength(50, MinimumLength = 1, ErrorMessage = "Value for {0} must be between {1} and {2} characters.")]
    string Name)
{
    public UpdateCharacterCommand ToCommand(Guid id, string version) => new(
        id,
        Name,
        int.Parse(
            version
                .Replace(
                    "\"",
                    string.Empty)));
}
