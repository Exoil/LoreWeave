using LoreWeave.Domain.Entities.Knows.Commands;
using LoreWeave.Domain.Transactions;

namespace LoreWeave.Domain.Repositories.Knows;

public interface IKnowRelationWriter
{
    Task CreateKnowRelationAsync(ITransaction transaction, CreateKnowRelation createKnowRelation);

    Task UpdateKnowRelationAsync(ITransaction transaction, UpdateKnowRelation updateKnowRelation);

    Task DeleteKnowRelationAsync(ITransaction transaction, DeleteKnowRelation deleteKnowRelation);
}
