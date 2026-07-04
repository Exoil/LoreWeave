namespace LoreWeave.Domain.Transactions;

public interface ITransaction : IAsyncDisposable
{
    Task CommitAsync();

    Task RollbackAsync();
}
