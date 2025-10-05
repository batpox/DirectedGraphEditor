using CommunityToolkit.Mvvm.ComponentModel;


namespace DirectedGraphCore.DirectedGraph;

/// <summary>
/// The node in a directed graph, with input and output slots where
/// the edges can connect.
/// </summary>
public class GraphNode : ObservableObject
{
    /// <summary>Unique ID for Node</summary>
    public string Id { get; }

    private string name;
    public string Name
    {
        get => name;
        set => SetProperty(ref name, value);
    }

    private GraphPosition position;
    public GraphPosition Position
    {
        get => position;
        set => SetProperty(ref position, value);
    }

    public GraphSize? Size { get; set; }

    public List<GraphSlot> Inputs { get; } = new();
    public List<GraphSlot> Outputs { get; } = new();

    public GraphNode(string id, string name)
    {
        Id = id;
        this.name = name;

        position = new GraphPosition(0, 0, 0);
    }
}
