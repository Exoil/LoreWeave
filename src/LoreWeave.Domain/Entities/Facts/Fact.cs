using LoreWeave.Domain.Entities.Facts.Commands;

using System.Diagnostics.CodeAnalysis;

namespace LoreWeave.Domain.Entities.Facts;

public sealed class Fact
{
    public Guid Id { get; private init; }
    
    public string Title { get; private set; }
    
    public string Content { get; private set; }

    public Fact(
        CreateFact createFact)
    {
        Id = createFact.Id;
        Title = createFact.Title;
        Content = createFact.Content;
    }
}
