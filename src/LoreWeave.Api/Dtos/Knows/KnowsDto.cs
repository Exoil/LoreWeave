using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace LoreWeave.Api.Dtos.Knows;

public record KnowsDto(
    [property: JsonPropertyName("fromCharacterId")]
    Guid FromCharacterId,
    [property: JsonPropertyName("toCharacterId")]
    Guid ToCharacterId,
    [StringLength(256, MinimumLength = 0, ErrorMessage = "Value for {0} must be between {1} and {2} characters.")]
    [property: JsonPropertyName("description")]
    string Description,
    [property: JsonPropertyName("isStrongRelation")]
    bool IsStrongRelation);
