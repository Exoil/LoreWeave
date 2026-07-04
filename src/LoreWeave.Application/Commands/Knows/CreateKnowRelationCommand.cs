namespace LoreWeave.Application.Commands.Knows;

public record CreateKnowRelationCommand(
    Guid FromCharacterId,
    Guid ToCharacterId,
    string Description,
    bool IsStrongRelation);
