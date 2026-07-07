using LoreWeave.Application.Models;

namespace LoreWeave.Application.Commands.Boards;

public record UpdateBoardCommand(
    Guid Id,
    string Name,
    BoardConfigurationPayload Configuration,
    ushort Version);
