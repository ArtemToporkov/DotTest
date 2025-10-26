using DotTest.Abstractions;
using DotTest.Enums;
using DotTest.ValueObjects;

namespace DotTest.Helpers;

public static class MazeSolver<TMaze> where TMaze : IMaze
{
    public static long Solve(TMaze maze)
    {
        var startState = maze.State;
        var goalState = CalculateGoalState(startState.Rooms[0].Length, maze.CorridorLength);
        var queue = new PriorityQueue<(MazeState State, long Cost), long>();
        var bestCosts = new Dictionary<MazeState, long>();
        var startHeuristic = CalculateHeuristic(startState);
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
                if (!bestCosts.ContainsKey(nextState) || newCost < bestCosts[nextState])
                {
                    bestCosts[nextState] = newCost;
                    var priority = newCost + CalculateHeuristic(nextState);
                    queue.Enqueue((nextState, newCost), priority);
                }
            }
        }
        return -1;
    }

    private static long CalculateHeuristic(MazeState state)
        => CalculateHeuristicForCorridor(state) + CalculateHeuristicForRooms(state);

    private static long CalculateHeuristicForCorridor(MazeState state)
    {
        var result = 0L;
        for (var i = 0; i < state.Corridor.Length; i++)
        {
            var cell = state.Corridor[i];
            if (cell.Type != MazeCellType.Object) continue;
            
            var mazeObject = cell.Object!.Value;
            var connectionCellIdx = GetConnectionCellIndexForRoomIndex(mazeObject.TargetRoomIdx);
            
            var steps = Math.Abs(i - connectionCellIdx) + 1;
            result += (long)steps * mazeObject.EnergyRequiredToMove;
        }
        return result;
    }

    private static long CalculateHeuristicForRooms(MazeState state)
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
                var startConnectionCell = GetConnectionCellIndexForRoomIndex(roomIdx);
                var targetConnectionCell = GetConnectionCellIndexForRoomIndex(mazeObject.TargetRoomIdx);
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
    
    private static int GetConnectionCellIndexForRoomIndex(int roomIndex) => roomIndex * 2 + 2;
}