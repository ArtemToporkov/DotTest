using DotTest.ValueObjects;

namespace DotTest.Abstractions;

public interface IMaze
{
    public MazeState State { get; }
    public static abstract int CorridorLength { get; }
    public Dictionary<MazeState, int> AvailableStatesWithEnergyRequired { get; }
    
    public static abstract IMaze FromStringLines(List<string> lines);
    
    public static abstract IMaze FromMazeState(MazeState state);
}