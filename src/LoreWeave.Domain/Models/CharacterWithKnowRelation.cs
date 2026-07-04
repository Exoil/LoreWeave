namespace LoreWeave.Domain.Models;

public sealed record CharacterWithKnowRelation(
    Guid Id,
    string Name,
    IReadOnlyCollection<KnowRelationDetail> KnowRelations,
    IReadOnlyCollection<FactDetail> Facts);