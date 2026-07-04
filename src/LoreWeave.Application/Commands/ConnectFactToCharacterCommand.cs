namespace LoreWeave.Application.Commands;

public sealed record ConnectFactToCharacterCommand(
    Guid CharacterId,
    Guid FactId);