using DotTest.Abstractions;
using DotTest.Enums;
using DotTest.ValueObjects;

namespace DotTest.Entities;

public class Maze : IMaze
{
    private static readonly int[] StopCorridorCellsIndices = [0, 1, 3, 5, 7, 9, 10];
    private static readonly int[] ConnectionCellsIndices = [2, 4, 6, 8];

    public int CorridorLength { get; }
    public MazeState State { get; }
    public Dictionary<MazeState, int> AvailableStatesWithEnergyRequired { get; }

    private Maze(MazeState state, int corridorLength)
    {
        State = state;
        CorridorLength = corridorLength;
        AvailableStatesWithEnergyRequired = new();
        UpdateAvailableStates();
    }

    public static IMaze FromMazeState(MazeState state) => new Maze(state, state.Corridor.Length);

    public static IMaze FromStringLines(List<string> lines)
    {
        var corridorLength = lines[0].Length - 2;
        var corridor = Enumerable.Repeat(new MazeCell(MazeCellType.Empty), corridorLength).ToArray();
        var roomLength = lines.Count - 3;

        var rooms = new MazeCell[4][];
        for (var i = 0; i < 4; i++)
            rooms[i] = new MazeCell[roomLength];

        for (var i = 0; i < roomLength; i++)
        {
            var row = lines[2 + i];
            for (var j = 0; j < 4; j++)
                rooms[j][i] = GetMazeCellFromChar(row[j * 2 + 3]);
        }

        var state = new MazeState { Corridor = corridor, Rooms = rooms };
        return new Maze(state, corridorLength);
    }

    private void UpdateAvailableStates()
    {
        GenerateMovesFromCorridorToRooms();
        GenerateMovesFromRoomsToCorridor();
    }

    private void GenerateMovesFromCorridorToRooms()
    {
        for (var corridorCellIdx = 0; corridorCellIdx < State.Corridor.Length; corridorCellIdx++)
        {
            var cell = State.Corridor[corridorCellIdx];
            if (cell.Type != MazeCellType.Object) continue;

            var mazeObject = cell.Object!.Value;
            var roomIsReady = State.Rooms[mazeObject.TargetRoomIdx].All(
                occupantCell => occupantCell.Type == MazeCellType.Empty || occupantCell.Object!.Value.Type == mazeObject.Type);
            if (!roomIsReady) continue;

            var connectionCellIdx = ConnectionCellsIndices[mazeObject.TargetRoomIdx];
            if (!IsCorridorPathClear(State.Corridor, corridorCellIdx, connectionCellIdx)) continue;

            var targetDepth = Array.FindLastIndex(
                State.Rooms[mazeObject.TargetRoomIdx], c => c.Type == MazeCellType.Empty);
            if (targetDepth == -1) continue;

            var steps = Math.Abs(corridorCellIdx - connectionCellIdx) + targetDepth + 1;
            var moveCost = steps * mazeObject.EnergyRequiredToMove;
            
            var newState = CreateNewState(corridorCellIdx, -1, -1, mazeObject.TargetRoomIdx, targetDepth, cell);
            AvailableStatesWithEnergyRequired[newState] = moveCost;
        }
    }
    
    private void GenerateMovesFromRoomsToCorridor()
    {
        var depth = State.Rooms[0].Length;
        for (var roomIdx = 0; roomIdx < 4; roomIdx++)
        {
            var topDepth = Array.FindIndex(State.Rooms[roomIdx], c => c.Type != MazeCellType.Empty);
            if (topDepth == -1) continue;

            var cell = State.Rooms[roomIdx][topDepth];
            var mazeObject = cell.Object!.Value;

            if (ShouldObjectStayInCorrectRoom(mazeObject, roomIdx, topDepth, depth)) continue;

            var startEntrance = ConnectionCellsIndices[roomIdx];
            foreach (var stopCorridorCell in StopCorridorCellsIndices)
            {
                if (IsCorridorPathClear(State.Corridor, startEntrance, stopCorridorCell))
                {
                    var steps = topDepth + 1 + Math.Abs(stopCorridorCell - startEntrance);
                    var moveCost = steps * mazeObject.EnergyRequiredToMove;

                    var newState = CreateNewState(-1, stopCorridorCell, roomIdx, -1, topDepth, cell);
                    AvailableStatesWithEnergyRequired[newState] = moveCost;
                }
            }
        }
    }

    private bool ShouldObjectStayInCorrectRoom(MazeObject mazeObject, int roomIdx, int depth, int maxDepth)
    {
        if (mazeObject.TargetRoomIdx != roomIdx) return false;
        for (var d = depth + 1; d < maxDepth; d++)
            if (State.Rooms[roomIdx][d].Object!.Value.Type != mazeObject.Type)
                return false;
        return true;
    }

    private MazeState CreateNewState(
        int fromCorridor, int toCorridor, 
        int fromRoom, int toRoom, 
        int depth, MazeCell cell)
    {
        var newCorridor = (MazeCell[])State.Corridor.Clone();
        var newRooms = State.Rooms.Select(r => (MazeCell[])r.Clone()).ToArray();

        if (fromCorridor != -1)
        {
            newCorridor[fromCorridor] = new MazeCell(MazeCellType.Empty);
            newRooms[toRoom][depth] = cell;
        }
        else
        {
            newRooms[fromRoom][depth] = new MazeCell(MazeCellType.Empty);
            newCorridor[toCorridor] = cell;
        }
        return new MazeState { Corridor = newCorridor, Rooms = newRooms };
    }
    
    private static bool IsCorridorPathClear(MazeCell[] corridor, int from, int to)
    {
        var start = Math.Min(from, to);
        var end = Math.Max(from, to);
        for (var i = start; i <= end; i++) 
            if (i != from && corridor[i].Type != MazeCellType.Empty) 
                return false;
        return true;
    }
    
    private static MazeCell GetMazeCellFromChar(char @char) => @char switch
    {
        'A' => new MazeCell(MazeCellType.Object, new MazeObject(MazeObjectType.A)),
        'B' => new MazeCell(MazeCellType.Object, new MazeObject(MazeObjectType.B)),
        'C' => new MazeCell(MazeCellType.Object, new MazeObject(MazeObjectType.C)),
        'D' => new MazeCell(MazeCellType.Object, new MazeObject(MazeObjectType.D)),
        '.' => new MazeCell(MazeCellType.Empty),
        '#' => new MazeCell(MazeCellType.Wall),
        _ => throw new FormatException($"Unexpected character: {@char}")
    };
}