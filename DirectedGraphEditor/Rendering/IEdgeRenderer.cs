// Rendering/IEdgeRenderer.cs
using Avalonia;
using Avalonia.Media;
using System.Collections.Generic;

namespace DirectedGraphEditor.Rendering;

public interface IEdgeRenderer
{
    void Render(DrawingContext ctx, Point p0, Point p1, IReadOnlyList<Point>? viaPoints, bool isSelected);
}
