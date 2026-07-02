using System.Text;

using Neo4j.Driver;

using LoreWeave.Domain.Entities.Characters;
using LoreWeave.Domain.Entities.Characters.Commands;
using LoreWeave.Domain.Entities.Characters.Queries;
using LoreWeave.Domain.Entities.Facts.Commands;
using LoreWeave.Domain.Entities.Knows;
using LoreWeave.Domain.Entities.Knows.Commands;
using LoreWeave.Domain.Extensions;
using LoreWeave.Domain.Models;
using LoreWeave.Domain.Repositories;
using LoreWeave.Infrastructure.Repositories.Extensions;

namespace LoreWeave.Infrastructure.Repositories;

public class CharacterRepository : ICharacterRepository
{
    public async Task CreateAsync(IAsyncTransaction transaction, CreateCharacter createCharacter)
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

        await transaction.RunAsync(query);
    }

    public async Task UpdateAsync(IAsyncTransaction transaction, Guid id, UpdateCharacter updateCharacter)
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

        await transaction.RunAsync(query);
    }

    public async Task<EntityExistence> CharacterExistsAsync(IAsyncTransaction transaction, Guid id)
    {
        const string queryString = @"
            MATCH (ch:Character {Id: $Id })
            RETURN ch IS NOT NULL AS Exists, coalesce(ch.Version, -1) AS Version";
        var query = new Query(queryString, new
        {
            Id = id.ToDatabaseId()
        });

        var cursorResult = await transaction.RunAsync(query);

        var records = await cursorResult.ToListAsync();

        if (records.Count == 0)
        {
            return new EntityExistence(false, -1);
        }

        var record = records[0];

        return new EntityExistence(record["Exists"].As<bool>(), record["Version"].As<int>());
    }

    public async Task DeleteAsync(IAsyncTransaction transaction, DeleteCharacter deleteCharacter)
    {
        const string queryString = @"
            MATCH (ch:Character {Id: $Id })
            DETACH DELETE ch";
        var query = new Query(queryString, new
        {
            Id = deleteCharacter.Id.ToDatabaseId()
        });

        await transaction.RunAsync(query);
    }

    public async Task<Character> GetAsync(IAsyncTransaction transaction, Guid id)
    {
        const string queryString = @"
            MATCH (ch:Character {Id: $Id})
            RETURN ch.Id AS Id, ch.Name AS Name, ch.Version AS Version";
        var query = new Query(queryString, new
        {
            Id = id.ToDatabaseId()
        });

        var cursorResult = await transaction.RunAsync(query);

        var character = await cursorResult
            .SingleAsync(record
                => record.ToCharacter());

        return character;
    }

    public async Task<IReadOnlyCollection<CharacterWithKnowRelation>> GetAsync(
        IAsyncTransaction transaction,
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

        var cursorResult = await transaction.RunAsync(query);

        var characters = await cursorResult.ToListAsync(record => record.ToCharacterWithKnowRelation());

        return characters.AsReadOnly();
    }

    public async Task CreateKnowRelationAsync(IAsyncTransaction transaction, CreateKnowRelation createKnowRelation)
    {
        const string queryString = @"
            MATCH (fromCh:Character {Id: $FromCharacterId}), (toCh:Character {Id: $ToCharacterId})
            MERGE (fromCh)-[r:KNOWS]->(toCh)
            SET r.Id = $Id, r.Version = $Version, r.IsStrong = $IsStrong, r.Description = $Description";
        var query = new Query(
            queryString,
            new
            {
                Id = createKnowRelation.Id.ToDatabaseId(),
                FromCharacterId = createKnowRelation.FromCharacterId.ToDatabaseId(),
                ToCharacterId = createKnowRelation.ToCharacterId.ToDatabaseId(),
                createKnowRelation.Description,
                IsStrong = createKnowRelation.IsStrongRelation,
                Version = 1
            });

        await transaction.RunAsync(query);
    }

    public async Task<EntityExistence> KnowRelationExistsAsync(
        IAsyncTransaction transaction,
        Guid fromCharacterId,
        Guid toCharacterId)
    {
        const string queryString = @"
            MATCH (fromCh:Character {Id: $FromCharacterId})-[r:KNOWS]->(toCh:Character {Id: $ToCharacterId})
            RETURN r IS NOT NULL AS Exists, coalesce(r.Version, -1) AS Version";
        var query = new Query(queryString, new
        {
            FromCharacterId = fromCharacterId.ToDatabaseId(),
            ToCharacterId = toCharacterId.ToDatabaseId()
        });

        var cursorResult = await transaction.RunAsync(query);

        var records = await cursorResult.ToListAsync();

        if (records.Count == 0)
        {
            return new EntityExistence(false, -1);
        }

        var record = records[0];

        return new EntityExistence(record["Exists"].As<bool>(), record["Version"].As<int>());
    }

    public async Task<KnowRelation> GetKnowRelationAsync(
        IAsyncTransaction transaction,
        Guid fromCharacterId,
        Guid toCharacterId)
    {
        const string queryString = @"
            MATCH (fromCh:Character {Id: $FromCharacterId})-[r:KNOWS]->(toCh:Character {Id: $ToCharacterId})
            RETURN
                r.Id AS Id,
                r.Description AS Description,
                r.IsStrong AS IsStrong,
                r.Version AS Version,
                fromCh.Id AS FromCharacterId,
                toCh.Id AS ToCharacterId";
        var query = new Query(queryString, new
        {
            FromCharacterId = fromCharacterId.ToDatabaseId(),
            ToCharacterId = toCharacterId.ToDatabaseId()
        });

        var cursorResult = await transaction.RunAsync(query);

        var knowRelation = await cursorResult
            .SingleAsync(record => record.ToKnowRelation());

        return knowRelation;
    }

    public async Task UpdateKnowRelationAsync(IAsyncTransaction transaction, UpdateKnowRelation updateKnowRelation)
    {
        const string queryString = @"
            MATCH (fromCh:Character {Id: $FromCharacterId})-[r:KNOWS]->(toCh:Character {Id: $ToCharacterId})
            SET
                r.Description = $Description,
                r.IsStrong = $IsStrong,
                r.Version = r.Version + 1";
        var query = new Query(
            queryString,
            new
            {
                FromCharacterId = updateKnowRelation.FromCharacterId.ToDatabaseId(),
                ToCharacterId = updateKnowRelation.ToCharacterId.ToDatabaseId(),
                updateKnowRelation.Description,
                IsStrong = updateKnowRelation.IsStrongRelation
            });

        await transaction.RunAsync(query);
    }

    public async Task DeleteKnowRelationAsync(IAsyncTransaction transaction, DeleteKnowRelation createKnowRelation)
    {
        const string queryString = @"
            MATCH (fromCh:Character {Id: $FromCharacterId})-[r:KNOWS]->(toCh:Character {Id: $ToCharacterId})
            DELETE r";
        var query = new Query(
            queryString,
            new
            {
                FromCharacterId = createKnowRelation.FromCharacterId.ToDatabaseId(),
                ToCharacterId = createKnowRelation.ToCharacterId.ToDatabaseId()
            });

        await transaction.RunAsync(query);
    }

    public async Task<IReadOnlyCollection<Guid>> FindPathBetweenCharactersAsync(
        IAsyncTransaction transaction,
        Guid fromCharacterId,
        Guid toCharacterId,
        int maxHops)
    {
        var queryString = $@"
            MATCH path = shortestPath(
                (from:Character {{Id: $FromCharacterId}})-[:KNOWS*..{maxHops}]-(to:Character {{Id: $ToCharacterId}})
            )
            RETURN [node IN nodes(path) | node.Id] AS CharacterIds";

        var query = new Query(queryString, new
        {
            FromCharacterId = fromCharacterId.ToDatabaseId(),
            ToCharacterId = toCharacterId.ToDatabaseId()
        });

        var cursorResult = await transaction.RunAsync(query);
        var records = await cursorResult.ToListAsync();

        if (records.Count == 0)
        {
            return Array.Empty<Guid>();
        }

        return records[0]["CharacterIds"]
            .As<List<string>>()
            .Select(id => id.DatabaseIdToGuid())
            .ToList()
            .AsReadOnly();
    }

    public async Task CreateAsync(IAsyncTransaction transaction, Guid characterId, CreateFact createFact)
    {
        const string queryString = @"
            MATCH (ch:Character {Id: $CharacterId})
            CREATE (f:Fact {Id: $Id, Title: $Title, Content: $Content, Version: 1})
            CREATE (ch)-[:HAS_FACT]->(f)";
        var query = new Query(queryString, new
        {
            CharacterId = characterId.ToDatabaseId(),
            Id = createFact.Id.ToDatabaseId(),
            createFact.Title,
            createFact.Content
        });

        await transaction.RunAsync(query);
    }
}
