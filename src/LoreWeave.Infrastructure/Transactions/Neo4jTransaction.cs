using Neo4j.Driver;

using LoreWeave.Domain.Transactions;

namespace LoreWeave.Infrastructure.Transactions;

public sealed class Neo4jTransaction : ITransaction
{
    public Neo4jTransaction(IAsyncTransaction asyncTransaction) => AsyncTransaction = asyncTransaction;

    internal IAsyncTransaction AsyncTransaction { get; }

    public Task CommitAsync() => AsyncTransaction.CommitAsync();

    public Task RollbackAsync() => AsyncTransaction.RollbackAsync();

    public ValueTask DisposeAsync() => AsyncTransaction.DisposeAsync();
}
