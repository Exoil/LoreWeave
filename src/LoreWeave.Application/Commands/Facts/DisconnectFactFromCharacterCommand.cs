namespace LoreWeave.Application.Commands.Facts;

public sealed record DisconnectFactFromCharacterCommand(
    Guid CharacterId,
    Guid FactId);