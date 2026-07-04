namespace LoreWeave.Application.Queries.Characters;

public record GetCharacterPageQuery(
    uint Number,
    uint Size,
    string SortType,
    string SortOrder,
    string? CharacterName);
