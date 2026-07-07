using LoreWeave.Application.Models;
using LoreWeave.Domain.Entities.Boards;
using LoreWeave.Domain.Entities.Boards.Commands;

using Shouldly;

namespace LoreWeave.Application.Test.Models;

public class BoardPayloadMapperTest
{
    private static readonly BoardConfiguration Configuration = new(
        "#166534",
        "#9f1239",
        "#64748b",
        "#f59e0b",
        "#7c3aed",
        20,
        4,
        false,
        false,
        true);

    [Fact]
    [Trait(Constants.TraitName, Constants.TestTitle)]
    public void ToBoardPayload_MapsAllProperties()
    {
        // Arrange
        const ushort version = 5;
        var board = new Board(new CreateBoard(Guid.NewGuid(), "Curse of Strahd"), Configuration, version);

        // Act
        var payload = board.ToBoardPayload();

        // Assert
        payload.Id.ShouldBe(board.Id, "Id should be mapped from the board");
        payload.Name.ShouldBe(board.Name, "Name should be mapped from the board");
        payload.Version.ShouldBe(version, "Version should be mapped from the board");
        payload.Configuration.ShouldBe(Configuration.ToBoardConfigurationPayload(), "Configuration should be mapped from the board");
    }

    [Fact]
    [Trait(Constants.TraitName, Constants.TestTitle)]
    public void ToBoardConfigurationPayload_MapsAllProperties()
    {
        // Act
        var payload = Configuration.ToBoardConfigurationPayload();

        // Assert
        payload.CharacterNodeColor.ShouldBe(Configuration.CharacterNodeColor, "CharacterNodeColor should be mapped");
        payload.FactNodeColor.ShouldBe(Configuration.FactNodeColor, "FactNodeColor should be mapped");
        payload.RelationEdgeColor.ShouldBe(Configuration.RelationEdgeColor, "RelationEdgeColor should be mapped");
        payload.FactEdgeColor.ShouldBe(Configuration.FactEdgeColor, "FactEdgeColor should be mapped");
        payload.PathHighlightColor.ShouldBe(Configuration.PathHighlightColor, "PathHighlightColor should be mapped");
        payload.NodeRadius.ShouldBe(Configuration.NodeRadius, "NodeRadius should be mapped");
        payload.EdgeWidth.ShouldBe(Configuration.EdgeWidth, "EdgeWidth should be mapped");
        payload.CurvedEdges.ShouldBe(Configuration.CurvedEdges, "CurvedEdges should be mapped");
        payload.ShowGrid.ShouldBe(Configuration.ShowGrid, "ShowGrid should be mapped");
        payload.ScalingObjects.ShouldBe(Configuration.ScalingObjects, "ScalingObjects should be mapped");
    }

    [Fact]
    [Trait(Constants.TraitName, Constants.TestTitle)]
    public void ToBoardConfiguration_RoundTripsAllProperties()
    {
        // Arrange
        var payload = Configuration.ToBoardConfigurationPayload();

        // Act
        var configuration = payload.ToBoardConfiguration();

        // Assert
        configuration.ShouldBe(Configuration, "Payload → domain mapping should round-trip all properties");
    }
}
