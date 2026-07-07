using LoreWeave.Domain.Entities.Boards.Commands;

namespace LoreWeave.Domain.Entities.Boards;

public sealed class Board
{
    public Board(CreateBoard createBoard)
    {
        Id = createBoard.Id;
        Name = createBoard.Name;
        Configuration = BoardConfiguration.Default;
        Version = 1;
    }

    public Board(CreateBoard createBoard, BoardConfiguration configuration, ushort version) : this(createBoard)
    {
        Configuration = configuration;
        Version = version;
    }

    public Guid Id { get; private init; }

    public string Name { get; private set; }

    public BoardConfiguration Configuration { get; private set; }

    public ushort Version { get; private set; }
}
