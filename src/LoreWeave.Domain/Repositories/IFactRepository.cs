using LoreWeave.Domain.Entities.Facts;
using LoreWeave.Domain.Entities.Facts.Commands;
using LoreWeave.Domain.Models;

using Neo4j.Driver;

namespace LoreWeave.Domain.Repositories;

public interface IFactRepository
{
    Task CreateAsync(IAsyncTransaction transaction, Guid characterId, CreateFact createFact);

    Task<EntityExistence> FactExistsAsync(IAsyncTransaction transaction, Guid id);

    Task<Fact> GetFactAsync(IAsyncTransaction transaction, Guid id);

    Task UpdateAsync(IAsyncTransaction transaction, UpdateFact updateFact);

    Task DeleteAsync(IAsyncTransaction transaction, DeleteFact deleteFact);

    Task ConnectFactToCharacterAsync(IAsyncTransaction transaction, Guid characterId, Guid factId);
}