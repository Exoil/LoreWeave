using LoreWeave.Domain.Entities.Characters.Commands;
using LoreWeave.Domain.Transactions;

namespace LoreWeave.Domain.Repositories.Characters;

public interface ICharacterWriter
{
    Task CreateAsync(ITransaction transaction, Guid boardId, CreateCharacter createCharacter);

    Task UpdateAsync(ITransaction transaction, Guid id, UpdateCharacter updateCharacter);

    Task DeleteAsync(ITransaction transaction, DeleteCharacter deleteCharacter);
}
