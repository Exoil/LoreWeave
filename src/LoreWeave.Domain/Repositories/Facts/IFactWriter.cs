using LoreWeave.Domain.Entities.Facts.Commands;
using LoreWeave.Domain.Transactions;

namespace LoreWeave.Domain.Repositories.Facts;

public interface IFactWriter
{
    Task CreateAsync(ITransaction transaction, Guid characterId, CreateFact createFact);

    Task UpdateAsync(ITransaction transaction, UpdateFact updateFact);

    Task DeleteAsync(ITransaction transaction, DeleteFact deleteFact);
}
