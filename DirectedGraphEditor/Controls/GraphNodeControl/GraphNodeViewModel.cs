using CommunityToolkit.Mvvm.ComponentModel;
using DirectedGraphCore.DirectedGraph;
using System.Collections.Generic;

namespace DirectedGraphEditor.Controls.GraphNodeControl;

public partial class GraphNodeViewModel : ObservableObject
{
    [ObservableProperty]
    private double x;

    [ObservableProperty]
    private double y;

    public GraphNode Node { get; }

    public NodeStyle Style { get; } = new(); // default style

    public GraphNodeViewModel(GraphNode node, double x = 0, double y = 0)
    {
        Node = node;
        this.x = x;
        this.y = y;
    }

    public string Name => Node.Name;

    public IReadOnlyList<GraphSlot> Inputs => Node.Inputs;
    public IReadOnlyList<GraphSlot> Outputs => Node.Outputs;
}