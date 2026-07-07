using LoreWeave.Domain.Entities.Boards;
using LoreWeave.Domain.Exceptions;

using Shouldly;

namespace LoreWeave.Domain.Test.Entities.Boards;

[Trait(Constants.TraitName, Constants.TestTitle)]
public class BoardConfigurationTest
{
    [Fact]
    public void Default_MatchesContractDefaults()
    {
        var configuration = BoardConfiguration.Default;

        configuration.CharacterNodeColor.ShouldBe("#4466cc");
        configuration.FactNodeColor.ShouldBe("#d97706");
        configuration.RelationEdgeColor.ShouldBe("#aaaaaa");
        configuration.FactEdgeColor.ShouldBe("#d9a066");
        configuration.PathHighlightColor.ShouldBe("#a855f7");
        configuration.NodeRadius.ShouldBe(16);
        configuration.EdgeWidth.ShouldBe(3);
        configuration.CurvedEdges.ShouldBeTrue();
        configuration.ShowGrid.ShouldBeTrue();
        configuration.ScalingObjects.ShouldBeTrue();
    }

    [Theory]
    [InlineData("#000000")]
    [InlineData("#FFFFFF")]
    [InlineData("#a1B2c3")]
    public void Create_WithValidHexColor_SetsColor(string color)
    {
        var configuration = BoardConfiguration.Default with { CharacterNodeColor = color };

        configuration.CharacterNodeColor.ShouldBe(color);
    }

    [Theory]
    [InlineData("4466cc")]
    [InlineData("#4466c")]
    [InlineData("#4466ccc")]
    [InlineData("#44 6cc")]
    [InlineData("blue")]
    public void Create_WithInvalidHexColor_Throws_Validation_Exception(string color)
    {
        var act = () => new BoardConfiguration(
            color,
            "#d97706",
            "#aaaaaa",
            "#d9a066",
            "#a855f7",
            16,
            3,
            true,
            true,
            true);

        var exception = act.ShouldThrow<ValueObjectException>();
        exception.ValidationErrors.TryGetValue(nameof(BoardConfiguration.CharacterNodeColor), out _).ShouldBeTrue();
    }

    [Theory]
    [InlineData(7)]
    [InlineData(49)]
    public void Create_WithNodeRadiusOutOfRange_Throws_Validation_Exception(int nodeRadius)
    {
        var act = () => new BoardConfiguration(
            "#4466cc",
            "#d97706",
            "#aaaaaa",
            "#d9a066",
            "#a855f7",
            nodeRadius,
            3,
            true,
            true,
            true);

        var exception = act.ShouldThrow<ValueObjectException>();
        exception.ValidationErrors.TryGetValue(nameof(BoardConfiguration.NodeRadius), out _).ShouldBeTrue();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(13)]
    public void Create_WithEdgeWidthOutOfRange_Throws_Validation_Exception(int edgeWidth)
    {
        var act = () => new BoardConfiguration(
            "#4466cc",
            "#d97706",
            "#aaaaaa",
            "#d9a066",
            "#a855f7",
            16,
            edgeWidth,
            true,
            true,
            true);

        var exception = act.ShouldThrow<ValueObjectException>();
        exception.ValidationErrors.TryGetValue(nameof(BoardConfiguration.EdgeWidth), out _).ShouldBeTrue();
    }

    [Theory]
    [InlineData(8, 1)]
    [InlineData(48, 12)]
    public void Create_WithBoundaryValues_Succeeds(int nodeRadius, int edgeWidth)
    {
        var configuration = new BoardConfiguration(
            "#4466cc",
            "#d97706",
            "#aaaaaa",
            "#d9a066",
            "#a855f7",
            nodeRadius,
            edgeWidth,
            true,
            true,
            true);

        configuration.NodeRadius.ShouldBe(nodeRadius);
        configuration.EdgeWidth.ShouldBe(edgeWidth);
    }
}
