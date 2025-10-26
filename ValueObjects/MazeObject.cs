using DotTest.Enums;
using DotTest.Helpers;

namespace DotTest.ValueObjects;

public readonly record struct MazeObject(MazeObjectType Type)
{
    public int EnergyRequiredToMove => Type.GetEnergyCost();
    public int TargetRoomIdx => Type.GetTargetRoomIndex();
}