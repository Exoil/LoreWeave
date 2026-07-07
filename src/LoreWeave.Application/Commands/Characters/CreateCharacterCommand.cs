namespace LoreWeave.Application.Commands.Characters;

public record CreateCharacterCommand(Guid BoardId, Guid Id, string Name);
