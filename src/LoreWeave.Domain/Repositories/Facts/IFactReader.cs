using LoreWeave.Domain.Entities.Facts;
using LoreWeave.Domain.Transactions;

namespace LoreWeave.Domain.Repositories.Facts;

public interface IFactReader
{
    Task<Fact> GetFactAsync(ITransaction transaction, Guid id);
}
