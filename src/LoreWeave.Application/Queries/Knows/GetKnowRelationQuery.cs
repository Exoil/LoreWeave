namespace LoreWeave.Application.Queries.Knows;

public record GetKnowRelationQuery(Guid FromCharacterId, Guid ToCharacterId);
