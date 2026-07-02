using LoreWeave.Domain.Entities.Knows;
using LoreWeave.Domain.Entities.Knows.Commands;
using LoreWeave.Domain.Models;

using Neo4j.Driver;

namespace LoreWeave.Domain.Repositories;

public interface IKnowRepository
{
    Task CreateKnowRelationAsync(IAsyncTransaction transaction, CreateKnowRelation createKnowRelation);

    Task<EntityExistence> KnowRelationExistsAsync(
        IAsyncTransaction transaction,
        Guid fromCharacterId,
        Guid toCharacterId);

    Task<KnowRelation> GetKnowRelationAsync(
        IAsyncTransaction transaction,
        Guid fromCharacterId,
        Guid toCharacterId);

    Task UpdateKnowRelationAsync(IAsyncTransaction transaction, UpdateKnowRelation updateKnowRelation);

    Task DeleteKnowRelationAsync(IAsyncTransaction transaction, DeleteKnowRelation createKnowRelation);

    Task<IReadOnlyCollection<Guid>> FindPathBetweenCharactersAsync(
        IAsyncTransaction transaction,
        Guid fromCharacterId,
        Guid toCharacterId,
        int maxHops);
}
