namespace LoreWeave.Application.Commands.Facts;

public sealed record CreateFactCommand(
    Guid BoardId,
    Guid CharacterId,
    string Title,
    string Content);
