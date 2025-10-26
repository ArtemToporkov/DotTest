using DotTest.Enums;

namespace DotTest.ValueObjects;

public readonly struct MazeState : IEquatable<MazeState>
{
    public required MazePositionType[] Corridor { get; init; }
    public required MazePositionType[][] Rooms { get; init; }

    public bool Equals(MazeState other)
        => Corridor.AsSpan().SequenceEqual(other.Corridor)
           && Rooms.Length == other.Rooms.Length
           && Rooms.Zip(other.Rooms).All(pair => pair.First.AsSpan().SequenceEqual(pair.Second));

    public override bool Equals(object? obj)
        => obj is MazeState other && Equals(other);

    public override int GetHashCode()
    {
        var hash = new HashCode();

        foreach (var c in Corridor)
            hash.Add(c);

        foreach (var room in Rooms)
            foreach (var pos in room)
                hash.Add(pos);

        return hash.ToHashCode();
    }

    public static bool operator ==(MazeState left, MazeState right) => left.Equals(right);
    public static bool operator !=(MazeState left, MazeState right) => !left.Equals(right);
}