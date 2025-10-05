namespace DirectedGraphCore.DirectedGraph;

/// <summary>
/// The overall directed graph model, consisting of Nodes and Edges
/// </summary>
public class GraphModel
{
    /// Key is Node.Id
    public Dictionary<string, GraphNode> Nodes { get; } = new();
    /// Key is SourceNodeId-TargetNodeId
    public Dictionary<string, GraphEdge> Edges { get; } = new();


    /// <summary>Adds Node if not already added.</summary>
    public GraphNode FindOrAddNode(string id, string name = null)
    {
        GraphNode node = null;
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

    public void SaveToFile(string filePath)
    {
        DgmlSerializer.Save(this, filePath);
    }

    public static GraphModel LoadFromFile(string filePath)
    {
        GraphModel gm = DgmlSerializer.Load(filePath);
        return gm;
    }
}
