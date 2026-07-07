using LoreWeave.Application.Commands.Facts;

using System.ComponentModel.DataAnnotations;

namespace LoreWeave.Api.Dtos.Characters;

public record CreateFactDto(
    [StringLength(100, MinimumLength = 1, ErrorMessage = "Value for {0} must be between {1} and {2} characters.")]
    string Title,
    [StringLength(3000, MinimumLength = 1, ErrorMessage = "Value for {0} must be between {1} and {2} characters.")]
    string Content)
{
    public CreateFactCommand ToCommand(Guid boardId, Guid characterId) =>
        new(boardId, characterId, Title, Content);
}