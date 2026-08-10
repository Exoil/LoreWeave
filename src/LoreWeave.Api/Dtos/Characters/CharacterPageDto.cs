using LoreWeave.Application.Queries.Characters;

namespace LoreWeave.Api.Dtos.Characters;

/// <summary>
///     Query-string parameters of the character page endpoint, bound as a single
///     [AsParameters] argument so the endpoint lambda stays within the parameter limit.
///     Simple types bind from the query string by default, so no per-property attributes
///     are needed and the wire contract is unchanged.
/// </summary>
public record CharacterPageDto(
    uint PageNumber,
    uint PageSize,
    string SortType,
    string SortOrder,
    string? NameFilter)
{
    public GetCharacterPageQuery ToQuery(Guid boardId) => new(
        boardId,
        PageNumber,
        PageSize,
        SortType,
        SortOrder,
        NameFilter);
}
