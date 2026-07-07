using Neo4j.Driver;

using LoreWeave.Domain.Entities.Boards;
using LoreWeave.Domain.Entities.Boards.Commands;
using LoreWeave.Domain.Extensions;
using LoreWeave.Domain.Models;
using LoreWeave.Domain.Repositories.Boards;
using LoreWeave.Domain.Transactions;
using LoreWeave.Infrastructure.Repositories.Extensions;
using LoreWeave.Infrastructure.Transactions;

namespace LoreWeave.Infrastructure.Repositories.Boards;

public class BoardRepository : IExistsBoard, IBoardReader, IBoardWriter
{
    private const string BoardReturnClause = @"
        RETURN
            b.Id AS Id,
            b.Name AS Name,
            b.CharacterNodeColor AS CharacterNodeColor,
            b.FactNodeColor AS FactNodeColor,
            b.RelationEdgeColor AS RelationEdgeColor,
            b.FactEdgeColor AS FactEdgeColor,
            b.PathHighlightColor AS PathHighlightColor,
            b.NodeRadius AS NodeRadius,
            b.EdgeWidth AS EdgeWidth,
            b.CurvedEdges AS CurvedEdges,
            b.ShowGrid AS ShowGrid,
            b.ScalingObjects AS ScalingObjects,
            b.Version AS Version";

    public async Task CreateAsync(ITransaction transaction, CreateBoard createBoard, BoardConfiguration configuration)
    {
        const string queryString = @"
            CREATE (b:Board {
                Id: $Id,
                Name: $Name,
                Version: 1,
                CharacterNodeColor: $CharacterNodeColor,
                FactNodeColor: $FactNodeColor,
                RelationEdgeColor: $RelationEdgeColor,
                FactEdgeColor: $FactEdgeColor,
                PathHighlightColor: $PathHighlightColor,
                NodeRadius: $NodeRadius,
                EdgeWidth: $EdgeWidth,
                CurvedEdges: $CurvedEdges,
                ShowGrid: $ShowGrid,
                ScalingObjects: $ScalingObjects
            })";
        var query = new Query(queryString, new
        {
            Id = createBoard.Id.ToDatabaseId(),
            createBoard.Name,
            configuration.CharacterNodeColor,
            configuration.FactNodeColor,
            configuration.RelationEdgeColor,
            configuration.FactEdgeColor,
            configuration.PathHighlightColor,
            configuration.NodeRadius,
            configuration.EdgeWidth,
            configuration.CurvedEdges,
            configuration.ShowGrid,
            configuration.ScalingObjects
        });

        await transaction.AsNeo4jTransaction().RunAsync(query);
    }

    public async Task<EntityExistence> BoardExistsAsync(ITransaction transaction, Guid id)
    {
        const string queryString = @"
            MATCH (b:Board {Id: $Id })
            RETURN b IS NOT NULL AS Exists, coalesce(b.Version, 0) AS Version";
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

    public async Task<Board> GetAsync(ITransaction transaction, Guid id)
    {
        const string queryString = $@"
            MATCH (b:Board {{Id: $Id}})
            {BoardReturnClause}";
        var query = new Query(queryString, new
        {
            Id = id.ToDatabaseId()
        });

        var cursorResult = await transaction.AsNeo4jTransaction().RunAsync(query);

        var board = await cursorResult
            .SingleAsync(record
                => record.ToBoard());

        return board;
    }

    public async Task<IReadOnlyCollection<Board>> GetAllAsync(ITransaction transaction)
    {
        // The contract caps the board list at 100 items.
        const string queryString = $@"
            MATCH (b:Board)
            WITH b
            ORDER BY b.Name ASC, b.Id ASC
            LIMIT 100
            {BoardReturnClause}";
        var query = new Query(queryString);

        var cursorResult = await transaction.AsNeo4jTransaction().RunAsync(query);

        var boards = await cursorResult.ToListAsync(record => record.ToBoard());

        return boards.AsReadOnly();
    }

    public async Task UpdateAsync(ITransaction transaction, Guid id, UpdateBoard updateBoard)
    {
        const string queryString = @"
            MATCH (b:Board {Id: $Id })
            SET
                b.Name = $Name,
                b.CharacterNodeColor = $CharacterNodeColor,
                b.FactNodeColor = $FactNodeColor,
                b.RelationEdgeColor = $RelationEdgeColor,
                b.FactEdgeColor = $FactEdgeColor,
                b.PathHighlightColor = $PathHighlightColor,
                b.NodeRadius = $NodeRadius,
                b.EdgeWidth = $EdgeWidth,
                b.CurvedEdges = $CurvedEdges,
                b.ShowGrid = $ShowGrid,
                b.ScalingObjects = $ScalingObjects,
                b.Version = b.Version + 1";
        var query = new Query(queryString, new
        {
            Id = id.ToDatabaseId(),
            updateBoard.Name,
            updateBoard.Configuration.CharacterNodeColor,
            updateBoard.Configuration.FactNodeColor,
            updateBoard.Configuration.RelationEdgeColor,
            updateBoard.Configuration.FactEdgeColor,
            updateBoard.Configuration.PathHighlightColor,
            updateBoard.Configuration.NodeRadius,
            updateBoard.Configuration.EdgeWidth,
            updateBoard.Configuration.CurvedEdges,
            updateBoard.Configuration.ShowGrid,
            updateBoard.Configuration.ScalingObjects
        });

        await transaction.AsNeo4jTransaction().RunAsync(query);
    }

    public async Task DeleteAsync(ITransaction transaction, DeleteBoard deleteBoard)
    {
        // Cascade: the board owns every Character and Fact carrying its
        // BoardId; DETACH DELETE also removes KNOWS and HAS_FACT edges,
        // including facts already orphaned from all characters.
        const string queryString = @"
            MATCH (b:Board {Id: $Id })
            OPTIONAL MATCH (n) WHERE (n:Character OR n:Fact) AND n.BoardId = b.Id
            DETACH DELETE b, n";
        var query = new Query(queryString, new
        {
            Id = deleteBoard.Id.ToDatabaseId()
        });

        await transaction.AsNeo4jTransaction().RunAsync(query);
    }
}
