// Rendering/LinearEdgeRenderer.cs
using Avalonia;
using Avalonia.Media;
using System;
using System.Collections.Generic;

namespace DirectedGraphEditor.Rendering;

public sealed class LinearEdgeRenderer : IEdgeRenderer
{
    public bool ShowArrowHead { get; init; } = true;

    public void Render(DrawingContext ctx, Point p0, Point p1, IReadOnlyList<Point>? viaPoints, bool isSelected)
    {
        if (ctx is null) return;

        var pen = new Pen(isSelected ? Brushes.DodgerBlue : Brushes.Gray, isSelected ? 2 : 1);

        // Build geometry for polyline (p0 -> viaPoints... -> p1) or simple line
        if (viaPoints == null || viaPoints.Count == 0)
        {
            ctx.DrawLine(pen, p0, p1);
        }
        else
        {
            var geom = new StreamGeometry();
            using (var g = geom.Open())
            {
                g.BeginFigure(p0, false);
                foreach (var v in viaPoints) g.LineTo(v, true);
                g.LineTo(p1, true);
            }
            ctx.DrawGeometry(null, pen, geom);
        }

        // Compute tangent for arrowhead: last segment vector
        Point lastFrom;
        if (viaPoints == null || viaPoints.Count == 0)
            lastFrom = p0;
        else
            lastFrom = viaPoints[viaPoints.Count - 1];

        var tangent = new Point(p1.X - lastFrom.X, p1.Y - lastFrom.Y);
        var tlen = Math.Sqrt(tangent.X * tangent.X + tangent.Y * tangent.Y);
        if (tlen > 1e-6)
        {
            var dir = new Point(tangent.X / tlen, tangent.Y / tlen);
            DrawArrowhead(ctx, p1, dir, isSelected);
        }
    }
    private static void DrawArrowhead(DrawingContext ctx, Point tip, Point dir, bool isSelected)
    {
        const double len = 10.0;
        const double spread = Math.PI / 8.0;

        var angle = Math.Atan2(dir.Y, dir.X);
        var a1 = angle + spread;
        var a2 = angle - spread;

        var p1 = new Point(tip.X - len * Math.Cos(a1), tip.Y - len * Math.Sin(a1));
        var p2 = new Point(tip.X - len * Math.Cos(a2), tip.Y - len * Math.Sin(a2));

        var geom = new StreamGeometry();
        using (var g = geom.Open())
        {
            g.BeginFigure(tip, true);
            g.LineTo(p1, true);
            g.LineTo(p2, true);
            g.LineTo(tip, true);
        }

        var fill = isSelected ? Brushes.DodgerBlue : Brushes.Gray;
        ctx.DrawGeometry(fill, null, geom);
    }
}
