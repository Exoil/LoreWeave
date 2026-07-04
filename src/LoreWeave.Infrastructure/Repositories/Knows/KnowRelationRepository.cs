using Neo4j.Driver;

using LoreWeave.Domain.Entities.Knows;
using LoreWeave.Domain.Entities.Knows.Commands;
using LoreWeave.Domain.Extensions;
using LoreWeave.Domain.Models;
using LoreWeave.Domain.Repositories.Knows;
using LoreWeave.Domain.Transactions;
using LoreWeave.Infrastructure.Repositories.Extensions;
using LoreWeave.Infrastructure.Transactions;

namespace LoreWeave.Infrastructure.Repositories.Knows;

public class KnowRelationRepository : IExistsKnowRelation, IKnowRelationReader, IKnowRelationWriter
{
    public async Task CreateKnowRelationAsync(ITransaction transaction, CreateKnowRelation createKnowRelation)
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

        await transaction.AsNeo4jTransaction().RunAsync(query);
    }

    public async Task<EntityExistence> KnowRelationExistsAsync(
        ITransaction transaction,
        Guid fromCharacterId,
        Guid toCharacterId)
    {
        const string queryString = @"
            MATCH (fromCh:Character {Id: $FromCharacterId})-[r:KNOWS]->(toCh:Character {Id: $ToCharacterId})
            RETURN r IS NOT NULL AS Exists, coalesce(r.Version, 0) AS Version";
        var query = new Query(queryString, new
        {
            FromCharacterId = fromCharacterId.ToDatabaseId(),
            ToCharacterId = toCharacterId.ToDatabaseId()
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

    public async Task<KnowRelation> GetKnowRelationAsync(
        ITransaction transaction,
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

        var cursorResult = await transaction.AsNeo4jTransaction().RunAsync(query);

        var knowRelation = await cursorResult
            .SingleAsync(record => record.ToKnowRelation());

        return knowRelation;
    }

    public async Task UpdateKnowRelationAsync(ITransaction transaction, UpdateKnowRelation updateKnowRelation)
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

        await transaction.AsNeo4jTransaction().RunAsync(query);
    }

    public async Task DeleteKnowRelationAsync(ITransaction transaction, DeleteKnowRelation deleteKnowRelation)
    {
        const string queryString = @"
            MATCH (fromCh:Character {Id: $FromCharacterId})-[r:KNOWS]->(toCh:Character {Id: $ToCharacterId})
            DELETE r";
        var query = new Query(
            queryString,
            new
            {
                FromCharacterId = deleteKnowRelation.FromCharacterId.ToDatabaseId(),
                ToCharacterId = deleteKnowRelation.ToCharacterId.ToDatabaseId()
            });

        await transaction.AsNeo4jTransaction().RunAsync(query);
    }

    public async Task<IReadOnlyCollection<Guid>> FindPathBetweenCharactersAsync(
        ITransaction transaction,
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

        var cursorResult = await transaction.AsNeo4jTransaction().RunAsync(query);
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
}
