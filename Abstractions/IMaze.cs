using DotTest.Enums;
using DotTest.ValueObjects;

namespace DotTest.Abstractions;

public interface IMaze
{
    public MazeState State { get; }
    public int EnergyLost { get; }
    public Dictionary<MazeState, int> AvailableStatesWithEnergyCost { get; }
    
    public static abstract IMaze FromStringLines(List<string> lines);

    public void Move(MazeState state);
}