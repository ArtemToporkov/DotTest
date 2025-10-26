using DotTest.Entities;
using DotTest.Enums;

namespace DotTest.ValueObjects;

public readonly record struct MazeCell
{
    public MazeCellType Type { get; }
    public MazeObject? Object { get; }

    public MazeCell(MazeCellType type, MazeObject? mazeObject = null)
    {
        if (type is not MazeCellType.Object && mazeObject is not null)
            throw new InvalidOperationException(
                $"{nameof(Type)} should be {nameof(MazeCellType.Object)} to have an object");
        Type = type;
        Object = mazeObject;
    }
}