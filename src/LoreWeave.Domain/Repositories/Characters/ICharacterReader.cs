using LoreWeave.Domain.Entities.Characters;
using LoreWeave.Domain.Entities.Characters.Queries;
using LoreWeave.Domain.Models;
using LoreWeave.Domain.Transactions;

namespace LoreWeave.Domain.Repositories.Characters;

public interface ICharacterReader
{
    Task<Character> GetAsync(ITransaction transaction, Guid id);

    Task<IReadOnlyCollection<CharacterWithKnowRelation>> GetPageAsync(
        ITransaction transaction,
        GetCharacterPage characterPage,
        CharacterSearchFilter searchFilter);
}