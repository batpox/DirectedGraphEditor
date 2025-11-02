// Rendering/LinearEdgeRenderer.cs
using Avalonia;
using Avalonia.Media;

namespace DirectedGraphEditor.Rendering;

public sealed class LinearEdgeRenderer : IEdgeRenderer
{
    public bool ShowArrowHead { get; init; } = true;

    public void Render(DrawingContext ctx, Point p0, Point p1, bool isSelected)
    {
        var pen = new Pen(isSelected ? Brushes.DodgerBlue : Brushes.Gray, isSelected ? 2.5 : 1.5);
        ctx.DrawLine(pen, p0, p1);

        if (!ShowArrowHead) return;

        Vector v = p1 - p0;
        var len = v.Length;
        if (len < 0.001) return;

        var n = v / len;
        var back = p1 - n * 12;
        var left = new Point(back.X - n.Y * 5, back.Y + n.X * 5);
        var right = new Point(back.X + n.Y * 5, back.Y - n.X * 5);

        var geo = new StreamGeometry();
        using (var g = geo.Open())
        {
            g.BeginFigure(p1, isFilled: true);
            g.LineTo(left);
            g.LineTo(right);
            g.EndFigure( isClosed: true);
        }
        var brush = isSelected ? Brushes.DodgerBlue : Brushes.Gray;
        ctx.DrawGeometry(brush, pen:null, geo);
    }
}
