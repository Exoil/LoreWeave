namespace LoreWeave.Application.Commands.Characters;

public record UpdateCharacterCommand(Guid BoardId, Guid Id, string Name, ushort Version);
