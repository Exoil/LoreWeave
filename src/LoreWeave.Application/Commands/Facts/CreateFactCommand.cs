namespace LoreWeave.Application.Commands.Facts;

public sealed record CreateFactCommand(
    Guid CharacterId,
    string Title,
    string Content);
