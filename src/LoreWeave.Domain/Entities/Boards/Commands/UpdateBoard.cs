using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

using LoreWeave.Domain.Models;

namespace LoreWeave.Domain.Entities.Boards.Commands;

public sealed record UpdateBoard : BaseValueObject
{
    [SetsRequiredMembers]
    public UpdateBoard(
        string name,
        BoardConfiguration configuration)
    {
        Name = name;
        Configuration = configuration;

        Validate();
    }

    protected override string ModelName => nameof(UpdateBoard);

    [StringLength(50, MinimumLength = 1, ErrorMessage = "Value for {0} must be between {1} and {2} characters.")]
    public required string Name { get; init; }

    // BoardConfiguration validates itself on construction.
    public required BoardConfiguration Configuration { get; init; }
}
