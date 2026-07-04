namespace LoreWeave.Application.Commands;

public sealed record DisconnectFactFromCharacterCommand(
    Guid CharacterId,
    Guid FactId);