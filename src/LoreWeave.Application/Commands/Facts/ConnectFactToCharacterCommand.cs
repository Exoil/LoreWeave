namespace LoreWeave.Application.Commands.Facts;

public sealed record ConnectFactToCharacterCommand(
    Guid BoardId,
    Guid CharacterId,
    Guid FactId);