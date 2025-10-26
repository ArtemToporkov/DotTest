using DotTest.Entities;
using DotTest.Enums;
using DotTest.ValueObjects;

namespace DotTest.Helpers;

public class MazeSolver
{
    private static readonly Dictionary<MazeCellType, int> EnergyRequiredToMove = new()
    {
        [MazeCellType.A] = 1, 
        [MazeCellType.B] = 10,
        [MazeCellType.C] = 100, 
        [MazeCellType.D] = 1000
    };

    public static long Solve(Maze maze)
    {
        var startState = maze.State;
        var goalState = CalculateGoalState(startState.Rooms[0].Length);
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
            var currentMaze = new Maze(currentState);
            foreach (var (nextState, moveCost) in currentMaze.AvailableStatesWithEnergyRequired)
            {
                var newCost = currentCost + moveCost;
                if (!bestCosts.TryGetValue(nextState, out var value) || newCost < value)
                {
                    value = newCost;
                    bestCosts[nextState] = value;
                    var priority = newCost + CalculateHeuristic(nextState);
                    queue.Enqueue((nextState, newCost), priority);
                }
            }
        }
        return -1;
    }

    private static long CalculateHeuristic(MazeState state)
    {
        var corridorCost = CalculateHeuristicForCorridor(state);
        var roomsCost = CalculateHeuristicForRooms(state);
        return corridorCost + roomsCost;
    }

    private static long CalculateHeuristicForCorridor(MazeState state)
    {
        var result = 0L;
        for (var corridorCellIdx = 0; corridorCellIdx < state.Corridor.Length; corridorCellIdx++)
        {
            var cellType = state.Corridor[corridorCellIdx];
            if (cellType == MazeCellType.Empty) 
                continue;
            
            var targetRoomIdx = GetTargetRoomIndex(cellType);
            var connectionCellIdx = GetConnectionCellIndexForRoomIndex(targetRoomIdx);
            
            var steps = Math.Abs(corridorCellIdx - connectionCellIdx) + 1;
            result += (long)steps * EnergyRequiredToMove[cellType];
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
                var cellType = state.Rooms[roomIdx][d];
                if (cellType == MazeCellType.Empty) continue;
                var targetRoomIdx = GetTargetRoomIndex(cellType);
                if (targetRoomIdx == roomIdx)
                {
                    var mustMove = false;
                    for (var dd = d + 1; dd < depth; dd++)
                        if (state.Rooms[roomIdx][dd] != cellType)
                        {
                            mustMove = true; 
                            break;
                        }
                    if (!mustMove) 
                        continue;
                }
                var startConnectionCell = GetConnectionCellIndexForRoomIndex(roomIdx);
                var targetConnectionCell = GetConnectionCellIndexForRoomIndex(targetRoomIdx);
                var steps = d + 1 + Math.Abs(startConnectionCell - targetConnectionCell) + 1;
                result += (long)steps * EnergyRequiredToMove[cellType];
            }
        return result;
    }

    private static MazeState CalculateGoalState(int depth)
    {
        var corridor = Enumerable.Repeat(MazeCellType.Empty, Maze.CorridorLength).ToArray(); 
        var rooms = new MazeCellType[4][];
        for (var i = 0; i < 4; i++)
        {
            rooms[i] = new MazeCellType[depth];
            Array.Fill(rooms[i], (MazeCellType)((int)MazeCellType.A + i));
        }
        return new MazeState { Corridor = corridor, Rooms = rooms };
    }
    
    private static int GetTargetRoomIndex(MazeCellType cellType) => (int)cellType - (int)MazeCellType.A;
    
    private static int GetConnectionCellIndexForRoomIndex(int roomIndex) => roomIndex * 2 + 2;
}