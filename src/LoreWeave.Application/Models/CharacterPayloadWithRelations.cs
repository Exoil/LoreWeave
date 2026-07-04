using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace LoreWeave.Application.Models;

public record CharacterPayloadWithRelations(
    [property: JsonPropertyName("id")]
    Guid Id,
    [StringLength(50, MinimumLength = 1, ErrorMessage = "Value for {0} must be between {1} and {2} characters.")]
    [property: JsonPropertyName("name")]
    string Name,
    [property: JsonPropertyName("knowCharacters")]
    IReadOnlyCollection<KnowCharacterRelationPayload> KnowCharacters,
    [property: JsonPropertyName("facts")]
    IReadOnlyCollection<CharacterFactPayload> Facts);