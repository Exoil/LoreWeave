using LoreWeave.Domain.Entities.Facts.Commands;

using Neo4j.Driver;

namespace LoreWeave.Domain.Repositories;

public interface IFactRepository
{
    Task CreateAsync(IAsyncTransaction transaction, Guid characterId, CreateFact createFact);
}
