using Neo4j.Driver;

using LoreWeave.Domain.Entities.Boards;
using LoreWeave.Domain.Entities.Boards.Commands;
using LoreWeave.Domain.Entities.Characters;
using LoreWeave.Domain.Entities.Characters.Commands;
using LoreWeave.Domain.Entities.Facts;
using LoreWeave.Domain.Entities.Facts.Commands;
using LoreWeave.Domain.Entities.Knows;
using LoreWeave.Domain.Extensions;
using LoreWeave.Domain.Models;

namespace LoreWeave.Infrastructure.Repositories.Extensions;

public static class RecordExtensions
{
    public static Board ToBoard(this IRecord record)
    {
        var createBoard = new CreateBoard(
            record["Id"].As<string>().DatabaseIdToGuid(),
            record["Name"].As<string>());

        var configuration = new BoardConfiguration(
            record["CharacterNodeColor"].As<string>(),
            record["FactNodeColor"].As<string>(),
            record["RelationEdgeColor"].As<string>(),
            record["FactEdgeColor"].As<string>(),
            record["PathHighlightColor"].As<string>(),
            record["NodeRadius"].As<int>(),
            record["EdgeWidth"].As<int>(),
            record["CurvedEdges"].As<bool>(),
            record["ShowGrid"].As<bool>(),
            record["ScalingObjects"].As<bool>());

        return new Board(createBoard, configuration, (ushort)record["Version"].As<int>());
    }

    public static Character ToCharacter(this IRecord record)
    {
        var createCharacter = new CreateCharacter(
            record["Id"].As<string>().DatabaseIdToGuid(),
            record["Name"].As<string>());

        return new Character(createCharacter, (ushort)record["Version"].As<int>());
    }

    public static CharacterWithKnowRelation ToCharacterWithKnowRelation(this IRecord record)
    {
        var character = new CharacterWithKnowRelation(
            record["Id"].As<string>().DatabaseIdToGuid(),
            record["Name"].As<string>(),
            record["KnowRelations"]
                .As<List<IReadOnlyDictionary<string, object>>>()
                .Select(relation => new KnowRelationDetail(
                    relation["Id"].As<string>().DatabaseIdToGuid(),
                    relation["Description"].As<string>(),
                    relation["IsStrong"].As<bool>()))
                .ToList()
                .AsReadOnly(),
            record["Facts"]
                .As<List<IReadOnlyDictionary<string, object>>>()
                .Select(fact => new FactDetail(
                    fact["Id"].As<string>().DatabaseIdToGuid(),
                    fact["Title"].As<string>(),
                    fact["Content"].As<string>()))
                .ToList()
                .AsReadOnly());

        return character;
    }

    public static Fact ToFact(this IRecord record)
    {
        var createFact = new CreateFact(
            record["Id"].As<string>().DatabaseIdToGuid(),
            record["Title"].As<string>(),
            record["Content"].As<string>());

        return new Fact(createFact, (ushort)record["Version"].As<int>());
    }

    public static KnowRelation ToKnowRelation(this IRecord record) =>
        new(
            record["Id"].As<string>().DatabaseIdToGuid(),
            record["Description"].As<string>(),
            record["IsStrong"].As<bool>(),
            record["FromCharacterId"].As<string>().DatabaseIdToGuid(),
            record["ToCharacterId"].As<string>().DatabaseIdToGuid(),
            (ushort)record["Version"].As<int>());
}
