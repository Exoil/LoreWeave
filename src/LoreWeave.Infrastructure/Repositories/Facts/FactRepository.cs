using Neo4j.Driver;

using LoreWeave.Domain.Entities.Facts;
using LoreWeave.Domain.Entities.Facts.Commands;
using LoreWeave.Domain.Extensions;
using LoreWeave.Domain.Models;
using LoreWeave.Domain.Repositories.Facts;
using LoreWeave.Domain.Transactions;
using LoreWeave.Infrastructure.Repositories.Extensions;
using LoreWeave.Infrastructure.Transactions;

namespace LoreWeave.Infrastructure.Repositories.Facts;

public class FactRepository : IExistsFact, IFactReader, IFactWriter, IFactConnection
{
    public async Task CreateAsync(ITransaction transaction, Guid characterId, CreateFact createFact)
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

        await transaction.AsNeo4jTransaction().RunAsync(query);
    }

    public async Task<EntityExistence> FactExistsAsync(ITransaction transaction, Guid id)
    {
        const string queryString = @"
            MATCH (f:Fact {Id: $Id })
            RETURN f IS NOT NULL AS Exists, coalesce(f.Version, 0) AS Version";
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

    public async Task<Fact> GetFactAsync(ITransaction transaction, Guid id)
    {
        const string queryString = @"
            MATCH (f:Fact {Id: $Id})
            RETURN f.Id AS Id, f.Title AS Title, f.Content AS Content, f.Version AS Version";
        var query = new Query(queryString, new
        {
            Id = id.ToDatabaseId()
        });

        var cursorResult = await transaction.AsNeo4jTransaction().RunAsync(query);

        var fact = await cursorResult
            .SingleAsync(record
                => record.ToFact());

        return fact;
    }

    public async Task UpdateAsync(ITransaction transaction, UpdateFact updateFact)
    {
        const string queryString = @"
            MATCH (f:Fact {Id: $Id })
            SET
                f.Title = $Title,
                f.Content = $Content,
                f.Version = f.Version + 1";
        var query = new Query(queryString, new
        {
            Id = updateFact.Id.ToDatabaseId(),
            updateFact.Title,
            updateFact.Content
        });

        await transaction.AsNeo4jTransaction().RunAsync(query);
    }

    public async Task DeleteAsync(ITransaction transaction, DeleteFact deleteFact)
    {
        const string queryString = @"
            MATCH (f:Fact {Id: $Id })
            DETACH DELETE f";
        var query = new Query(queryString, new
        {
            Id = deleteFact.Id.ToDatabaseId()
        });

        await transaction.AsNeo4jTransaction().RunAsync(query);
    }

    public async Task ConnectFactToCharacterAsync(ITransaction transaction, Guid characterId, Guid factId)
    {
        const string queryString = @"
            MATCH (ch:Character {Id: $CharacterId}), (f:Fact {Id: $FactId})
            MERGE (ch)-[:HAS_FACT]->(f)";
        var query = new Query(queryString, new
        {
            CharacterId = characterId.ToDatabaseId(),
            FactId = factId.ToDatabaseId()
        });

        await transaction.AsNeo4jTransaction().RunAsync(query);
    }

    public async Task<bool> FactConnectionExistsAsync(ITransaction transaction, Guid characterId, Guid factId)
    {
        const string queryString = @"
            MATCH (ch:Character {Id: $CharacterId})-[r:HAS_FACT]->(f:Fact {Id: $FactId})
            RETURN count(r) > 0 AS Exists";
        var query = new Query(queryString, new
        {
            CharacterId = characterId.ToDatabaseId(),
            FactId = factId.ToDatabaseId()
        });

        var cursorResult = await transaction.AsNeo4jTransaction().RunAsync(query);

        var records = await cursorResult.ToListAsync();

        return records.Count > 0 && records[0]["Exists"].As<bool>();
    }

    public async Task DisconnectFactFromCharacterAsync(ITransaction transaction, Guid characterId, Guid factId)
    {
        const string queryString = @"
            MATCH (ch:Character {Id: $CharacterId})-[r:HAS_FACT]->(f:Fact {Id: $FactId})
            DELETE r";
        var query = new Query(queryString, new
        {
            CharacterId = characterId.ToDatabaseId(),
            FactId = factId.ToDatabaseId()
        });

        await transaction.AsNeo4jTransaction().RunAsync(query);
    }
}
