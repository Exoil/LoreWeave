using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace LoreWeave.Application.Models;

public record CharacterPayload(
    [property: JsonPropertyName("id")]
    Guid Id,
    [StringLength(50, MinimumLength = 1, ErrorMessage = "Value for {0} must be between {1} and {2} characters.")]
    [property: JsonPropertyName("name")]
    string Name,
    [property: JsonPropertyName("version")]
    [Range(1, ushort.MaxValue, ErrorMessage = "Value for {0} must be between {1} and {2}.")]
    ushort Version)
{
    public string Etag => $"\"{Version}\"";
}
