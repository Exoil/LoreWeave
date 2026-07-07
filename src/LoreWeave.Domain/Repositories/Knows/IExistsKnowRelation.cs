using LoreWeave.Domain.Models;
using LoreWeave.Domain.Transactions;

namespace LoreWeave.Domain.Repositories.Knows;

public interface IExistsKnowRelation
{
    Task<EntityExistence> KnowRelationExistsAsync(
        ITransaction transaction,
        Guid boardId,
        Guid fromCharacterId,
        Guid toCharacterId);
}
