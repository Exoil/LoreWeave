using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

using LoreWeave.Domain.Models;

namespace LoreWeave.Domain.Entities.Boards;

public sealed record BoardConfiguration : BaseValueObject
{
    private const string HexColorPattern = "^#[0-9a-fA-F]{6}$";

    private const string HexColorErrorMessage = "Value for {0} must be a 6-digit hex colour, e.g. #4466cc.";

    [SetsRequiredMembers]
    public BoardConfiguration(
        string characterNodeColor,
        string factNodeColor,
        string relationEdgeColor,
        string factEdgeColor,
        string pathHighlightColor,
        int nodeRadius,
        int edgeWidth,
        bool curvedEdges,
        bool showGrid,
        bool scalingObjects)
    {
        CharacterNodeColor = characterNodeColor;
        FactNodeColor = factNodeColor;
        RelationEdgeColor = relationEdgeColor;
        FactEdgeColor = factEdgeColor;
        PathHighlightColor = pathHighlightColor;
        NodeRadius = nodeRadius;
        EdgeWidth = edgeWidth;
        CurvedEdges = curvedEdges;
        ShowGrid = showGrid;
        ScalingObjects = scalingObjects;

        Validate();
    }

    protected override string ModelName => nameof(BoardConfiguration);

    // Matches the palette the frontend used to hard-code before boards existed,
    // so pre-board graphs keep looking the same.
    public static BoardConfiguration Default => new(
        "#4466cc",
        "#d97706",
        "#aaaaaa",
        "#d9a066",
        "#a855f7",
        16,
        3,
        true,
        true,
        true);

    [RegularExpression(HexColorPattern, ErrorMessage = HexColorErrorMessage)]
    public required string CharacterNodeColor { get; init; }

    [RegularExpression(HexColorPattern, ErrorMessage = HexColorErrorMessage)]
    public required string FactNodeColor { get; init; }

    [RegularExpression(HexColorPattern, ErrorMessage = HexColorErrorMessage)]
    public required string RelationEdgeColor { get; init; }

    [RegularExpression(HexColorPattern, ErrorMessage = HexColorErrorMessage)]
    public required string FactEdgeColor { get; init; }

    [RegularExpression(HexColorPattern, ErrorMessage = HexColorErrorMessage)]
    public required string PathHighlightColor { get; init; }

    [Range(8, 48, ErrorMessage = "Value for {0} must be between {1} and {2}.")]
    public required int NodeRadius { get; init; }

    [Range(1, 12, ErrorMessage = "Value for {0} must be between {1} and {2}.")]
    public required int EdgeWidth { get; init; }

    public required bool CurvedEdges { get; init; }

    public required bool ShowGrid { get; init; }

    public required bool ScalingObjects { get; init; }
}
