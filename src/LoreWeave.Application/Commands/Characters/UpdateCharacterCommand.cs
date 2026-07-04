namespace LoreWeave.Application.Commands.Characters;

public record UpdateCharacterCommand(Guid Id, string Name, ushort Version);
