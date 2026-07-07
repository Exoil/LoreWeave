using LoreWeave.Domain.Models;
using LoreWeave.Domain.Transactions;

namespace LoreWeave.Domain.Repositories.Facts;

public interface IExistsFact
{
    Task<EntityExistence> FactExistsAsync(ITransaction transaction, Guid boardId, Guid id);
}
