using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace LoreWeave.Api.Dtos.Boards;

public record BoardConfigurationDto(
    [RegularExpression("^#[0-9a-fA-F]{6}$", ErrorMessage = "Value for {0} must be a 6-digit hex colour, e.g. #4466cc.")]
    [property: JsonPropertyName("characterNodeColor")]
    string CharacterNodeColor,
    [RegularExpression("^#[0-9a-fA-F]{6}$", ErrorMessage = "Value for {0} must be a 6-digit hex colour, e.g. #4466cc.")]
    [property: JsonPropertyName("factNodeColor")]
    string FactNodeColor,
    [RegularExpression("^#[0-9a-fA-F]{6}$", ErrorMessage = "Value for {0} must be a 6-digit hex colour, e.g. #4466cc.")]
    [property: JsonPropertyName("relationEdgeColor")]
    string RelationEdgeColor,
    [RegularExpression("^#[0-9a-fA-F]{6}$", ErrorMessage = "Value for {0} must be a 6-digit hex colour, e.g. #4466cc.")]
    [property: JsonPropertyName("factEdgeColor")]
    string FactEdgeColor,
    [RegularExpression("^#[0-9a-fA-F]{6}$", ErrorMessage = "Value for {0} must be a 6-digit hex colour, e.g. #4466cc.")]
    [property: JsonPropertyName("pathHighlightColor")]
    string PathHighlightColor,
    [Range(8, 48, ErrorMessage = "Value for {0} must be between {1} and {2}.")]
    [property: JsonPropertyName("nodeRadius")]
    int NodeRadius,
    [Range(1, 12, ErrorMessage = "Value for {0} must be between {1} and {2}.")]
    [property: JsonPropertyName("edgeWidth")]
    int EdgeWidth,
    [property: JsonPropertyName("curvedEdges")]
    bool CurvedEdges,
    [property: JsonPropertyName("showGrid")]
    bool ShowGrid,
    [property: JsonPropertyName("scalingObjects")]
    bool ScalingObjects);
