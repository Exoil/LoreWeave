using LoreWeave.Domain.Entities.Knows;
using LoreWeave.Domain.Transactions;

namespace LoreWeave.Domain.Repositories.Knows;

public interface IKnowRelationReader
{
    Task<KnowRelation> GetKnowRelationAsync(
        ITransaction transaction,
        Guid fromCharacterId,
        Guid toCharacterId);

    Task<IReadOnlyCollection<Guid>> FindPathBetweenCharactersAsync(
        ITransaction transaction,
        Guid fromCharacterId,
        Guid toCharacterId,
        int maxHops);
}
