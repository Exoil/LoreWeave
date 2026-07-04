using LoreWeave.Domain.Models;

namespace LoreWeave.Domain.Entities.Facts.Commands;

public sealed record DeleteFact : BaseValueObject
{
    public DeleteFact(Guid id) => Id = id;

    public Guid Id { get; init; }
}