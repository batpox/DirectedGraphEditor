using Avalonia.Media;
using System;
using System.Collections.Generic;

using Avalonia;

namespace DirectedGraphEditor.Rendering;

/// <summary>
/// Renders an edge as one or many smooth bezier segments.
/// If viaPoints is null, renders a single curved segment like before.
/// If viaPoints contains points, renders a smooth spline through [p0] + viaPoints + [p1].
/// </summary>
public sealed class RoutedBezierEdgeRenderer : IEdgeRenderer
{
    public double Curvature { get; set; } = 0.25; // fallback, still available for single segment

    public void Render(DrawingContext ctx, Point p0, Point p1, IReadOnlyList<Point>? viaPoints, bool isSelected)
    {
        if (ctx is null) return;

        if (viaPoints == null || viaPoints.Count == 0)
        {
            // previous single-segment behavior (quadratic -> cubic conversion)
            RenderSingleSegment(ctx, p0, p1, isSelected);
            return;
        }

        // Build full set: p0, via..., p1
        var pts = new List<Point>(1 + viaPoints.Count + 1) { p0 };
        pts.AddRange(viaPoints);
        pts.Add(p1);

        // Create smooth cubic spline via Catmull-Rom -> cubic bezier conversion
        var geom = new StreamGeometry();
        using (var g = geom.Open())
        {
            g.BeginFigure(pts[0], false);

            // For each segment from P[i] -> P[i+1], we need P[i-1], P[i], P[i+1], P[i+2] to compute control points.
            for (int i = 0; i < pts.Count - 1; i++)
            {
                // define P0..P3 for Catmull-Rom; clamp endpoints by repeating ends
                var P1 = pts[Math.Max(0, i - 1)];
                var P2 = pts[i];
                var P3 = pts[i + 1];
                var P4 = pts[Math.Min(pts.Count - 1, i + 2)];

                // Catmull-Rom to Bezier control points for segment P2 -> P3
                var c1 = new Point(
                    P2.X + (P3.X - P1.X) / 6.0,
                    P2.Y + (P3.Y - P1.Y) / 6.0);
                var c2 = new Point(
                    P3.X - (P4.X - P2.X) / 6.0,
                    P3.Y - (P4.Y - P2.Y) / 6.0);

                g.CubicBezierTo(c1, c2, P3, true);
            }
        }

        var pen = new Pen(isSelected ? Brushes.DodgerBlue : Brushes.Gray, isSelected ? 2 : 1);
        ctx.DrawGeometry(null, pen, geom);

        // Arrowhead: approximate tangent near last segment end
        var last = pts.Count - 1;
        var beforeLast = Math.Max(0, last - 1);
        var tangent = new Point(pts[last].X - pts[beforeLast].X, pts[last].Y - pts[beforeLast].Y);
        var tlen = Math.Sqrt(tangent.X * tangent.X + tangent.Y * tangent.Y);
        if (tlen > 1e-6)
        {
            var dir = new Point(tangent.X / tlen, tangent.Y / tlen);
            DrawArrowhead(ctx, pts[last], dir, isSelected);
        }
    }

    private void RenderSingleSegment(DrawingContext ctx, Point p0, Point p1, bool isSelected)
    {
        var dx = p1.X - p0.X;
        var dy = p1.Y - p0.Y;
        var dist = Math.Sqrt(dx * dx + dy * dy);
        if (dist < 1e-6)
        {
            var pen = new Pen(isSelected ? Brushes.DodgerBlue : Brushes.Gray, isSelected ? 2 : 1);
            ctx.DrawLine(pen, p0, p1);
            return;
        }

        var mid = new Point((p0.X + p1.X) / 2.0, (p0.Y + p1.Y) / 2.0);
        var nx = -dy / dist;
        var ny = dx / dist;

        var cp = new Point(
            mid.X + nx * Curvature * dist,
            mid.Y + ny * Curvature * dist);

        var c1 = new Point(p0.X + (2.0 / 3.0) * (cp.X - p0.X), p0.Y + (2.0 / 3.0) * (cp.Y - p0.Y));
        var c2 = new Point(p1.X + (2.0 / 3.0) * (cp.X - p1.X), p1.Y + (2.0 / 3.0) * (cp.Y - p1.Y));

        var penStroke = new Pen(isSelected ? Brushes.DodgerBlue : Brushes.Gray, isSelected ? 2 : 1);
        var geom = new StreamGeometry();
        using (var g = geom.Open())
        {
            g.BeginFigure(p0, false);
            g.CubicBezierTo(c1, c2, p1, true);
        }

        ctx.DrawGeometry(null, penStroke, geom);

        var tangent = new Point(p1.X - c2.X, p1.Y - c2.Y);
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