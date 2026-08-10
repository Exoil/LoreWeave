using System.Diagnostics.CodeAnalysis;

using LoreWeave.Domain.Models;

namespace LoreWeave.Domain.Entities.Knows.Commands;

public sealed record DeleteKnowRelation : BaseValueObject
{
    [SetsRequiredMembers]
    public DeleteKnowRelation(
        Guid fromCharacterId,
        Guid toCharacterId)
    {
        if (fromCharacterId == toCharacterId)
        {
            throw new ArgumentException("From and To cannot be the same character id",
                nameof(toCharacterId));
        }

        FromCharacterId = fromCharacterId;
        ToCharacterId = toCharacterId;

        Validate();
    }

    protected override string ModelName => nameof(DeleteKnowRelation);

    public required Guid FromCharacterId { get; init; }

    public required Guid ToCharacterId { get; init; }
}
