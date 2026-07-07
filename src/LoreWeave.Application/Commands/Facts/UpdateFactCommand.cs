namespace LoreWeave.Application.Commands.Facts;

public sealed record UpdateFactCommand(
    Guid BoardId,
    Guid Id,
    string Title,
    string Content,
    ushort Version);