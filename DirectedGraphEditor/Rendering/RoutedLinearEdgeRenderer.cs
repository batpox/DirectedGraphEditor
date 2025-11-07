using Avalonia;
using Avalonia.Media;
using System;
using System.Collections.Generic;

namespace DirectedGraphEditor.Rendering;

/// <summary>
/// Routed linear renderer that draws a multi-segment polyline but replaces sharp corners
/// with small rounded joins (quadratic beziers) so turns are visually smooth and arrowheads
/// can be inset so they don't fall under node rectangles.
/// </summary>
public sealed class RoutedLinearEdgeRenderer : IEdgeRenderer
{
    public double CornerRadius { get; set; } = 8.0;      // px radius for rounding corners
    public double ArrowInset { get; set; } = 10.0;       // inset arrow tip from final point
    public double ArrowLength { get; set; } = 10.0;
    public double ArrowSpread { get; set; } = Math.PI / 8.0;

    public void Render(DrawingContext ctx, Point p0, Point p1, IReadOnlyList<Point>? viaPoints, bool isSelected)
    {
        if (ctx is null) return;

        // Build full point list
        var pts = new List<Point> { p0 };
        if (viaPoints != null && viaPoints.Count > 0) pts.AddRange(viaPoints);
        pts.Add(p1);

        if (pts.Count < 2)
            return;

        // Compute inset tip so arrow isn't drawn over node
        var lastFrom = pts[pts.Count - 2];
        var dx = p1.X - lastFrom.X;
        var dy = p1.Y - lastFrom.Y;
        var segLen = Math.Sqrt(dx * dx + dy * dy);
        var dir = segLen > 1e-6 ? new Point(dx / segLen, dy / segLen) : new Point(1, 0);
        var tip = new Point(p1.X - dir.X * ArrowInset, p1.Y - dir.Y * ArrowInset);

        // Create geometry with rounded joins
        var geom = new StreamGeometry();
        using (var g = geom.Open())
        {
            // Start at first point
            g.BeginFigure(pts[0], false);

            for (int i = 0; i < pts.Count - 1; i++)
            {
                var a = pts[i];
                var b = (i == pts.Count - 1) ? p1 : pts[i + 1];

                // If this is the last segment, end at 'tip' not exact p1
                if (i == pts.Count - 2) b = tip;

                // If there is a next-next point, we may need a rounded corner at b (except for last segment)
                if (i < pts.Count - 2)
                {
                    var c = pts[i + 2];

                    // compute unit direction vectors
                    var v1 = Normalize(b - a);
                    var v2 = Normalize(c - b);

                    // compute available lengths on adjacent segments
                    var la = Distance(a, b);
                    var lb = Distance(b, c);

                    // actual corner radius constrained by segment lengths
                    var r = Math.Min(CornerRadius, Math.Min(la / 2.0, lb / 2.0));

                    // points where straight segments should end/start (p_before, p_after)
                    var pBefore = new Point(b.X - v1.X * r, b.Y - v1.Y * r);
                    var pAfter = new Point(b.X + v2.X * r, b.Y + v2.Y * r);

                    // draw straight from current position to pBefore
                    g.LineTo(pBefore, true);

                    // draw quadratic corner (control at corner point b)
                    g.QuadraticBezierTo(b, pAfter, true);

                    // advance i by 1 (we consumed the next anchor partially)
                    // next loop will treat segment from pAfter -> c (handled as normal)
                    // to implement correctly, replace pts[i+1] with pAfter for geometry continuity
                    pts[i + 1] = pAfter;
                }
                else
                {
                    // Last simple segment (or only two points)
                    g.LineTo(b, true);
                }
            }
        }

        var pen = new Pen(isSelected ? Brushes.DodgerBlue : Brushes.Gray, isSelected ? 2 : 1);
        ctx.DrawGeometry(null, pen, geom);

        // Draw arrowhead at original p1 direction but anchored to 'tip'
        if (segLen > 1e-6)
            DrawArrowhead(ctx, tip, dir, isSelected);
    }

    private static Point Normalize(Point v)
    {
        var len = Math.Sqrt(v.X * v.X + v.Y * v.Y);
        return len > 1e-8 ? new Point(v.X / len, v.Y / len) : new Point(0, 0);
    }

    private static double Distance(Point a, Point b)
        => Math.Sqrt((a.X - b.X) * (a.X - b.X) + (a.Y - b.Y) * (a.Y - b.Y));

    private void DrawArrowhead(DrawingContext ctx, Point tip, Point dir, bool isSelected)
    {
        var len = ArrowLength;
        var spread = ArrowSpread;
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