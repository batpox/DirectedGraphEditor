using System.Text.Json.Serialization;

namespace DirectedGraphCore.Models;

public enum EnumNodePinDirection { Input, Output }

/// <summary>Attachment point on a node where edges can connect.</summary>
public class NodePin
{
    /// <summary>Stable ID used by edges and persistence.</summary>
    public string Id { get; } = Guid.NewGuid().ToString("N");

    /// <summary>Incoming or Outgoing.</summary>
    public EnumNodePinDirection Direction { get; }

    /// <summary>Order within its side; safe to change without breaking edges.</summary>
    public int Index { get; set; } = 0;

    /// <summary>Human label (optional).</summary>
    public string? Label { get; set; }

    /// <summary>How many edges may attach (1 = normal).</summary>
    public int Capacity { get; set; } = 1;

    /// <summary>Visual size.</summary>
    public GraphSize Size { get; set; } = new(0.1, 0.1, 0.1);

    /// <summary>Owning node ID (stable for persistence).</summary>
    public string NodeId { get; }

    /// <summary>Pretty ID for UI only (changes safely when Index/Direction change).</summary>
    [JsonIgnore] public string DisplayId => $"{(Direction == EnumNodePinDirection.Input ? "In" : "Out")}-{Index:000}";

    /// <summary>Optional back reference; ignore in JSON to avoid cycles.</summary>
    [JsonIgnore] public GraphNode? Node { get; }

    public NodePin(int index, EnumNodePinDirection direction, GraphNode node)
    {
        Index = index;
        Direction = direction;
        Node = node;
        NodeId = node.Id;
    }
}
