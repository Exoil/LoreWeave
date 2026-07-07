namespace LoreWeave.Application.Commands.Knows;

public record DeleteKnowRelationCommand(
    Guid BoardId,
    Guid FromCharacterId,
    Guid ToCharacterId);
