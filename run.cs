using DotTest.Abstractions;
using DotTest.Entities;
using DotTest.Enums;

namespace DotTest;

public static class Program
{
    private static int Solve(List<string> lines)
    {
        var maze = Maze.FromStringLines(lines);
        return 0;
    }

    public static void Main()
    {
        var lines = new List<string>();

        while (Console.ReadLine() is { } line)
            lines.Add(line);

        var result = Solve(lines);
        Console.WriteLine(result);
    }
}