namespace LoreWeave.Application.Commands.Facts;

public sealed record DisconnectFactFromCharacterCommand(
    Guid BoardId,
    Guid CharacterId,
    Guid FactId);