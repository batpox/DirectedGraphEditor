using System.Xml.Serialization;
using System.Collections.Generic;

namespace DirectedGraphCore.DirectedGraph;


/// <summary>
/// The node in a directed graph, with input and output slots where
/// the edges can connect.
/// </summary>
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

/// <summary>
/// Locations where edges can connect to a node.
/// </summary>
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

////public class GraphConnection
////{
////    public GraphSlot From { get; }
////    public GraphSlot To { get; }

////    public GraphConnection(GraphSlot from, GraphSlot to)
////    {
////        if (from.Direction != GraphSlotDirection.Output || to.Direction != GraphSlotDirection.Input)
////            throw new ArgumentException("Invalid connection: must be Output -> Input");

////        From = from;
////        To = to;
////    }
////}

/// <summary>
/// The overall directed graph model, consisting of Nodes and Edges
/// </summary>
public class GraphModel
{
    public Dictionary<string, GraphNode> Nodes { get; } = new();
    public List<GraphEdge> Edges { get; } = new();

    public void AddNode(string id, string name = null)
    {
        if (!Nodes.ContainsKey(id))
            Nodes[id] = new GraphNode( id, name );
    }

    /// <summary>
    /// An edge that connects two nodes. The nodes may be one and the same.
    /// </summary>
    /// <param name="sourceNodeId"></param>
    /// <param name="targetNodeId"></param>
    public void AddEdge(string sourceNodeId, string targetNodeId)
    {
        AddNode(sourceNodeId);
        AddNode(targetNodeId);
        Edges.Add(new GraphEdge { SourceNodeId = sourceNodeId, TargetNodeId = targetNodeId });
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

////public class GraphNode
////{
////    public required string Id { get; set; }
////    public required string Label { get; set; }
////}

public class GraphEdge
{
    public required string SourceNodeId { get; set; }
    public required string TargetNodeId { get; set; }
}
