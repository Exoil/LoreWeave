using Neo4j.Driver;

using LoreWeave.Domain.Entities.Characters;
using LoreWeave.Domain.Entities.Characters.Commands;
using LoreWeave.Domain.Entities.Characters.Queries;
using LoreWeave.Domain.Entities.Knows;
using LoreWeave.Domain.Entities.Knows.Commands;
using LoreWeave.Domain.Models;

namespace LoreWeave.Domain.Repositories;

public interface ICharacterRepository 
    : IFactRepository, IKnowRepository, IExistsCharacter
{
    Task CreateAsync(IAsyncTransaction transaction, CreateCharacter createCharacter);

    Task UpdateAsync(IAsyncTransaction transaction, Guid id, UpdateCharacter updateCharacter);

    Task DeleteAsync(IAsyncTransaction transaction, DeleteCharacter deleteCharacter);

    Task<Character> GetAsync(IAsyncTransaction transaction, Guid id);

    Task<IReadOnlyCollection<CharacterWithKnowRelation>> GetAsync(
        IAsyncTransaction transaction,
        GetCharacterPage characterPage,
        CharacterSearchFilter searchFilter);
}
