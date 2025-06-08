using System.Xml.Serialization;
using System.Collections.Generic;

namespace DirectedGraphCore;



public class GraphNode
{
    /// <summary>Unique ID for Node</summary>
    public string Id { get; }
    public string Name { get; set; }
    public List<GraphSlot> Inputs { get; } = new();
    public List<GraphSlot> Outputs { get; } = new();

    public GraphNode(string id, string name)
    {
        Id = id;
        Name = name;
    }
}

public class GraphSlot
{
    public string Name { get; }
    public GraphSlotDirection Direction { get; }
    public GraphNode Owner { get; }

    public GraphSlot(string name, GraphSlotDirection direction, GraphNode owner)
    {
        Name = name;
        Direction = direction;
        Owner = owner;
    }
}

public enum GraphSlotDirection
{
    Input,
    Output
}

public class GraphConnection
{
    public GraphSlot From { get; }
    public GraphSlot To { get; }

    public GraphConnection(GraphSlot from, GraphSlot to)
    {
        if (from.Direction != GraphSlotDirection.Output || to.Direction != GraphSlotDirection.Input)
            throw new ArgumentException("Invalid connection: must be Output -> Input");

        From = from;
        To = to;
    }
}

public class GraphModel
{
    public Dictionary<string, Node> Nodes { get; } = new();
    public List<Edge> Edges { get; } = new();

    public void AddNode(string id, string label = null)
    {
        if (!Nodes.ContainsKey(id))
            Nodes[id] = new Node { Id = id, Label = label ?? id };
    }

    public void AddEdge(string sourceId, string targetId)
    {
        AddNode(sourceId);
        AddNode(targetId);
        Edges.Add(new Edge { SourceId = sourceId, TargetId = targetId });
    }

    public void SaveToFile(string filePath)
    {
        DgmlSerializer.Save(this, filePath);
    }

    public static GraphModel LoadFromFile(string filePath)
    {
        return DgmlSerializer.Load(filePath);
    }
}

public class Node
{
    public string Id { get; set; }
    public string Label { get; set; }
}

public class Edge
{
    public string SourceId { get; set; }
    public string TargetId { get; set; }
}
