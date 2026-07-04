namespace LoreWeave.Application.Commands;

public sealed record UpdateFactCommand(
    Guid Id,
    string Title,
    string Content,
    ushort Version);