using DotTest.Abstractions;
using DotTest.Enums;
using DotTest.ValueObjects;

namespace DotTest.Entities;

public class Maze(MazeState state) : IMaze
{
    private const int CorridorLength = 10;
    private const int RoomsCount = 4;
    private static readonly int[] SupportedRoomsLength = [2, 4];

    public MazeState State { get; } = state;
    public int EnergyLost { get; private set; } = 0;
    public Dictionary<MazeState, int> AvailableStatesWithEnergyCost { get; } = new();

    public static IMaze FromStringLines(List<string> lines)
    {
        var corridor = Enumerable.Repeat(MazePositionType.Empty, CorridorLength).ToArray();
        var rooms = new MazePositionType[][RoomsCount];
        var roomLength = lines.Count - 3;
        for (var i = 0; i < 4; i++)
            rooms[i] = new MazePositionType[roomLength];
        for (var i = 0; i < roomLength; i++)
        {
            var roomsLevel = lines[i + 2];
            for (var j = 0; j < rooms.Length; j++)
                rooms[j][i] = GetMazePositionTypeFromChar(roomsLevel[j * 2 + 3]);
        }

        var state = new MazeState { Corridor = corridor, Rooms = rooms };
        return new Maze(state);
    }

    public void Move(MazeState state)
    {
        throw new NotImplementedException();
    }
    
    private static MazePositionType GetMazePositionTypeFromChar(char @char) => @char switch
    {
        'A' => MazePositionType.A,
        'B' => MazePositionType.B,
        'C' => MazePositionType.C,
        'D' => MazePositionType.D,
        '#' => MazePositionType.Wall,
        '.' => MazePositionType.Empty,
        _ => throw new ArgumentOutOfRangeException(nameof(@char), @char, null)
    };

    private static void UpdateAvailableStates()
    {
        throw new NotImplementedException();
    }
}