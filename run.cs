using DotTest.Entities;
using DotTest.Helpers;

public class Program
{
    private static long Solve(List<string> lines)
    {
        var maze = Maze.FromStringLines(lines);
        return MazeSolver.Solve(maze);
    }
    
    public static void Main(string[] args)
    {
        var lines = new List<string>();
        while (Console.ReadLine() is { } line && !string.IsNullOrEmpty(line))
            lines.Add(line);

        var result = Solve(lines);
        Console.WriteLine(result);
    }
}