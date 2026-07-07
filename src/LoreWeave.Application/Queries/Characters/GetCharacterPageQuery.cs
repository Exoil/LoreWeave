namespace LoreWeave.Application.Queries.Characters;

public record GetCharacterPageQuery(
    Guid BoardId,
    uint Number,
    uint Size,
    string SortType,
    string SortOrder,
    string? CharacterName);
