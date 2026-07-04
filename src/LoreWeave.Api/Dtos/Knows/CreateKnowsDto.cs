using LoreWeave.Application.Commands;

using System.ComponentModel.DataAnnotations;

namespace LoreWeave.Api.Dtos.Knows;

public record CreateKnowsDto(
    Guid FromCharacterId,
    Guid ToCharacterId,
    [StringLength(256, MinimumLength = 0, ErrorMessage = "Value for {0} must be between {1} and {2} characters.")]
    string Description,
    bool IsStrongRelation)
{
    public CreateKnowRelationCommand ToCommand() =>
        new(
            FromCharacterId,
            ToCharacterId,
            Description,
            IsStrongRelation);
}
