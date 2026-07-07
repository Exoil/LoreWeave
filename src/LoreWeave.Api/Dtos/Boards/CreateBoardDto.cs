using LoreWeave.Application.Commands.Boards;

using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace LoreWeave.Api.Dtos.Boards;

public record CreateBoardDto(
    [property: JsonPropertyName("name")]
    [StringLength(50, MinimumLength = 1, ErrorMessage = "Value for {0} must be between {1} and {2} characters.")]
    string Name)
{
    public CreateBoardCommand ToCommand() => new(Guid.CreateVersion7(), Name);
}
