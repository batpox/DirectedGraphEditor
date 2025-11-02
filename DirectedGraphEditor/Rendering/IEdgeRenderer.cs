// Rendering/IEdgeRenderer.cs
using Avalonia;
using Avalonia.Media;

namespace DirectedGraphEditor.Rendering;

public interface IEdgeRenderer
{
    void Render(DrawingContext ctx, Point p0, Point p1, bool isSelected);
}
