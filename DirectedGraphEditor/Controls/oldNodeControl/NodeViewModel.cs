using CommunityToolkit.Mvvm.ComponentModel;
using DirectedGraphCore.Models;
using System.Collections.Generic;

namespace DirectedGraphEditor.Controls.OldNodeControl;

public sealed class NodeViewModel : ObservableObject
{
    private float _x;
    private float _y;

    ////public float X => Node.Position.X;
    ////public float Y => Node.Position.Y;



    public GraphNode Node { get; }
    public string Name => Node.Name;

    public NodeViewModel(GraphNode node)
    {
        Node = node; // reference the model node
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
    public float X
    {
        get => _x;
        set { if (SetProperty(ref _x, value)) Node.Position.X = value; }
    }

    // Y coordinate (binds to Canvas.Top, etc.)
    public float Y
    {
        get => _y;
        set { if (SetProperty(ref _y, value)) Node.Position.Y = value; }
    }


    public NodeStyle Style { get; } = new(); // default style

    public IEnumerable<NodePin> Inputs => Node.Inputs;
    public IEnumerable<NodePin> Outputs => Node.Outputs;


}