using LoreWeave.Application.Commands.Knows;

using System.ComponentModel.DataAnnotations;

namespace LoreWeave.Api.Dtos.Knows;

public record CreateKnowsDto(
    Guid FromCharacterId,
    Guid ToCharacterId,
    [StringLength(256, MinimumLength = 0, ErrorMessage = "Value for {0} must be between {1} and {2} characters.")]
    string Description,
    bool IsStrongRelation)
{
    public CreateKnowRelationCommand ToCommand(Guid boardId) =>
        new(
            boardId,
            FromCharacterId,
            ToCharacterId,
            Description,
            IsStrongRelation);
}
