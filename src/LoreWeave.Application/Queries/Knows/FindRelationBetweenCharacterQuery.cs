namespace LoreWeave.Application.Queries.Knows;

public record FindRelationBetweenCharacterQuery(
    Guid FromCharacterId,
    Guid ToCharacterId,
    int MaxHops = 10);