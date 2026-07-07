// V003 — Board node + board membership of Character/Fact
//
// ── Board node ──────────────────────────────────────────────────────────────
// A Board is one graph of one RPG game. It carries the name and the visual
// configuration set by the GM (colours, node radius, edge width, view flags).
// Board.Id is the internal GUID stored as a database string (see
// Domain.Extensions.ToDatabaseId) and must be globally unique. Community
// edition supports node uniqueness constraints, so this is enforced at the
// DB level.

CREATE CONSTRAINT board_UQ_id IF NOT EXISTS
FOR (b:Board) REQUIRE b.Id IS UNIQUE;

// ── Board membership ────────────────────────────────────────────────────────
// Every Character and Fact belongs to exactly ONE board via a BoardId property
// holding the owning Board.Id. All data queries filter on it, so both get a
// range index. KNOWS relationships need no membership of their own — they only
// ever connect Character nodes of the same board (enforced in application
// code, which scopes both endpoints by BoardId before creating the edge).

CREATE INDEX character_IDX_boardid IF NOT EXISTS
FOR (ch:Character) ON (ch.BoardId);

CREATE INDEX fact_IDX_boardid IF NOT EXISTS
FOR (f:Fact) ON (f.BoardId);

// ── Data migration: attach pre-board data to a default board ────────────────
// Databases created before boards existed hold Character/Fact nodes without a
// BoardId. After the API paths moved under /v1/boards/{boardId} those nodes
// would be unreachable, so they are attached to a "Default board" created with
// the server-side default configuration (identical to the palette the frontend
// used to hard-code — existing graphs look the same after the migration).
//
// The board is only created when orphaned nodes actually exist, so fresh
// databases (including integration-test containers) stay empty. The fixed Id
// keeps the statement idempotent.

MATCH (n) WHERE (n:Character OR n:Fact) AND n.BoardId IS NULL
WITH count(n) AS orphanCount
WHERE orphanCount > 0
MERGE (b:Board {Id: '00000000-0000-4000-8000-000000000001'})
ON CREATE SET
    b.Name = 'Default board',
    b.Version = 1,
    b.CharacterNodeColor = '#4466cc',
    b.FactNodeColor = '#d97706',
    b.RelationEdgeColor = '#aaaaaa',
    b.FactEdgeColor = '#d9a066',
    b.PathHighlightColor = '#a855f7',
    b.NodeRadius = 16,
    b.EdgeWidth = 3,
    b.CurvedEdges = true,
    b.ShowGrid = true,
    b.ScalingObjects = true;

MATCH (b:Board {Id: '00000000-0000-4000-8000-000000000001'})
MATCH (n) WHERE (n:Character OR n:Fact) AND n.BoardId IS NULL
SET n.BoardId = b.Id;
