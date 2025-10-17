using System.Text.Json;
using DirectedGraphCore.Models;
using System.Collections.ObjectModel;

namespace DirectedGraphCore.Models;

/// <summary>
/// The overall directed graph model, consisting of Nodes and Edges.
/// Edges attach to specific <see cref="NodePin"/>s (SourcePinId/TargetPinId).
/// </summary>
public class GraphModel
{
    /// <summary>Keyed by <see cref="GraphNode.Id"/>.</summary>
    public Dictionary<string, GraphNode> Nodes { get; } = new();

    /// <summary>Keyed by <see cref="GraphEdge.Id"/> (stable GUID per edge).</summary>
    public Dictionary<string, GraphEdge> Edges { get; } = new();

    /// <summary>Full path to the model's store.</summary>
    public string FilePath { get; set; } = string.Empty;

    // ───────────────────────────────────────────────────────────────
    // Loading / Reset
    // ───────────────────────────────────────────────────────────────
    public void ResetFromJsonFile(string path)
    {
        if (!File.Exists(path))
            throw new FileNotFoundException("Graph file not found", path);

        var json = File.ReadAllText(path);
        var model = JsonSerializer.Deserialize<GraphModel>(json);
        if (model == null)
            throw new InvalidOperationException("Invalid or empty graph file.");

        ResetFrom(model);
        FilePath = path;
    }

    /// <summary>Reset from a DGML (Directed Graph Markup Language) file.</summary>
    public void LoadFromDgmlFile(string path)
    {
        if (!File.Exists(path))
            throw new FileNotFoundException("Graph file not found", path);

        GraphModel model = DgmlSerializer.LoadFromDgml(path);
        ResetFrom(model);
        FilePath = path;
    }

    /// <summary>Reset this model from another model by clearing and rebuilding nodes, edges, ...</summary>
    public void ResetFrom(GraphModel source)
    {
        Nodes.Clear();
        Edges.Clear();

        foreach (var kv in source.Nodes)
            Nodes[kv.Key] = kv.Value;

        foreach (var kv in source.Edges)
            Edges[kv.Key] = kv.Value;
    }

    // ───────────────────────────────────────────────────────────────
    // Nodes
    // ───────────────────────────────────────────────────────────────
    /// <summary>Adds Node if not already added.</summary>
    public GraphNode FindOrAddNode(string id, string? name = null)
    {
        if (!Nodes.TryGetValue(id, out var node))
        {
            node = new GraphNode(id, name ?? id);
            Nodes[id] = node;
        }
        return node;
    }

    public bool TryGetNode(string nodeId, out GraphNode node) => Nodes.TryGetValue(nodeId, out node!);

    // ───────────────────────────────────────────────────────────────
    // Pins
    // ───────────────────────────────────────────────────────────────
    /// <summary>Get a pin by nodeId + pinId; returns null if not found.</summary>
    public NodePin? GetPin(string nodeId, string pinId)
    {
        return TryGetNode(nodeId, out var node)
            ? node.FindPin(pinId)
            : null;
    }

    /// <summary>Simple predicate to validate a directed connection.</summary>
    public static bool CanConnect(NodePin src, NodePin dst) =>
        src.Direction == EnumNodePinDirection.Output && dst.Direction == EnumNodePinDirection.Input;

    // ───────────────────────────────────────────────────────────────
    // Edges (pin-aware)
    // ───────────────────────────────────────────────────────────────
    /// <summary>
    /// Create a new edge from (sourceNodeId, sourcePinId) → (targetNodeId, targetPinId).
    /// Throws if nodes/pins are missing or directions don’t match.
    /// </summary>
    public GraphEdge AddEdge(string sourceNodeId, string sourcePinId, string targetNodeId, string targetPinId)
    {
        var srcNode = FindOrAddNode(sourceNodeId);
        var dstNode = FindOrAddNode(targetNodeId);

        var srcPin = srcNode.FindPin(sourcePinId)
                    ?? throw new InvalidOperationException($"Source pin '{sourcePinId}' not found on node '{sourceNodeId}'.");
        var dstPin = dstNode.FindPin(targetPinId)
                    ?? throw new InvalidOperationException($"Target pin '{targetPinId}' not found on node '{targetNodeId}'.");

        if (!CanConnect(srcPin, dstPin))
            throw new InvalidOperationException($"Invalid connection: {srcPin.Direction} → {dstPin.Direction}.");

        var edge = new GraphEdge
        {
            // Id is auto (Guid) inside GraphEdge
            SourceNodeId = srcNode.Id,
            SourcePinId = srcPin.Id,
            TargetNodeId = dstNode.Id,
            TargetPinId = dstPin.Id
        };

        Edges[edge.Id] = edge;
        return edge;
    }

    /// <summary>
    /// Convenience: add an edge using pin *indices* on each side (0-based).
    /// Picks Outputs[index] on source, Inputs[index] on target.
    /// </summary>
    public GraphEdge AddEdgeByIndex(string sourceNodeId, int sourceOutputIndex, string targetNodeId, int targetInputIndex)
    {
        var src = FindOrAddNode(sourceNodeId);
        var dst = FindOrAddNode(targetNodeId);

        if (sourceOutputIndex < 0 || sourceOutputIndex >= src.Outputs.Count)
            throw new ArgumentOutOfRangeException(nameof(sourceOutputIndex));
        if (targetInputIndex < 0 || targetInputIndex >= dst.Inputs.Count)
            throw new ArgumentOutOfRangeException(nameof(targetInputIndex));

        var srcPin = src.Outputs[sourceOutputIndex];
        var dstPin = dst.Inputs[targetInputIndex];

        return AddEdge(src.Id, srcPin.Id, dst.Id, dstPin.Id);
    }

    /// <summary>
    /// Back-compat convenience (no pins specified): connects first output of source to first input of target.
    /// Creates default pins if missing.
    /// </summary>
    public GraphEdge FindOrAddEdge(string sourceNodeId, string targetNodeId)
    {
        var src = FindOrAddNode(sourceNodeId);
        var dst = FindOrAddNode(targetNodeId);

        if (src.Outputs.Count == 0) src.AddOutput();
        if (dst.Inputs.Count == 0) dst.AddInput();

        var srcPin = src.Outputs[0];
        var dstPin = dst.Inputs[0];

        // Reuse existing identical edge if one exists
        var existing = Edges.Values.FirstOrDefault(e =>
            e.SourceNodeId == src.Id && e.SourcePinId == srcPin.Id &&
            e.TargetNodeId == dst.Id && e.TargetPinId == dstPin.Id);

        return existing ?? AddEdge(src.Id, srcPin.Id, dst.Id, dstPin.Id);
    }

    /// <summary>Remove an edge by its Id; returns true if removed.</summary>
    public bool RemoveEdge(string edgeId) => Edges.Remove(edgeId);

    /// <summary>Remove all edges attached to a node (any pin).</summary>
    public int RemoveEdgesOfNode(string nodeId)
    {
        var toDelete = Edges.Values
            .Where(e => e.SourceNodeId == nodeId || e.TargetNodeId == nodeId)
            .Select(e => e.Id)
            .ToList();

        foreach (var id in toDelete) Edges.Remove(id);
        return toDelete.Count;
    }

    // ───────────────────────────────────────────────────────────────
    // DGML helpers (unchanged surface)
    // ───────────────────────────────────────────────────────────────
    public void SaveAsDgml(string filePath)
    {
        DgmlSerializer.SaveAsDgml(this, filePath);
        FilePath = filePath;
    }

    public static GraphModel LoadFromFile(string filePath)
    {
        GraphModel gm = DgmlSerializer.LoadFromDgml(filePath);
        gm.FilePath = filePath;
        return gm;
    }

    // Returns all edges incident to a specific pin
    public IEnumerable<GraphEdge> EdgesOfPin(string nodeId, string pinId) =>
        Edges.Values.Where(e =>
            (e.SourceNodeId == nodeId && e.SourcePinId == pinId) ||
            (e.TargetNodeId == nodeId && e.TargetPinId == pinId));

    public bool HasEdgesOnPin(string nodeId, string pinId) =>
        EdgesOfPin(nodeId, pinId).Any();

}
