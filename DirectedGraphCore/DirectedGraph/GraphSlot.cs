namespace DirectedGraphCore.DirectedGraph;

/// <summary>
/// Locations relative to a node where edges can connect. 
/// Although this is more about visualization (and not Graph Theory),
/// it has been placed with the model for convenience.
/// </summary>
public class GraphSlot
{
    /// <summary>Unique ID for Slot within the Node</summary>
    public string Id { get; }

    /// <summary>Position relative to the Node origin</summary>
    public GraphPosition Position { get; set; } = new GraphPosition(0, 0, 0);

    /// <summary>Size of the Slot for visualization</summary>
    public GraphSize Size { get; set; } = new GraphSize(0.1, 0.1, 0.1);

    /// <summary>Incoming or Outgoing</summary>
    public GraphSlotDirection Direction { get; }

    /// <summary>Each slot must belong to a Node</summary>
    public GraphNode MyNode { get; }

    /// <summary>Integer indicating which slo</summary>
    public int NodeSlotIndex { get; set; } = 0;

    public GraphSlot(string name, GraphSlotDirection direction, GraphNode gNode)
    {
        Id = name;
        Direction = direction;
        MyNode = gNode;
    }


}
