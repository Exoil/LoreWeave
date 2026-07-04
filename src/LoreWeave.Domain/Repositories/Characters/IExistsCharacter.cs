using LoreWeave.Domain.Models;
using LoreWeave.Domain.Transactions;

namespace LoreWeave.Domain.Repositories.Characters;

public interface IExistsCharacter
{
    Task<EntityExistence> CharacterExistsAsync(ITransaction transaction, Guid id);
}