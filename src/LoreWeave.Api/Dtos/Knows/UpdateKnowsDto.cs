using LoreWeave.Application.Commands;

using System.ComponentModel.DataAnnotations;

namespace LoreWeave.Api.Dtos.Knows;

public record UpdateKnowsDto(
    [StringLength(256, MinimumLength = 0, ErrorMessage = "Value for {0} must be between {1} and {2} characters.")]
    string Description,
    bool IsStrongRelation)
{
    public UpdateKnowRelationCommand ToCommand(Guid from, Guid to, string version) =>
        new(
            from,
            to,
            Description,
            IsStrongRelation,
            int.Parse(
                version
                    .Replace(
                        "\"",
                        string.Empty)));
}