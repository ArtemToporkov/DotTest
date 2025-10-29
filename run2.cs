using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace DotTest;

public interface IGraph
{
    public IReadOnlyCollection<string> Gateways { get; }
    
    public IEnumerable<string> GetSortedNeighbours(string node);
    public bool TryRemoveEdge(string from, string to);
    public PathsInfo FindShortestPathsInfo(string start);
    public IGraph Clone();
    public IEnumerable<(string u, string v)> GetEdges();
}

public record PathsInfo(IReadOnlyDictionary<string, int> Distances);

public class Graph : IGraph
{
    public IReadOnlyCollection<string> Gateways => _gateways;
    
    private readonly Dictionary<string, SortedSet<string>> _graph;
    private readonly HashSet<string> _gateways;

    public Graph(IEnumerable<(string, string)> edges)
    {
        _graph = new();
        _gateways = new();

        foreach (var (from, to) in edges)
        {
            AddDirectedEdge(from, to);
            AddDirectedEdge(to, from);
            MarkIfGateway(from);
            MarkIfGateway(to);
        }
    }

    private void AddDirectedEdge(string from, string to)
    {
        _graph.TryAdd(from, new(StringComparer.Ordinal));
        _graph[from].Add(to);
    }

    private void MarkIfGateway(string node)
    {
        if (IsGateway(node))
            _gateways.Add(node);
    }
    
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
        return new PathsInfo(distances);
    }
    
    public IEnumerable<(string u, string v)> GetEdges()
    {
        var edges = new List<(string u, string v)>();
        foreach (var u in _graph.Keys.OrderBy(k => k, StringComparer.Ordinal))
            foreach (var v in _graph[u])
                if (StringComparer.Ordinal.Compare(u, v) < 0)
                    edges.Add((u, v));
        return edges;
    }

    public IGraph Clone() => new Graph(GetEdges());

    private static bool IsGateway(string node) 
        => !string.IsNullOrEmpty(node) && char.IsUpper(node[0]);
}

public class VirusSolver<TGraph> where TGraph : IGraph
{
    private readonly HashSet<string> _seenStates = new();

    public List<string> Solve(TGraph graph, string startNode)
        => TrySolve(graph, startNode, out var result) ? result : new List<string>();

    private bool TrySolve(TGraph graph, string virusPosition, out List<string> result)
    {
        result = [];
        
        var stateKey = MakeStateKey(graph, virusPosition);
        if (_seenStates.Contains(stateKey))
            return false;
        
        var pathsFromVirus = graph.FindShortestPathsInfo(virusPosition);
        if (!TryFindVirusBestTarget(graph, pathsFromVirus, out _))
            return true;
        
        _seenStates.Add(stateKey);

        var candidateCuts = new List<(string gateway, string node)>();
        foreach (var gateway in graph.Gateways.OrderBy(g => g, StringComparer.Ordinal))
            foreach (var neighbour in graph.GetSortedNeighbours(gateway))
                candidateCuts.Add((gateway, neighbour));
        
        foreach (var (gatewayToCut, nodeToCut) in candidateCuts)
        {
            var graphClone = (TGraph)graph.Clone();
            graphClone.TryRemoveEdge(gatewayToCut, nodeToCut);

            var pathsAfterCut = graphClone.FindShortestPathsInfo(virusPosition);
            if (!TryFindVirusBestTarget(graphClone, pathsAfterCut, out var newTargetGateway))
            {
                result.Add($"{gatewayToCut}-{nodeToCut}");
                return true; 
            }
            
            var distancesToNewTarget = graphClone.FindShortestPathsInfo(newTargetGateway);
            if (!TryFindNextStepTowardsTarget(
                    graphClone, virusPosition, distancesToNewTarget, out var nextVirusPosition))
                continue;

            if (char.IsUpper(nextVirusPosition[0]))
                continue;

            if (TrySolve(graphClone, nextVirusPosition, out var subsequentCuts))
            {
                result.Add($"{gatewayToCut}-{nodeToCut}");
                result.AddRange(subsequentCuts);
                return true;
            }
        }

        return false;
    }

    private static string MakeStateKey(TGraph graph, string virusPosition)
    {
        var gatewayEdges = new List<string>();
        foreach (var gateway in graph.Gateways.OrderBy(g => g, StringComparer.Ordinal))
            foreach (var neighbour in graph.GetSortedNeighbours(gateway))
                gatewayEdges.Add($"{gateway}-{neighbour}");
        return $"{virusPosition}|{string.Join(",", gatewayEdges)}";
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
    
    private static bool TryFindNextStepTowardsTarget(
        TGraph graph, string currentNode, PathsInfo distancesToTarget, [NotNullWhen(true)] out string? nextStep)
    {
        nextStep = null;
        if (!distancesToTarget.Distances.TryGetValue(currentNode, out var currentDistance))
            return false;
        
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
    public static void Main()
    {
        var edges = new List<(string, string)>();
        while (Console.ReadLine()?.Trim() is { } line && !string.IsNullOrEmpty(line))
        {
            var parts = line.Split('-');
            if (parts.Length == 2)
                edges.Add((parts[0], parts[1]));
        }

        var graph = new Graph(edges);
        var solver = new VirusSolver<Graph>();
        var result = solver.Solve(graph, "a");
        
        foreach (var edge in result)
            Console.WriteLine(edge);
    }
}