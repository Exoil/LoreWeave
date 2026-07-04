using Neo4j.Driver;

using LoreWeave.Domain.Transactions;

namespace LoreWeave.Infrastructure.Transactions;

internal static class TransactionExtensions
{
    public static IAsyncTransaction AsNeo4jTransaction(this ITransaction transaction)
        => transaction is Neo4jTransaction neo4jTransaction
            ? neo4jTransaction.AsyncTransaction
            : throw new InvalidOperationException(
                $"Transaction of type '{transaction.GetType().Name}' cannot be used with Neo4j repositories.");
}
