using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

using LoreWeave.Domain.Models;

namespace LoreWeave.Domain.Entities.Knows.Commands;

public sealed record CreateKnowRelation : BaseValueObject
{
    [SetsRequiredMembers]
    public CreateKnowRelation(
        Guid id,
        Guid fromCharacterId,
        Guid toCharacterId,
        string description,
        bool isStrongRelation)
    {
        if (fromCharacterId == toCharacterId)
        {
            throw new ArgumentException("From and To cannot be the same character id",
                nameof(toCharacterId));
        }

        Id = id;
        FromCharacterId = fromCharacterId;
        ToCharacterId = toCharacterId;
        Description = description;
        IsStrongRelation = isStrongRelation; 

        Validate();
    }

    protected override string ModelName => nameof(CreateKnowRelation);

    public required Guid Id { get; init; }

    public required Guid FromCharacterId { get; init; }

    public required Guid ToCharacterId { get; init; }

    [StringLength(256, MinimumLength = 0, ErrorMessage = "Value for {0} must be between {1} and {2} characters.")]
    public required string Description { get; init; }
    
    public required bool IsStrongRelation { get; init; }
}
