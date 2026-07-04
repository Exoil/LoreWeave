using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace LoreWeave.Api.Dtos.Facts;

public record UpdateFactDto(
    [property: JsonPropertyName("title")]
    [StringLength(100, MinimumLength = 1, ErrorMessage = "Value for {0} must be between {1} and {2} characters.")]
    string Title,
    [property: JsonPropertyName("content")]
    [StringLength(512, MinimumLength = 1, ErrorMessage = "Value for {0} must be between {1} and {2} characters.")]
    string Content);
