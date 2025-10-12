namespace DirectedGraphCore.Models;

/// <summary>
/// Locations relative to a node where edges can connect. 
/// Although this is more about visualization (and not Graph Theory),
/// it has been placed with the model for convenience.
/// </summary>
public class GraphSlot
{
    /// <summary>Unique ID for Slot within the Node. E.g. "In-001" or "Out-003" </summary>
    public string Id
    {
        get
        {
            string prefix = Direction == GraphSlotDirection.Input ? "In" : "Out";
            return $"{prefix}-{Index:000}";
        }
    }

    /// <summary>Incoming or Outgoing</summary>
    public GraphSlotDirection Direction { get; }
    /// <summary>Integer indicating which slot index to use. Not unique since In can have the same index as an out</summary>
    public int Index { get; set; } = 0;


    /// <summary>Position relative to the Node origin</summary>
    public GraphPosition Position { get; set; } = new GraphPosition(0, 0, 0);

    /// <summary>Size of the Slot for visualization</summary>
    public GraphSize Size { get; set; } = new GraphSize(0.1, 0.1, 0.1);


    /// <summary>Each slot must belong to a Node</summary>
    public GraphNode MyNode { get; }


    public GraphSlot(int index, GraphSlotDirection direction, GraphNode gNode)
    {
        Index = index;
        Direction = direction;
        MyNode = gNode;
    }


}
