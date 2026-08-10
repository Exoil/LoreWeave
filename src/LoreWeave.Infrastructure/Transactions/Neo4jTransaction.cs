using System.Diagnostics.CodeAnalysis;

using Neo4j.Driver;

using LoreWeave.Domain.Transactions;

namespace LoreWeave.Infrastructure.Transactions;

[SuppressMessage("Minor Code Smell", "S101:Types should be named in PascalCase",
    Justification = "Neo4j is the vendor's own spelling of the product name; Neo4J would be wrong.")]
public sealed class Neo4jTransaction : ITransaction
{
    public Neo4jTransaction(IAsyncTransaction asyncTransaction) => AsyncTransaction = asyncTransaction;

    internal IAsyncTransaction AsyncTransaction { get; }

    public Task CommitAsync() => AsyncTransaction.CommitAsync();

    public Task RollbackAsync() => AsyncTransaction.RollbackAsync();

    public ValueTask DisposeAsync() => AsyncTransaction.DisposeAsync();
}
