using LoreWeave.Domain.Entities.Boards;

namespace LoreWeave.Application.Models;

public static class BoardPayloadMapper
{
    public static BoardPayload ToBoardPayload(this Board board) =>
        new(
            board.Id,
            board.Name,
            board.Configuration.ToBoardConfigurationPayload(),
            board.Version);

    public static BoardConfigurationPayload ToBoardConfigurationPayload(this BoardConfiguration configuration) =>
        new(
            configuration.CharacterNodeColor,
            configuration.FactNodeColor,
            configuration.RelationEdgeColor,
            configuration.FactEdgeColor,
            configuration.PathHighlightColor,
            configuration.NodeRadius,
            configuration.EdgeWidth,
            configuration.CurvedEdges,
            configuration.ShowGrid,
            configuration.ScalingObjects);

    public static BoardConfiguration ToBoardConfiguration(this BoardConfigurationPayload payload) =>
        new(
            payload.CharacterNodeColor,
            payload.FactNodeColor,
            payload.RelationEdgeColor,
            payload.FactEdgeColor,
            payload.PathHighlightColor,
            payload.NodeRadius,
            payload.EdgeWidth,
            payload.CurvedEdges,
            payload.ShowGrid,
            payload.ScalingObjects);
}
