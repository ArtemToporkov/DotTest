using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace DotTest;

public interface IGraph
{
    public IReadOnlyCollection<string> Gateways { get; }
    
    public static abstract IGraph FromEdges(List<(string, string)> edges);
    public IEnumerable<string> GetSortedNeighbours(string node);
    public bool TryRemoveEdge(string from, string to);
    public PathsInfo FindShortestPathsInfo(string start);
}

public record PathsInfo(IReadOnlyDictionary<string, int> Distances, IReadOnlyDictionary<string, string> CameFrom);

public class Graph : IGraph
{
    public IReadOnlyCollection<string> Gateways => _gateways;
    
    private readonly Dictionary<string, SortedSet<string>> _graph;
    private readonly HashSet<string> _gateways;

    private Graph(List<(string, string)> edges)
    {
        _graph = new();
        _gateways = new();

        foreach (var (from, to) in edges)
        {
            if (from == to)
                throw new InvalidOperationException("Self-loops are not supported");
            AddDirectedEdgeOrThrow(from, to);
            AddDirectedEdgeOrThrow(to, from);
            MarkIfGateway(from);
            MarkIfGateway(to);
        }
    }

    private void AddDirectedEdgeOrThrow(string from, string to)
    {
        _graph.TryAdd(from, new(StringComparer.Ordinal));
        if (!_graph[from].Add(to))
            throw new InvalidOperationException("Multiple edges are not supported");
    }

    private void MarkIfGateway(string node)
    {
        if (IsGateway(node))
            _gateways.Add(node);
    }

    public static IGraph FromEdges(List<(string, string)> edges) => new Graph(edges);

    public IEnumerable<string> GetSortedNeighbours(string node) => 
        _graph.TryGetValue(node, out var neighbors) ? neighbors : Enumerable.Empty<string>();

    public bool TryRemoveEdge(string from, string to)
    {
        if (!_graph.TryGetValue(from, out var fromNeighbours) || !fromNeighbours.Contains(to))
            return false;
        if (!_graph.TryGetValue(to, out var toNeighbours) || !toNeighbours.Contains(from))
            throw new InvalidOperationException(
                "Domain invalid state: graph should contain both (from, to) and (to, from) edges");

        fromNeighbours.Remove(to);
        toNeighbours.Remove(from);
        return true;
    }
    
    public PathsInfo FindShortestPathsInfo(string start)
    {
        var distances = new Dictionary<string, int> { [start] = 0 };
        var cameFrom = new Dictionary<string, string>();
        var queue = new Queue<string>();

        if (_graph.ContainsKey(start))
            queue.Enqueue(start);

        while (queue.Count > 0)
        {
            var currentNode = queue.Dequeue();
            foreach (var neighbour in GetSortedNeighbours(currentNode))
            {
                if (!distances.ContainsKey(neighbour))
                {
                    distances[neighbour] = distances[currentNode] + 1;
                    cameFrom[neighbour] = currentNode;
                    queue.Enqueue(neighbour);
                }
            }
        }
        return new PathsInfo(distances, cameFrom);
    }
    
    private static bool IsGateway(string node) 
        => !string.IsNullOrEmpty(node) && char.IsUpper(node[0]);
}

public static class VirusSolver<TGraph> where TGraph : IGraph
{
    public static List<string> Solve(TGraph graph, string startNode)
    {
        var cuts = new List<string>();
        var virusPosition = startNode;

        while (true)
        {
            var pathsFromVirusBeforeCut = graph.FindShortestPathsInfo(virusPosition);
            if (!TryFindVirusBestTarget(graph, pathsFromVirusBeforeCut, out var targetGateway))
                break; 

            if (!TryFindNodeToCutOnPath(graph, virusPosition, targetGateway, out var nodeToCut))
                break;
            
            cuts.Add($"{targetGateway}-{nodeToCut}");
            graph.TryRemoveEdge(targetGateway, nodeToCut);

            var pathsFromVirusAfterCut = graph.FindShortestPathsInfo(virusPosition);
            if (!TryFindVirusBestTarget(graph, pathsFromVirusAfterCut, out var newTargetGateway))
                break;

            var distancesToNewTarget = graph.FindShortestPathsInfo(newTargetGateway);
            if (!TryFindNextStepTowardsTarget(graph, virusPosition, distancesToNewTarget, out var nextVirusPosition))
                break;
            
            virusPosition = nextVirusPosition;
        }
        return cuts;
    }

    private static bool TryFindVirusBestTarget(
        TGraph graph, PathsInfo pathsInfo, [NotNullWhen(true)] out string? bestTarget)
    {
        bestTarget = null;
        var minDistance = int.MaxValue;

        foreach (var gateway in graph.Gateways.OrderBy(g => g, StringComparer.Ordinal))
        {
            if (pathsInfo.Distances.TryGetValue(gateway, out var distance) && distance < minDistance)
            {
                minDistance = distance;
                bestTarget = gateway;
            }
        }
        return bestTarget is not null;
    }
    
    private static bool TryFindNodeToCutOnPath(TGraph graph, string startNode, string targetGateway, [NotNullWhen(true)] out string? nodeToCut)
    {
        nodeToCut = null;
        var distancesToTarget = graph.FindShortestPathsInfo(targetGateway);
        if (!distancesToTarget.Distances.ContainsKey(startNode))
            return false;

        var currentNode = startNode;
        while (true)
        {
            if (!TryFindNextStepTowardsTarget(graph, currentNode, distancesToTarget, out var nextStep))
                return false;
            
            if (nextStep == targetGateway)
            {
                nodeToCut = currentNode;
                return true;
            }
            
            currentNode = nextStep;
        }
    }

    private static bool TryFindNextStepTowardsTarget(
        TGraph graph, string currentNode, PathsInfo distancesToTarget, [NotNullWhen(true)] out string? nextStep)
    {
        nextStep = null;
        var currentDistance = distancesToTarget.Distances[currentNode];
        
        foreach (var neighbour in graph.GetSortedNeighbours(currentNode))
        {
            if (distancesToTarget.Distances.TryGetValue(neighbour, out var neighbourDistance) && 
                neighbourDistance < currentDistance)
            {
                nextStep = neighbour;
                return true;
            }
        }
        return false;
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