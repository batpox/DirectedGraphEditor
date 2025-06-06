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
    public List<GraphNode> Nodes { get; } = new();
    public List<GraphConnection> Connections { get; } = new();

    public void AddNode(GraphNode node) => Nodes.Add(node);

    public void Connect(GraphSlot from, GraphSlot to)
    {
        if (Connections.Any(c => c.From == from && c.To == to)) return;
        Connections.Add(new GraphConnection(from, to));
    }

    public void Disconnect(GraphConnection connection) => Connections.Remove(connection);

    public IEnumerable<GraphConnection> GetConnectionsFrom(GraphNode node) =>
        Connections.Where(c => c.From.Owner == node);

    public IEnumerable<GraphConnection> GetConnectionsTo(GraphNode node) =>
        Connections.Where(c => c.To.Owner == node);
}
