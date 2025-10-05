
////public class GraphNode
////{
////    public required string Id { get; set; }
////    public required string Label { get; set; }
////}
using DirectedGraphCore.DirectedGraph;

/// <summary>
/// Serialized DGML as 'Link'
/// </summary>
public class GraphEdge
{
    /// <summary>Id is the concatenation of SourceNodeId and TargetNodeId</summary>
    public string Id { get; set; }


    public int SourceSlotIndex { get; set; }
    public int TargetSlotIndex { get; set; }

    public GraphNode SourceNode { get; set; }
    public GraphNode TargetNode { get; set; }

    public string? SourceNodeId => SourceNode?.Id;
    public string? TargetNodeId => TargetNode?.Id;

    public GraphEdge()
    {
        Id = BuildId();
    }


    public GraphEdge(GraphNode sourceNode, GraphNode targetNode)
    {
        SourceNode = sourceNode; 
        SourceSlotIndex = 0;

        TargetNode = targetNode;
        TargetSlotIndex = 0;

        Id = BuildId(SourceNode.Id, TargetNode.Id);
    }

    public GraphEdge(GraphNode sourceNode, int sourceSlot, GraphNode targetNode, int targetSlot)
    {
        SourceNode = sourceNode;
        SourceSlotIndex = sourceSlot;

        TargetNode = targetNode;
        TargetSlotIndex = targetSlot;

        Id = BuildId(SourceNode.Id, TargetNode.Id);
    }

    /// <summary>Construct and EdgeID using source and target node IDs.</summary>
    public static string BuildId(string sourceNodeId, string targetNodeId) => $"{sourceNodeId}-{targetNodeId}";

    /// <summary>Construct and EdgeID using source and target node IDs.</summary>
    public string BuildId() => BuildId(SourceNode.Id, TargetNode.Id);
            
}
