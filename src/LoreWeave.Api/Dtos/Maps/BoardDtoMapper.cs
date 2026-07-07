using LoreWeave.Api.Dtos.Boards;
using LoreWeave.Application.Models;

namespace LoreWeave.Api.Dtos.Maps;

public static class BoardDtoMapper
{
    public static BoardDto ToBoardDto(this BoardPayload boardPayload) =>
        new(
            boardPayload.Id,
            boardPayload.Name,
            boardPayload.Configuration.ToBoardConfigurationDto());

    public static BoardConfigurationDto ToBoardConfigurationDto(this BoardConfigurationPayload payload) =>
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

    public static BoardConfigurationPayload ToBoardConfigurationPayload(this BoardConfigurationDto dto) =>
        new(
            dto.CharacterNodeColor,
            dto.FactNodeColor,
            dto.RelationEdgeColor,
            dto.FactEdgeColor,
            dto.PathHighlightColor,
            dto.NodeRadius,
            dto.EdgeWidth,
            dto.CurvedEdges,
            dto.ShowGrid,
            dto.ScalingObjects);
}
