using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using DirectedGraphCore.Geometry;

namespace DirectedGraphCore.Models;

/// <summary>
/// A node in a directed graph, with input/output pins that edges can attach to.
/// </summary>
public class GraphNode : ObservableObject
{
    /// <summary>Stable node ID.</summary>
    public string Id { get; }

    private string name;
    public string Name
    {
        get => name;
        set => SetProperty(ref name, value);
    }

    private Point3 position;
    public Point3 Position
    {
        get => position;
        set => SetProperty(ref position, value);
    }

    public Size3? Size { get; set; }

    /// <summary>Pins rendered on the left (incoming).</summary>
    public ObservableCollection<NodePin> Inputs { get; } = new();

    /// <summary>Pins rendered on the right (outgoing).</summary>
    public ObservableCollection<NodePin> Outputs { get; } = new();

    public GraphNode(string id, string name)
    {
        Id = id;
        this.name = name;
        position = new Point3(0, 0, 0);
    }

    // --------------------------
    // Convenience helpers
    // --------------------------

    /// <summary>Add an input pin at the end (auto Index).</summary>
    public NodePin AddInput(string? label = null, int capacity = 1)
    {
        var pin = new NodePin(index: Inputs.Count, direction: EnumNodePinDirection.Input, node: this)
        {
            Label = label,
            Capacity = capacity
        };
        Inputs.Add(pin);
        return pin;
    }

    /// <summary>Add an output pin at the end (auto Index).</summary>
    public NodePin AddOutput(string? label = null, int capacity = 1)
    {
        var pin = new NodePin(index: Outputs.Count, direction: EnumNodePinDirection.Output, node: this)
        {
            Label = label,
            Capacity = capacity
        };
        Outputs.Add(pin);
        return pin;
    }

    /// <summary>Insert an input pin at a specific index and reindex that side.</summary>
    public NodePin InsertInput(int atIndex, string? label = null, int capacity = 1)
    {
        atIndex = Clamp(atIndex, 0, Inputs.Count);
        var pin = new NodePin(atIndex, EnumNodePinDirection.Input, this) { Label = label, Capacity = capacity };
        Inputs.Insert(atIndex, pin);
        Reindex(Inputs);
        return pin;
    }

    /// <summary>Insert an output pin at a specific index and reindex that side.</summary>
    public NodePin InsertOutput(int atIndex, string? label = null, int capacity = 1)
    {
        atIndex = Clamp(atIndex, 0, Outputs.Count);
        var pin = new NodePin(atIndex, EnumNodePinDirection.Output, this) { Label = label, Capacity = capacity };
        Outputs.Insert(atIndex, pin);
        Reindex(Outputs);
        return pin;
    }

    /// <summary>Remove a pin (reindexes its side). Returns true if removed.</summary>
    public bool RemovePin(NodePin pin)
    {
        bool removed = pin.Direction switch
        {
            EnumNodePinDirection.Input => Inputs.Remove(pin),
            EnumNodePinDirection.Output => Outputs.Remove(pin),
            _ => false
        };
        if (removed)
        {
            Reindex(pin.Direction == EnumNodePinDirection.Input ? Inputs : Outputs);
        }
        return removed;
    }

    /// <summary>Find a pin by its stable PinId.</summary>
    public NodePin? FindPin(string pinId)
        => Inputs.FirstOrDefault(p => p.Id == pinId)
        ?? Outputs.FirstOrDefault(p => p.Id == pinId);

    // ...
    public NodePin? FindPinById(string pinId)
        => Inputs.Concat(Outputs).FirstOrDefault(p => p.Id == pinId);

    /// <summary>All pins on this node (inputs first, then outputs).</summary>
    public IEnumerable<NodePin> AllPins()
        => Inputs.Concat(Outputs);

    // --------------------------
    // Internal utilities
    // --------------------------

    private static void Reindex(ObservableCollection<NodePin> pins)
    {
        for (int i = 0; i < pins.Count; i++)
            pins[i].Index = i;
    }

    private static int Clamp(int value, int min, int max)
        => value < min ? min : (value > max ? max : value);
}
