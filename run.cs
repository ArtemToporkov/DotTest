namespace DotTest;

public interface IMaze
{
    public MazeState State { get; }
    public Dictionary<MazeState, int> AvailableStatesWithEnergyRequired { get; }
    
    public static abstract IMaze FromStringLines(List<string> lines);
    
    public static abstract IMaze FromMazeState(MazeState state);

    public int GetConnectionCellIndexForRoomIndex(int roomIdx);
}

public enum MazeCellType
{
    Empty,
    Wall,
    Object
}

public enum MazeObjectType
{
    A,
    B,
    C,
    D
}

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

public readonly record struct MazeObject(MazeObjectType Type)
{
    public int EnergyRequiredToMove => Type.GetEnergyCost();
    public int TargetRoomIdx => Type.GetTargetRoomIndex();
}

public readonly struct MazeState : IEquatable<MazeState>
{
    public required MazeCell[] Corridor { get; init; }
    public required MazeCell[][] Rooms { get; init; }

    public bool Equals(MazeState other)
        => Corridor.AsSpan().SequenceEqual(other.Corridor)
           && Rooms.Length == other.Rooms.Length
           && Rooms.Zip(other.Rooms).All(pair => pair.First.AsSpan().SequenceEqual(pair.Second));

    public override bool Equals(object? obj) => obj is MazeState other && Equals(other);

    public override int GetHashCode()
    {
        var hash = new HashCode();
        foreach (var c in Corridor) hash.Add(c);
        foreach (var room in Rooms)
        foreach (var pos in room) hash.Add(pos);
        return hash.ToHashCode();
    }
    
    public static bool operator ==(MazeState left, MazeState right) => left.Equals(right);
    public static bool operator !=(MazeState left, MazeState right) => !left.Equals(right);
}

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

public class Maze : IMaze
{
    private static readonly int[] StopCorridorCellsIndices = [0, 1, 3, 5, 7, 9, 10];
    private static readonly int[] ConnectionCellsIndices = [2, 4, 6, 8];

    public MazeState State { get; }
    public Dictionary<MazeState, int> AvailableStatesWithEnergyRequired { get; }

    private Maze(MazeState state)
    {
        State = state;
        AvailableStatesWithEnergyRequired = new();
        UpdateAvailableStates();
    }

    public static IMaze FromMazeState(MazeState state) => new Maze(state);

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
        return new Maze(state);
    }

    public int GetConnectionCellIndexForRoomIndex(int roomIdx)
    {
        return roomIdx switch
        {
            0 => 2, 
            1 => 4, 
            2 => 6, 
            3 => 8,
            _ => throw new InvalidOperationException("Room index should be between 0 and 3")
        };
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

public static class MazeSolver<TMaze> where TMaze : IMaze
{
    public static long Solve(TMaze maze)
    {
        var startState = maze.State;
        var goalState = CalculateGoalState(startState.Rooms[0].Length, maze.State.Corridor.Length);
        var queue = new PriorityQueue<(MazeState State, long Cost), long>();
        var bestCosts = new Dictionary<MazeState, long>();
        var startHeuristic = CalculateHeuristic(maze, startState);
        queue.Enqueue((startState, 0), startHeuristic);
        bestCosts[startState] = 0;
        while (queue.TryDequeue(out var current, out _))
        {
            var (currentState, currentCost) = current;
            if (bestCosts.TryGetValue(currentState, out var knownCost) && currentCost > knownCost) continue;
            if (currentState.Equals(goalState)) return currentCost;
            var currentMaze = TMaze.FromMazeState(currentState);
            foreach (var (nextState, moveCost) in currentMaze.AvailableStatesWithEnergyRequired)
            {
                var newCost = currentCost + moveCost;
                if (!bestCosts.TryGetValue(nextState, out var value) || newCost < value)
                {
                    value = newCost;
                    bestCosts[nextState] = value;
                    var priority = newCost + CalculateHeuristic(maze, nextState);
                    queue.Enqueue((nextState, newCost), priority);
                }
            }
        }
        return -1;
    }

    private static long CalculateHeuristic(TMaze maze, MazeState state)
        => CalculateHeuristicForCorridor(maze, state) + CalculateHeuristicForRooms(maze, state);

    private static long CalculateHeuristicForCorridor(TMaze maze, MazeState state)
    {
        var result = 0L;
        for (var i = 0; i < state.Corridor.Length; i++)
        {
            var cell = state.Corridor[i];
            if (cell.Type != MazeCellType.Object) 
                continue;
            
            var mazeObject = cell.Object!.Value;
            var connectionCellIdx = maze.GetConnectionCellIndexForRoomIndex(mazeObject.TargetRoomIdx);
            
            var steps = Math.Abs(i - connectionCellIdx) + 1;
            result += (long)steps * mazeObject.EnergyRequiredToMove;
        }
        return result;
    }

    private static long CalculateHeuristicForRooms(TMaze maze, MazeState state)
    {
        var result = 0L;
        var depth = state.Rooms[0].Length;
        for (var roomIdx = 0; roomIdx < 4; roomIdx++)
        for (var d = 0; d < depth; d++)
        {
            var cell = state.Rooms[roomIdx][d];
            if (cell.Type != MazeCellType.Object) continue;

            var mazeObject = cell.Object!.Value;
            if (mazeObject.TargetRoomIdx == roomIdx)
            {
                var mustMove = false;
                for (var dd = d + 1; dd < depth; dd++)
                    if (state.Rooms[roomIdx][dd].Object!.Value.Type != mazeObject.Type)
                    {
                        mustMove = true; 
                        break;
                    }
                if (!mustMove) continue;
            }
            var startConnectionCell = maze.GetConnectionCellIndexForRoomIndex(roomIdx);
            var targetConnectionCell = maze.GetConnectionCellIndexForRoomIndex(mazeObject.TargetRoomIdx);
            var steps = d + 1 + Math.Abs(startConnectionCell - targetConnectionCell) + 1;
            result += (long)steps * mazeObject.EnergyRequiredToMove;
        }
        return result;
    }

    private static MazeState CalculateGoalState(int depth, int corridorLength)
    {
        var corridor = Enumerable.Repeat(new MazeCell(MazeCellType.Empty), corridorLength).ToArray();
        var rooms = new MazeCell[4][];
        for (var i = 0; i < 4; i++)
        {
            rooms[i] = new MazeCell[depth];
            var goalObject = new MazeObject((MazeObjectType)i);
            var goalCell = new MazeCell(MazeCellType.Object, goalObject);
            Array.Fill(rooms[i], goalCell);
        }
        return new MazeState { Corridor = corridor, Rooms = rooms };
    }
}

public class Program
{
    private static long Solve<TMaze>(List<string> lines) where TMaze : IMaze
    {
        var maze = (TMaze)TMaze.FromStringLines(lines);
        return MazeSolver<TMaze>.Solve(maze);
    }
    
    public static void Main(string[] args)
    {
        var lines = new List<string>();
        while (Console.ReadLine() is { } line && !string.IsNullOrEmpty(line))
            lines.Add(line);

        var result = Solve<Maze>(lines);
        Console.WriteLine(result);
    }
}