namespace LoreWeave.Application.Commands;

public sealed record CreateFactCommand(
    Guid CharacterId,
    string Title,
    string Content);
