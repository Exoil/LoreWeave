namespace LoreWeave.Application.Commands.Facts;

public sealed record UpdateFactCommand(
    Guid Id,
    string Title,
    string Content,
    ushort Version);