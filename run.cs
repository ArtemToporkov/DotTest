using DotTest.Abstractions;
using DotTest.Entities;
using DotTest.Helpers;

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