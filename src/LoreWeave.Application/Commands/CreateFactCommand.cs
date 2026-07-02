using LoreWeave.Domain.Entities.Facts.Commands;

namespace LoreWeave.Application.Commands;

public sealed record CreateFactCommand(
    Guid CharacterId,
    CreateFact CreateFact);
