using LoreWeave.Domain.Entities.Boards;
using LoreWeave.Domain.Entities.Boards.Commands;
using LoreWeave.Domain.Transactions;

namespace LoreWeave.Domain.Repositories.Boards;

public interface IBoardWriter
{
    Task CreateAsync(ITransaction transaction, CreateBoard createBoard, BoardConfiguration configuration);

    Task UpdateAsync(ITransaction transaction, Guid id, UpdateBoard updateBoard);

    Task DeleteAsync(ITransaction transaction, DeleteBoard deleteBoard);
}
