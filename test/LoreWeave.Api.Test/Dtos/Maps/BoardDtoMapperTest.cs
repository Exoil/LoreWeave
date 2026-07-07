using System;

using LoreWeave.Api.Dtos.Boards;
using LoreWeave.Api.Dtos.Maps;
using LoreWeave.Application.Models;

using Shouldly;

namespace LoreWeave.Api.Test.Dtos.Maps;

public class BoardDtoMapperTest
{
    private static readonly BoardConfigurationPayload ConfigurationPayload = new(
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

    [Fact]
    [Trait(Constants.TraitName, Constants.TestTitle)]
    public void ToBoardDto_MapsAllProperties()
    {
        // Arrange
        var boardPayload = new BoardPayload(Guid.NewGuid(), "Curse of Strahd", ConfigurationPayload, 3);

        // Act
        var boardDto = boardPayload.ToBoardDto();

        // Assert
        boardDto.ShouldBeOfType<BoardDto>("Mapper should produce a BoardDto");
        boardDto.Id.ShouldBe(boardPayload.Id, "Id should be mapped from the payload");
        boardDto.Name.ShouldBe(boardPayload.Name, "Name should be mapped from the payload");
        boardDto.Configuration.ShouldBe(
            ConfigurationPayload.ToBoardConfigurationDto(),
            "Configuration should be mapped from the payload");
    }

    [Fact]
    [Trait(Constants.TraitName, Constants.TestTitle)]
    public void ToBoardConfigurationDto_MapsAllProperties()
    {
        // Act
        var dto = ConfigurationPayload.ToBoardConfigurationDto();

        // Assert
        dto.CharacterNodeColor.ShouldBe(ConfigurationPayload.CharacterNodeColor, "CharacterNodeColor should be mapped");
        dto.FactNodeColor.ShouldBe(ConfigurationPayload.FactNodeColor, "FactNodeColor should be mapped");
        dto.RelationEdgeColor.ShouldBe(ConfigurationPayload.RelationEdgeColor, "RelationEdgeColor should be mapped");
        dto.FactEdgeColor.ShouldBe(ConfigurationPayload.FactEdgeColor, "FactEdgeColor should be mapped");
        dto.PathHighlightColor.ShouldBe(ConfigurationPayload.PathHighlightColor, "PathHighlightColor should be mapped");
        dto.NodeRadius.ShouldBe(ConfigurationPayload.NodeRadius, "NodeRadius should be mapped");
        dto.EdgeWidth.ShouldBe(ConfigurationPayload.EdgeWidth, "EdgeWidth should be mapped");
        dto.CurvedEdges.ShouldBe(ConfigurationPayload.CurvedEdges, "CurvedEdges should be mapped");
        dto.ShowGrid.ShouldBe(ConfigurationPayload.ShowGrid, "ShowGrid should be mapped");
        dto.ScalingObjects.ShouldBe(ConfigurationPayload.ScalingObjects, "ScalingObjects should be mapped");
    }

    [Fact]
    [Trait(Constants.TraitName, Constants.TestTitle)]
    public void ToBoardConfigurationPayload_RoundTripsAllProperties()
    {
        // Arrange
        var dto = ConfigurationPayload.ToBoardConfigurationDto();

        // Act
        var payload = dto.ToBoardConfigurationPayload();

        // Assert
        payload.ShouldBe(ConfigurationPayload, "Dto → payload mapping should round-trip all properties");
    }
}
