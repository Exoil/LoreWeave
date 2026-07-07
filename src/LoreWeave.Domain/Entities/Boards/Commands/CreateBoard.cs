using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

using LoreWeave.Domain.Models;

namespace LoreWeave.Domain.Entities.Boards.Commands;

public sealed record CreateBoard : BaseValueObject
{
    [SetsRequiredMembers]
    public CreateBoard(
        Guid id,
        string name)
    {
        Id = id;
        Name = name;

        Validate();
    }

    protected override string ModelName => nameof(CreateBoard);

    public required Guid Id { get; init; }

    [StringLength(50, MinimumLength = 1, ErrorMessage = "Value for {0} must be between {1} and {2} characters.")]
    public required string Name { get; init; }
}
