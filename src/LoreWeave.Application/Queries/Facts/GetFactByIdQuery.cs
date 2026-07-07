namespace LoreWeave.Application.Queries.Facts;

public sealed record GetFactByIdQuery(Guid BoardId, Guid Id);