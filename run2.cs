using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;

namespace DotTest;

public interface IGraph
{
    public IReadOnlySet<string> Gateways { get; }
    public IReadOnlySet<(string FirstNode, string SecondNode)> Edges { get; }

    public static abstract IGraph FromEdges(IReadOnlySet<(string, string)> edges);
    public IReadOnlySet<string> GetNeighbours(string node, NeighboursSort sort = NeighboursSort.None);
    public bool TryRemoveEdge(string firstNode, string secondNode);
    public IReadOnlyDictionary<string, int> FindShortestPathsDistances(string startNode);
}

public enum NeighboursSort
{
    None,
    ByAscending,
    ByDescending
}

public class GraphException(string message) : Exception(message);

public class Graph : IGraph
{
    public IReadOnlySet<string> Gateways => _gateways;
    public IReadOnlySet<(string FirstNode, string SecondNode)> Edges => _cachedEdges ??= GetEdges();

    private const int MaxEdgesSupported = 100;
    private const int MaxNodesSupported = 20;
    private IReadOnlySet<(string FirstNode, string SecondNode)>? _cachedEdges;
    private readonly Dictionary<string, SortedSet<string>> _adjacencyList;
    private readonly HashSet<string> _gateways;

    private Graph(IReadOnlySet<(string, string)> edges)
    {
        _adjacencyList = new();
        _gateways = new();

        if (edges.Count > MaxEdgesSupported)
            throw new GraphException($"More than {MaxEdgesSupported} edges are not supported");
        var nodes = new HashSet<string>();
        foreach (var (from, to) in edges)
        {
            if (from == to)
                throw new GraphException("Self-loops are not supported");
            nodes.Add(from);
            nodes.Add(to);
            if (nodes.Count > MaxNodesSupported)
                throw new GraphException($"More than {MaxNodesSupported} nodes are not supported");
            AddDirectedEdgeOrThrow(from, to);
            AddDirectedEdgeOrThrow(to, from);
            MarkIfGateway(from);
            MarkIfGateway(to);
        }
    }

    public static IGraph FromEdges(IReadOnlySet<(string, string)> edges) => new Graph(edges);

    private void AddDirectedEdgeOrThrow(string from, string to)
    {
        _adjacencyList.TryAdd(from, new(StringComparer.Ordinal));
        if (!_adjacencyList[from].Add(to))
            throw new GraphException("Multiple edges are not supported");
        _cachedEdges = null;
    }

    private void MarkIfGateway(string node)
    {
        if (IsGateway(node))
            _gateways.Add(node);
    }
    
    public IReadOnlySet<string> GetNeighbours(string node, NeighboursSort sort = NeighboursSort.None)
    {
        if (!_adjacencyList.TryGetValue(node, out var neighbors))
            return new HashSet<string>();
        if (sort is NeighboursSort.None or NeighboursSort.ByAscending)
            return neighbors;
        
        return neighbors.Reverse().ToHashSet();
    }

    public bool TryRemoveEdge(string firstNode, string secondNode)
    {
        if (!_adjacencyList.TryGetValue(firstNode, out var firstNeighbours) 
            || !firstNeighbours.Contains(secondNode))
            return false;
        if (!_adjacencyList.TryGetValue(secondNode, out var secondNeighbours) 
            || !secondNeighbours.Contains(firstNode))
            throw new GraphException(
                $"Invalid domain state: graph should contain both ({nameof(firstNode)}, {nameof(secondNode)}) " +
                $"and ({nameof(secondNode)}, {nameof(firstNode)}) edges");

        firstNeighbours.Remove(secondNode);
        secondNeighbours.Remove(firstNode);
        _cachedEdges = null;
        return true;
    }
    
    public IReadOnlyDictionary<string, int> FindShortestPathsDistances(string startNode)
    {
        var distances = new Dictionary<string, int>
        {
            [startNode] = 0
        };
        var queue = new Queue<string>();

        if (_adjacencyList.ContainsKey(startNode))
            queue.Enqueue(startNode);

        while (queue.Count > 0)
        {
            var currentNode = queue.Dequeue();
            foreach (var neighbour in GetNeighbours(currentNode, sort: NeighboursSort.ByAscending))
            {
                if (!distances.ContainsKey(neighbour))
                {
                    distances[neighbour] = distances[currentNode] + 1;
                    queue.Enqueue(neighbour);
                }
            }
        }
        return distances;
    }
    
    private IReadOnlySet<(string FirstNode, string SecondNode)> GetEdges()
    {
        var edges = new HashSet<(string FirstNode, string SecondNode)>();
        foreach (var u in _adjacencyList.Keys)
            foreach (var v in _adjacencyList[u])
                edges.Add((u, v));
        
        return edges;
    }

    private static bool IsGateway(string node) => !string.IsNullOrEmpty(node) && char.IsUpper(node[0]);
}

public class VirusSolver<TGraph> where TGraph : IGraph
{
    private readonly HashSet<string> _seenStates = new();

    public List<string> Solve(TGraph graph, string startNode)
        => TrySolve(graph, startNode, out var result) ? result : new List<string>();

    private bool TrySolve(TGraph graph, string virusPosition, out List<string> result)
    {
        result = new List<string>();
        
        if (TryHandleTerminalState(graph, virusPosition, out var isSolved))
            return isSolved;

        var candidateCuts = GenerateCandidateCuts(graph);
        
        foreach (var (gatewayToCut, nodeToCut) in candidateCuts)
        {
            var graphClone = (TGraph)TGraph.FromEdges(graph.Edges);
            graphClone.TryRemoveEdge(gatewayToCut, nodeToCut);

            if (!TrySimulateVirusMove(graphClone, virusPosition, out var nextVirusPosition))
            {
                result.Add($"{gatewayToCut}-{nodeToCut}");
                return true; 
            }
            
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

    private bool TryHandleTerminalState(TGraph graph, string virusPosition, out bool isSolved)
    {
        isSolved = false;
        var stateKey = CreateStateKey(graph, virusPosition);
        if (_seenStates.Contains(stateKey))
            return true; 

        var distances = graph.FindShortestPathsDistances(virusPosition);
        if (!TryFindVirusBestTarget(graph, distances, out _))
        {
            isSolved = true;
            return true;
        }
        
        _seenStates.Add(stateKey);
        return false;
    }

    private static List<(string gateway, string node)> GenerateCandidateCuts(TGraph graph)
    {
        var candidates = new List<(string gateway, string node)>();
        foreach (var gateway in graph.Gateways.OrderBy(g => g, StringComparer.Ordinal))
            foreach (var neighbour in graph.GetNeighbours(gateway, sort: NeighboursSort.ByAscending))
                candidates.Add((gateway, neighbour));
        
        return candidates;
    }

    private static bool TrySimulateVirusMove(TGraph graph, string currentPosition, [NotNullWhen(true)] out string? nextPosition)
    {
        nextPosition = null;
        var distancesAfterCut = graph.FindShortestPathsDistances(currentPosition);
        if (!TryFindVirusBestTarget(graph, distancesAfterCut, out var newTargetGateway))
            return false;

        var distancesToNewTarget = graph.FindShortestPathsDistances(newTargetGateway);
        return TryFindNextStepTowardsTarget(graph, currentPosition, distancesToNewTarget, out nextPosition);
    }
    
    private static string CreateStateKey(TGraph graph, string virusPosition)
    {
        var sb = new StringBuilder(virusPosition);
        sb.Append('|');
        var gatewayEdges = graph.Gateways
            .OrderBy(g => g, StringComparer.Ordinal)
            .SelectMany(g => graph.GetNeighbours(g).Select(n => $"{g}-{n}"));
        sb.Append(string.Join(",", gatewayEdges));
        return sb.ToString();
    }

    private static bool TryFindVirusBestTarget(
        TGraph graph, IReadOnlyDictionary<string, int> distances, [NotNullWhen(true)] out string? bestTarget)
    {
        bestTarget = null;
        var minDistance = int.MaxValue;

        foreach (var gateway in graph.Gateways.OrderBy(g => g, StringComparer.Ordinal))
        {
            if (distances.TryGetValue(gateway, out var distance) && distance < minDistance)
            {
                minDistance = distance;
                bestTarget = gateway;
            }
        }
        return bestTarget is not null;
    }
    
    private static bool TryFindNextStepTowardsTarget(
        TGraph graph, string currentNode, IReadOnlyDictionary<string, int> distancesToTarget, [NotNullWhen(true)] out string? nextStep)
    {
        nextStep = null;
        if (!distancesToTarget.TryGetValue(currentNode, out var currentDistance))
            return false;
        
        foreach (var neighbour in graph.GetNeighbours(currentNode, sort: NeighboursSort.ByAscending))
        {
            if (distancesToTarget.TryGetValue(neighbour, out var neighbourDistance) && 
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
        var edges = new HashSet<(string, string)>();
        while (Console.ReadLine()?.Trim() is { } line && !string.IsNullOrEmpty(line))
        {
            var parts = line.Split('-');
            if (parts.Length == 2)
                edges.Add((parts[0], parts[1]));
        }

        var graph = (Graph)Graph.FromEdges(edges);
        var solver = new VirusSolver<Graph>();
        var result = solver.Solve(graph, "a");
        
        foreach (var edge in result)
            Console.WriteLine(edge);
    }
}