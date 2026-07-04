namespace LoreWeave.Domain.Entities.Knows;

public sealed class KnowRelation
{
    public KnowRelation(
        Guid id,
        string description,
        bool isStrongRelation,
        Guid fromCharacterId,
        Guid toCharacterId,
        ushort version)
    {
        Id = id;
        Description = description;
        IsStrongRelation = isStrongRelation;
        FromCharacterId = fromCharacterId;
        ToCharacterId = toCharacterId;
        Version = version;
    }

    public Guid Id { get; private init; }

    public string Description { get; private set; }

    public Guid FromCharacterId { get; private set; }

    public Guid ToCharacterId { get; private set; }
    
    public bool IsStrongRelation { get; private set; }

    public ushort Version { get; private set; }
}
