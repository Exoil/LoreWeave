using LoreWeave.Domain.Models;

namespace LoreWeave.Domain.Entities.Boards.Commands;

public sealed record DeleteBoard : BaseValueObject
{
    public DeleteBoard(Guid id) => Id = id;

    public Guid Id { get; init; }
}
