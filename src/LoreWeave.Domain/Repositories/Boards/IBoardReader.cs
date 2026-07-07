using LoreWeave.Domain.Entities.Boards;
using LoreWeave.Domain.Transactions;

namespace LoreWeave.Domain.Repositories.Boards;

public interface IBoardReader
{
    Task<Board> GetAsync(ITransaction transaction, Guid id);

    Task<IReadOnlyCollection<Board>> GetAllAsync(ITransaction transaction);
}
