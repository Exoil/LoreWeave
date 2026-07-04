namespace LoreWeave.Application.Commands;

public record UpdateCharacterCommand(Guid Id, string Name, ushort Version);
