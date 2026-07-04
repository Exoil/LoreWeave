using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace LoreWeave.Application.Models;

public record FactPayload(
    [property: JsonPropertyName("id")]
    Guid Id,
    [StringLength(100, MinimumLength = 1, ErrorMessage = "Value for {0} must be between {1} and {2} characters.")]
    [property: JsonPropertyName("title")]
    string Title,
    [StringLength(512, MinimumLength = 1, ErrorMessage = "Value for {0} must be between {1} and {2} characters.")]
    [property: JsonPropertyName("content")]
    string Content,
    [property: JsonPropertyName("version")]
    [Range(1, ushort.MaxValue, ErrorMessage = "Value for {0} must be between {1} and {2}.")]
    ushort Version)
{
    public string Etag => $"\"{Version}\"";
}