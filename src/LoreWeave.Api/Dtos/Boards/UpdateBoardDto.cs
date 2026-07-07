using LoreWeave.Api.Dtos.Maps;
using LoreWeave.Application.Commands.Boards;

using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace LoreWeave.Api.Dtos.Boards;

public record UpdateBoardDto(
    [property: JsonPropertyName("name")]
    [StringLength(50, MinimumLength = 1, ErrorMessage = "Value for {0} must be between {1} and {2} characters.")]
    string Name,
    [property: JsonPropertyName("configuration")]
    BoardConfigurationDto Configuration)
{
    public UpdateBoardCommand ToCommand(Guid id, string version) => new(
        id,
        Name,
        Configuration.ToBoardConfigurationPayload(),
        ushort.Parse(
            version
                .Replace(
                    "\"",
                    string.Empty)));
}
