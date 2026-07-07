namespace LoreWeave.Application.Commands.Knows;

public record UpdateKnowRelationCommand(
    Guid BoardId,
    Guid FromCharacterId,
    Guid ToCharacterId,
    string Description,
    bool IsStrongRelation,
    ushort Version);