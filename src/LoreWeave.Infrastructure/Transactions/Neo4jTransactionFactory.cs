using Neo4j.Driver;

using LoreWeave.Domain.Transactions;

namespace LoreWeave.Infrastructure.Transactions;

public sealed class Neo4jTransactionFactory : ITransactionFactory, IAsyncDisposable, IDisposable
{
    private bool _disposed;
    private IAsyncSession _session;

    public Neo4jTransactionFactory(IAsyncSession session) => _session = session;

    public async Task<ITransaction> CreateAsync()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        return new Neo4jTransaction(await _session.BeginTransactionAsync());
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _session.Dispose();
        _session = null!;
        _disposed = true;
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        await _session.DisposeAsync();
        _session = null!;
        _disposed = true;
    }
}
