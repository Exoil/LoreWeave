namespace LoreWeave.Application.Commands.Knows;

public record DeleteKnowRelationCommand(
    Guid FromCharacterId,
    Guid ToCharacterId);
