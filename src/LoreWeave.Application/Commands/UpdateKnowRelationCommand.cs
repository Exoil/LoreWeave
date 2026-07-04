namespace LoreWeave.Application.Commands;

public record UpdateKnowRelationCommand(
    Guid FromCharacterId,
    Guid ToCharacterId,
    string Description,
    bool IsStrongRelation,
    ushort Version);