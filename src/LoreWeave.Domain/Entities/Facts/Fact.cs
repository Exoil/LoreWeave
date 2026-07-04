using LoreWeave.Domain.Entities.Facts.Commands;

namespace LoreWeave.Domain.Entities.Facts;

public sealed class Fact
{
    public Fact(
        CreateFact createFact)
    {
        Id = createFact.Id;
        Title = createFact.Title;
        Content = createFact.Content;
        Version = 1;
    }

    public Fact(CreateFact createFact, ushort version) : this(createFact) => Version = version;

    public Guid Id { get; private init; }

    public string Title { get; private set; }

    public string Content { get; private set; }

    public ushort Version { get; private set; }
}