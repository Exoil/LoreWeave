// V002 — Fact node + HAS_FACT relationship
//
// ── Fact node ───────────────────────────────────────────────────────────────
// A Fact records a piece of lore (Title, Content) about a Character. Fact.Id is
// the internal GUID stored as a database string (see Domain.Extensions.ToDatabaseId)
// and must be globally unique. Community edition supports node uniqueness
// constraints, so this is enforced at the DB level.

CREATE CONSTRAINT fact_UQ_id IF NOT EXISTS
FOR (f:Fact) REQUIRE f.Id IS UNIQUE;

// ── HAS_FACT relationship ───────────────────────────────────────────────────
// HAS_FACT connects a Character to its Fact and is DIRECTED:
//   (ch:Character)-[:HAS_FACT]->(f:Fact)   means `ch` owns the fact `f`.
// The edge carries NO properties, so there is nothing to index or constrain on
// it. Facts are created in CharacterRepository.CreateAsync via:
//   MATCH (ch:Character {Id: $CharacterId})
//   CREATE (f:Fact {...})
//   CREATE (ch)-[:HAS_FACT]->(f)
