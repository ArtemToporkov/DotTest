using System;
using System.Collections.Generic;

namespace DotTest;

public interface IGraph
{
    public static abstract IGraph FromEdges(List<(string, string)> edges);
}

public class Graph : IGraph
{
    private readonly Dictionary<string, List<string>> _edges;

    private Graph(List<(string, string)> edges)
    {
        _edges = new();
        foreach (var (from, to) in edges)
        {
            _edges.TryAdd(from, new());
            _edges.TryAdd(to, new());
            _edges[from].Add(to);
            _edges[to].Add(from);
        }
    }

    public static IGraph FromEdges(List<(string, string)> edges) => new Graph(edges);
}

public static class VirusSolver<TGraph> where TGraph : IGraph
{
    public static List<string> Solve(TGraph graph, string startPosition)
    {
        return [];
    }
}

public class Program
{
    private static List<string> Solve<TGraph>(List<(string, string)> edges) where TGraph : IGraph
    {
        var graph = TGraph.FromEdges(edges);
        var result = VirusSolver<TGraph>.Solve((TGraph)graph, "a");
        return result;
    }

    public static void Main()
    {
        var edges = new List<(string, string)>();
        while (Console.ReadLine()?.Trim() is { } line && !string.IsNullOrEmpty(line))
        {
            var parts = line.Split('-');
            if (parts.Length == 2)
                edges.Add((parts[0], parts[1]));
        }

        var result = Solve<Graph>(edges);
        foreach (var edge in result)
            Console.WriteLine(edge);
    }
}