using LoreWeave.Domain.Models;

using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace LoreWeave.Domain.Entities.Facts.Commands;

public sealed record UpdateFact : BaseValueObject
{
    [SetsRequiredMembers]
    public UpdateFact(
        Guid id,
        string title,
        string content,
        ushort version)
    {
        Id = id;
        Title = title;
        Content = content;
        Version = version;

        Validate();
    }

    protected override string ModelName => nameof(UpdateFact);

    public required Guid Id { get; init; }

    [StringLength(100, MinimumLength = 1, ErrorMessage = "Value for {0} must be between {1} and {2} characters.")]
    public required string Title { get; init; }

    [StringLength(512, MinimumLength = 1, ErrorMessage = "Value for {0} must be between {1} and {2} characters.")]
    public required string Content { get; init; }

    [Range(1, ushort.MaxValue, ErrorMessage = "Value for {0} must be between {1} and {2}.")]
    public required ushort Version { get; init; }
}
