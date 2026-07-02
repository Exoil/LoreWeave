using LoreWeave.Domain.Models;

using Neo4j.Driver;

namespace LoreWeave.Domain.Repositories;

public interface IExistsCharacter
{
    Task<EntityExistence> CharacterExistsAsync(IAsyncTransaction transaction, Guid id);
}
