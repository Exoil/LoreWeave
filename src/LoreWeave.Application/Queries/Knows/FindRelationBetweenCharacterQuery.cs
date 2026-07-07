namespace LoreWeave.Application.Queries.Knows;

public record FindRelationBetweenCharacterQuery(
    Guid BoardId,
    Guid FromCharacterId,
    Guid ToCharacterId,
    int MaxHops = 10);