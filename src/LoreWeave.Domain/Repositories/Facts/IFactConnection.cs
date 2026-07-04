using LoreWeave.Domain.Transactions;

namespace LoreWeave.Domain.Repositories.Facts;

public interface IFactConnection
{
    Task ConnectFactToCharacterAsync(ITransaction transaction, Guid characterId, Guid factId);

    Task<bool> FactConnectionExistsAsync(ITransaction transaction, Guid characterId, Guid factId);

    Task DisconnectFactFromCharacterAsync(ITransaction transaction, Guid characterId, Guid factId);
}
