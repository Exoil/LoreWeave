using LoreWeave.Domain.Models;
using LoreWeave.Domain.Transactions;

namespace LoreWeave.Domain.Repositories.Boards;

public interface IExistsBoard
{
    Task<EntityExistence> BoardExistsAsync(ITransaction transaction, Guid id);
}
