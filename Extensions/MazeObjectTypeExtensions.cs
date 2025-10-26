using DotTest.Enums;

namespace DotTest.Helpers;

public static class MazeObjectTypeExtensions
{
    public static int GetEnergyCost(this MazeObjectType type) => type switch
    {
        MazeObjectType.A => 1,
        MazeObjectType.B => 10,
        MazeObjectType.C => 100,
        MazeObjectType.D => 1000,
        _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
    };
    
    public static int GetTargetRoomIndex(this MazeObjectType type) => (int)type;

    public static MazeCellType ToMazeCellType(this MazeObjectType type) => MazeCellType.Object;
}