using LoreWeave.Application.Commands.Facts;

using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace LoreWeave.Api.Dtos.Facts;

public sealed record UpdateFactDto(
    [property: JsonPropertyName("title")]
    [StringLength(100, MinimumLength = 1, ErrorMessage = "Value for {0} must be between {1} and {2} characters.")]
    string Title,
    [property: JsonPropertyName("content")]
    [StringLength(3000, MinimumLength = 1, ErrorMessage = "Value for {0} must be between {1} and {2} characters.")]
    string Content)
{
    public UpdateFactCommand ToCommand(Guid id, string version) => new(
        id,
        Title,
        Content,
        ushort.Parse(
            version
                .Replace(
                    "\"",
                    string.Empty)));
}
