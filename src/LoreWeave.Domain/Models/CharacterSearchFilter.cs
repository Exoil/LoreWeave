using System.ComponentModel.DataAnnotations;

namespace LoreWeave.Domain.Models;

public sealed record CharacterSearchFilter : BaseValueObject
{
    public CharacterSearchFilter(string? name)
    {
        Name = name;
        Validate();
    }

    protected override string ModelName { get; } = nameof(CharacterSearchFilter);

    [StringLength(100, ErrorMessage = "Value for {0} must be at most {1} characters.")]
    public string? Name { get; init; }
}
