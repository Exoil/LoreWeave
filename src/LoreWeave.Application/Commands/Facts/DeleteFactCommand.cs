namespace LoreWeave.Application.Commands.Facts;

public sealed record DeleteFactCommand(Guid BoardId, Guid Id);