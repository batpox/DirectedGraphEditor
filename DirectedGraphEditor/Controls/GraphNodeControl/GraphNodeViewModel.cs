using CommunityToolkit.Mvvm.ComponentModel;
using DirectedGraphCore.DirectedGraph;
using System.Collections.Generic;

namespace DirectedGraphEditor.Controls.GraphNodeControl;

public sealed class GraphNodeViewModel : ObservableObject
{
    private double _x;
    private double _y;

    public GraphNode Node { get; }

    public GraphNodeViewModel(GraphNode node)
    {
        Node = node;
        _x = node.Position.X;
        _y = node.Position.Y;

        node.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(GraphNode.Position))
            {
                SetProperty(ref _x, node.Position.X, nameof(X));
                SetProperty(ref _y, node.Position.Y, nameof(Y));
            }
        };
    }

    // X coordinate (binds to Canvas.Left, etc.)
    public double X
    {
        get => _x;
        set { if (SetProperty(ref _x, value)) Node.Position.X = value; }
    }

    // Y coordinate (binds to Canvas.Top, etc.)
    public double Y
    {
        get => _y;
        set { if (SetProperty(ref _y, value)) Node.Position.Y = value; }
    }


    public NodeStyle Style { get; } = new(); // default style

    public string Name => Node.Name;

    public IReadOnlyList<GraphSlot> Inputs => Node.Inputs;
    public IReadOnlyList<GraphSlot> Outputs => Node.Outputs;
}