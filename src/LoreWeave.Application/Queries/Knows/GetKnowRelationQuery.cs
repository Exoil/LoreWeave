namespace LoreWeave.Application.Queries.Knows;

public record GetKnowRelationQuery(Guid BoardId, Guid FromCharacterId, Guid ToCharacterId);
