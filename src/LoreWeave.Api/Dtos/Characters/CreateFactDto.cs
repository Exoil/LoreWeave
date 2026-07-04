using LoreWeave.Application.Commands;

using System.ComponentModel.DataAnnotations;

namespace LoreWeave.Api.Dtos.Characters;

public record CreateFactDto(
    [StringLength(100, MinimumLength = 1, ErrorMessage = "Value for {0} must be between {1} and {2} characters.")]
    string Title,
    [StringLength(512, MinimumLength = 1, ErrorMessage = "Value for {0} must be between {1} and {2} characters.")]
    string Content)
{
    public CreateFactCommand ToCommand(Guid characterId) =>
        new(characterId, Title, Content);
}