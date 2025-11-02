using DirectedGraphCore.Models;
using System;

using Avalonia;

namespace DirectedGraphEditor.Controls;

public sealed class PinEventArgs : EventArgs
{
    public GraphNode Node { get; }
    public NodePin Pin { get; }
    /// <summary>Pointer location in the node control’s local space (center of the pin).</summary>
    public Point LocalPoint { get; }
    /// <summary>Pointer location in canvas space (center of the pin), if a Canvas ancestor exists.</summary>
    public Point? CanvasPoint { get; }

    public PinEventArgs(GraphNode node, NodePin pin, Point localPoint, Point? canvasPoint = null)
    {
        Node = node; Pin = pin; LocalPoint = localPoint; CanvasPoint = canvasPoint;
    }
}
