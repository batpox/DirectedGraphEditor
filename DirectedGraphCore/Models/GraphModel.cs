using System.Text.Json;

namespace DirectedGraphCore.Models;

/// <summary>
/// The overall directed graph model, consisting of Nodes and Edges
/// </summary>
public class GraphModel
{
    /// Key is Node.Id
    public Dictionary<string, GraphNode> Nodes { get; } = new();
    /// Key is SourceNodeId-TargetNodeId
    public Dictionary<string, GraphEdge> Edges { get; } = new();

    /// <summary>Full path to the model's store</summary>
    public string FilePath { get; set; } = string.Empty;

    // ─── Resets: tear down and rebuild  ─────────────────────────────
    public void ResetFromJsonFile(string path)
    {
        if (!File.Exists(path))
            throw new FileNotFoundException("Graph file not found", path);

        var json = File.ReadAllText(path);
        var model = JsonSerializer.Deserialize<GraphModel>(json);
        if (model == null)
            throw new InvalidOperationException("Invalid or empty graph file.");

        ResetFrom(model);
    }

    /// <summary>Reset from a DGML (Direct Graph Markup Language) file</summary>
    public void LoadFromDgmlFile(string path)
    {
        if (!File.Exists(path))
            throw new FileNotFoundException("Graph file not found", path);

        GraphModel model = DgmlSerializer.LoadFromDgml(path);
        ResetFrom(model);
    }

    /// <summary>Reset this model from another model by clearing and rebuilding nodes, edges, ...<summary>
    public void ResetFrom(GraphModel source)
    {
        Nodes.Clear();
        Edges.Clear();
        foreach (var kv in source.Nodes)
            Nodes[kv.Key] = kv.Value;
        foreach (var kv in source.Edges)
            Edges[kv.Key] = kv.Value;
    }


    /// <summary>Adds Node if not already added.</summary>
    public GraphNode FindOrAddNode(string id, string name = null)
    {
        GraphNode? node;
        if (!Nodes.TryGetValue(id, out node) )
        {
            node = new GraphNode(id, name ?? id);
            Nodes[id] = node;
        }
        return node;
    }

    /// <summary>Add edge that connects two nodes if not there already. The nodes may be one and the same.
    /// </summary>
    /// <param name="sourceNodeId"></param>
    /// <param name="targetNodeId"></param>
    public GraphEdge FindOrAddEdge(string sourceNodeId, string targetNodeId)
    {
        GraphNode sourceNode = FindOrAddNode(sourceNodeId);
        GraphNode targetNode = FindOrAddNode(targetNodeId);

        string edgeId = GraphEdge.BuildId(sourceNode.Id, targetNode.Id);

        if (!Edges.TryGetValue(edgeId, out var edge))
        {
            edge = new GraphEdge(sourceNode, targetNode);
            Edges[edgeId] = edge;
        }
        return edge;
    }

    public void SaveAsDgml(string filePath)
    {
        DgmlSerializer.SaveAsDgml(this, filePath);
    }

    public static GraphModel LoadFromFile(string filePath)
    {
        GraphModel gm = DgmlSerializer.LoadFromDgml(filePath);
        return gm;
    }
}
