using LoreWeave.Domain.Entities.Characters.Commands;

namespace LoreWeave.Domain.Entities.Characters;

public sealed class Character
{
    public Character(CreateCharacter createCharacter)
    {
        Id = createCharacter.Id;
        Name = createCharacter.Name;
        Version = 1;
    }

    public Character(CreateCharacter createCharacter, ushort version) : this(createCharacter) => Version = version;

    public Guid Id { get; private init; }

    public string Name { get; private set; }

    public ushort Version { get; private set; }
}
