using LoreWeave.Domain.Entities.Characters.Commands;
using LoreWeave.Domain.Models;

using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace LoreWeave.Domain.Entities.Facts.Commands;

public sealed record CreateFact : BaseValueObject
{
    [SetsRequiredMembers]
    public CreateFact(
        Guid id,
        string title,
        string content)
    {
        Id = id;
        Title = title;
        Content = content;

        Validate();
    }

    protected override string ModelName => nameof(CreateCharacter);

    public required Guid Id { get; init; }

    [StringLength(100, MinimumLength = 1, ErrorMessage = "Value for {0} must be between {1} and {2} characters.")]
    public required string Title { get; init; }
    
    [StringLength(512, MinimumLength = 1, ErrorMessage = "Value for {0} must be between {1} and {2} characters.")]
    public required string Content { get; init; }
}
