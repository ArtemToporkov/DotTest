using DotTest.Enums;
using DotTest.ValueObjects;

namespace DotTest.Entities;

public class Maze
{
    public const int CorridorLength = 11;
    
    private static readonly int[] StopCorridorCells = [0, 1, 3, 5, 7, 9, 10];
    private static readonly int[] ConnectionCells = [2, 4, 6, 8];

    private static readonly Dictionary<MazeCellType, int> EnergyRequiredToMove = new()
    {
        [MazeCellType.A] = 1, [MazeCellType.B] = 10,
        [MazeCellType.C] = 100, [MazeCellType.D] = 1000
    };

    public MazeState State { get; }
    public Dictionary<MazeState, int> AvailableStatesWithEnergyRequired { get; }

    internal Maze(MazeState state)
    {
        State = state;
        AvailableStatesWithEnergyRequired = new();
        UpdateAvailableStates();
    }
    
    public static Maze FromStringLines(List<string> lines)
    {
        var corridor = Enumerable.Repeat(MazeCellType.Empty, CorridorLength).ToArray();
        var roomLength = lines.Count - 3;

        var rooms = new MazeCellType[4][];
        for (var i = 0; i < 4; i++)
            rooms[i] = new MazeCellType[roomLength];

        for (var i = 0; i < roomLength; i++)
        {
            var row = lines[2 + i];
            for (var j = 0; j < 4; j++)
                rooms[j][i] = GetMazePositionTypeFromChar(row[j * 2 + 3]);
        }

        var state = new MazeState { Corridor = corridor, Rooms = rooms };
        return new Maze(state);
    }

    private void UpdateAvailableStates()
    {
        AvailableStatesWithEnergyRequired.Clear();
        GenerateMovesFromCorridorToRooms();
        GenerateMovesFromRoomsToCorridor();
    }

    private void GenerateMovesFromCorridorToRooms()
    {
        for (var corridorCellIdx = 0; corridorCellIdx < State.Corridor.Length; corridorCellIdx++)
        {
            var cellType = State.Corridor[corridorCellIdx];
            if (cellType == MazeCellType.Empty) continue;

            var targetRoom = (int)cellType - (int)MazeCellType.A;
            var roomIsReady = State.Rooms[targetRoom].All(
                occupant => occupant == MazeCellType.Empty || occupant == cellType);
            if (!roomIsReady) 
                continue;

            var connectionCellIdx = ConnectionCells[targetRoom];
            if (!IsCorridorPathClear(State.Corridor, corridorCellIdx, connectionCellIdx)) 
                continue;

            var targetDepth = Array.FindLastIndex(State.Rooms[targetRoom], p => p == MazeCellType.Empty);
            if (targetDepth == -1) 
                continue;

            var steps = Math.Abs(corridorCellIdx - connectionCellIdx) + targetDepth + 1;
            var moveCost = steps * EnergyRequiredToMove[cellType];
            
            var newState = CreateNewState(corridorCellIdx, -1, -1, targetRoom, targetDepth, cellType);
            AvailableStatesWithEnergyRequired[newState] = moveCost;
        }
    }
    
    private void GenerateMovesFromRoomsToCorridor()
    {
        var depth = State.Rooms[0].Length;
        for (var roomIdx = 0; roomIdx < 4; roomIdx++)
        {
            var topDepth = Array.FindIndex(State.Rooms[roomIdx], p => p != MazeCellType.Empty);
            if (topDepth == -1) 
                continue;

            var cellType = State.Rooms[roomIdx][topDepth];

            if (ShouldObjectStayInCorrectRoom(cellType, roomIdx, topDepth, depth))
                continue;

            var startEntrance = ConnectionCells[roomIdx];
            foreach (var stopCorridorCell in StopCorridorCells)
            {
                if (IsCorridorPathClear(State.Corridor, startEntrance, stopCorridorCell))
                {
                    var steps = topDepth + 1 + Math.Abs(stopCorridorCell - startEntrance);
                    var moveCost = steps * EnergyRequiredToMove[cellType];

                    var newState = CreateNewState(-1, stopCorridorCell, roomIdx, -1, topDepth, cellType);
                    AvailableStatesWithEnergyRequired[newState] = moveCost;
                }
            }
        }
    }

    private bool ShouldObjectStayInCorrectRoom(MazeCellType @object, int roomIdx, int depth, int maxDepth)
    {
        var targetRoomIndex = (int)@object - (int)MazeCellType.A;
        if (targetRoomIndex != roomIdx) 
            return false;

        for (var d = depth + 1; d < maxDepth; d++)
            if (State.Rooms[roomIdx][d] != @object)
                return false;
        
        return true;
    }

    private MazeState CreateNewState(
        int corridorFrom, int corridorTo, 
        int roomFrom, int roomTo, 
        int depth, MazeCellType cellType)
    {
        var newCorridor = (MazeCellType[])State.Corridor.Clone();
        var newRooms = State.Rooms
            .Select(r => (MazeCellType[])r.Clone())
            .ToArray();
        if (corridorFrom != -1)
        {
            newCorridor[corridorFrom] = MazeCellType.Empty; 
            newRooms[roomTo][depth] = cellType;
        }
        else
        {
            newRooms[roomFrom][depth] = MazeCellType.Empty; 
            newCorridor[corridorTo] = cellType;
        }
        return new MazeState { Corridor = newCorridor, Rooms = newRooms };
    }
    
    private static bool IsCorridorPathClear(MazeCellType[] corridor, int from, int to)
    {
        var start = Math.Min(from, to);
        var end = Math.Max(from, to);
        for (var i = start; i <= end; i++) 
            if (i != from && corridor[i] != MazeCellType.Empty) 
                return false;
        return true;
    }
    
    private static MazeCellType GetMazePositionTypeFromChar(char @char) => @char switch
    {
        'A' => MazeCellType.A, 'B' => MazeCellType.B, 'C' => MazeCellType.C, 'D' => MazeCellType.D,
        '#' => MazeCellType.Wall, '.' => MazeCellType.Empty,
        _ => throw new FormatException($"Unexpected character: {@char}. Should be 'A', 'B', 'C', 'D', '.' or '#'.")
    };
}