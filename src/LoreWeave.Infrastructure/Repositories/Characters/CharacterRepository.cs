using System.Text;

using Neo4j.Driver;

using LoreWeave.Domain.Entities.Characters;
using LoreWeave.Domain.Entities.Characters.Commands;
using LoreWeave.Domain.Entities.Characters.Queries;
using LoreWeave.Domain.Extensions;
using LoreWeave.Domain.Models;
using LoreWeave.Domain.Repositories.Characters;
using LoreWeave.Domain.Transactions;
using LoreWeave.Infrastructure.Repositories.Extensions;
using LoreWeave.Infrastructure.Transactions;

namespace LoreWeave.Infrastructure.Repositories.Characters;

public class CharacterRepository : IExistsCharacter, ICharacterReader, ICharacterWriter
{
    public async Task CreateAsync(ITransaction transaction, CreateCharacter createCharacter)
    {
        const string queryString = @"
            CREATE (ch:Character {Id: $CharacterId, Name: $Name, Version: 1})
            RETURN ID(ch) AS CharacterNodeId";
        var query = new Query(queryString,
            new
            {
                CharacterId = createCharacter.Id.ToDatabaseId(),
                createCharacter.Name
            });

        await transaction.AsNeo4jTransaction().RunAsync(query);
    }

    public async Task UpdateAsync(ITransaction transaction, Guid id, UpdateCharacter updateCharacter)
    {
        const string queryString = @"
            MATCH (ch:Character {Id: $CharacterId })
            SET
                ch.Name = $Name,
                ch.Version = ch.Version + 1
            RETURN ID(ch) AS CharacterNodeId";
        var query = new Query(queryString, new
        {
            CharacterId = id.ToDatabaseId(),
            updateCharacter.Name
        });

        await transaction.AsNeo4jTransaction().RunAsync(query);
    }

    public async Task<EntityExistence> CharacterExistsAsync(ITransaction transaction, Guid id)
    {
        const string queryString = @"
            MATCH (ch:Character {Id: $Id })
            RETURN ch IS NOT NULL AS Exists, coalesce(ch.Version, 0) AS Version";
        var query = new Query(queryString, new
        {
            Id = id.ToDatabaseId()
        });

        var cursorResult = await transaction.AsNeo4jTransaction().RunAsync(query);

        var records = await cursorResult.ToListAsync();

        if (records.Count == 0)
        {
            return new EntityExistence(false, 0);
        }

        var record = records[0];

        return new EntityExistence(record["Exists"].As<bool>(), (ushort)record["Version"].As<int>());
    }

    public async Task DeleteAsync(ITransaction transaction, DeleteCharacter deleteCharacter)
    {
        const string queryString = @"
            MATCH (ch:Character {Id: $Id })
            DETACH DELETE ch";
        var query = new Query(queryString, new
        {
            Id = deleteCharacter.Id.ToDatabaseId()
        });

        await transaction.AsNeo4jTransaction().RunAsync(query);
    }

    public async Task<Character> GetAsync(ITransaction transaction, Guid id)
    {
        const string queryString = @"
            MATCH (ch:Character {Id: $Id})
            RETURN ch.Id AS Id, ch.Name AS Name, ch.Version AS Version";
        var query = new Query(queryString, new
        {
            Id = id.ToDatabaseId()
        });

        var cursorResult = await transaction.AsNeo4jTransaction().RunAsync(query);

        var character = await cursorResult
            .SingleAsync(record
                => record.ToCharacter());

        return character;
    }

    public async Task<IReadOnlyCollection<CharacterWithKnowRelation>> GetPageAsync(
        ITransaction transaction,
        GetCharacterPage characterPage,
        CharacterSearchFilter searchFilter)
    {
        var skip = (int)((characterPage.Page - 1) * characterPage.Size);
        var limit = (int)characterPage.Size;

        var queryStringBuilder = new StringBuilder(
            "MATCH (ch:Character)");

        queryStringBuilder
            .AppendLine("WHERE $NameFilter = '' OR toLower(ch.Name) CONTAINS toLower($NameFilter)")
            .AppendLine("OPTIONAL MATCH (ch)-[r:KNOWS]->(toCh:Character)")
            .AppendLine("WITH ch, collect(CASE WHEN toCh IS NULL THEN null ELSE {Id: toCh.Id, Description: r.Description, IsStrong: r.IsStrong} END) AS KnowRelations")
            .AppendLine("ORDER BY")
            .AppendLine("CASE WHEN $SortType = 'Id' AND $SortOrder = 'Asc' THEN ch.Id END ASC,")
            .AppendLine("CASE WHEN $SortType = 'Id' AND $SortOrder = 'Desc' THEN ch.Id END DESC,")
            .AppendLine("CASE WHEN $SortType = 'Name' AND $SortOrder = 'Asc' THEN ch.Name END ASC,")
            .AppendLine("CASE WHEN $SortType = 'Name' AND $SortOrder = 'Desc' THEN ch.Name END DESC")
            .AppendLine("SKIP $Skip")
            .AppendLine("LIMIT $Limit")
            .AppendLine("RETURN ch.Id AS Id, ch.Name AS Name, KnowRelations");

        var query = new Query(queryStringBuilder.ToString(), new
        {
            characterPage.SortType,
            characterPage.SortOrder,
            Skip = skip,
            Limit = limit,
            NameFilter = searchFilter.Name ?? ""
        });

        var cursorResult = await transaction.AsNeo4jTransaction().RunAsync(query);

        var characters = await cursorResult.ToListAsync(record => record.ToCharacterWithKnowRelation());

        return characters.AsReadOnly();
    }
}
