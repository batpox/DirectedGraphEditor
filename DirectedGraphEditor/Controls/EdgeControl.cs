// Controls/EdgeControl.cs
using Avalonia;
using Avalonia.Media;
using DirectedGraphEditor.Rendering;
using System;

namespace DirectedGraphEditor.Controls;

public sealed class EdgeControl : GraphElementControl
{
    public static readonly StyledProperty<Point> SourcePointProperty =
        AvaloniaProperty.Register<EdgeControl, Point>(nameof(SourcePoint));
    public static readonly StyledProperty<Point> TargetPointProperty =
        AvaloniaProperty.Register<EdgeControl, Point>(nameof(TargetPoint));
    public static readonly StyledProperty<bool> ShowEndpointHandlesProperty =
        AvaloniaProperty.Register<EdgeControl, bool>(nameof(ShowEndpointHandles), true);

    public Point SourcePoint { get => GetValue(SourcePointProperty); set => SetValue(SourcePointProperty, value); }
    public Point TargetPoint { get => GetValue(TargetPointProperty); set => SetValue(TargetPointProperty, value); }
    public bool ShowEndpointHandles { get => GetValue(ShowEndpointHandlesProperty); set => SetValue(ShowEndpointHandlesProperty, value); }

    public IEdgeRenderer Renderer { get; set; } = new LinearEdgeRenderer();

    // Handles: visual only — DragController does the logic.
    private const double HandleRadius = 5;

    public override void Render(DrawingContext ctx)
    {
        base.Render(ctx);
        Renderer.Render(ctx, SourcePoint, TargetPoint, IsSelected);

        if (ShowEndpointHandles && IsSelected)
        {
            var fill = Brushes.White;
            var pen = new Pen(Brushes.DodgerBlue, 1);
            ctx.DrawEllipse(fill, pen, SourcePoint, HandleRadius, HandleRadius);
            ctx.DrawEllipse(fill, pen, TargetPoint, HandleRadius, HandleRadius);
        }
    }

    // Utility for endpoint hit tests (used by IHitTester)
    public bool HitEndpoint(Point canvasPoint, double hitRadius, out bool isTargetEnd)
    {
        isTargetEnd = false;
        Vector sourceVector = canvasPoint - SourcePoint;
        if (sourceVector.Length <= hitRadius) 
            return true;
        Vector targetVector = canvasPoint - TargetPoint;
        if (targetVector.Length <= hitRadius) 
        { 
            isTargetEnd = true; 
            return true; 
        }
        return false;
    }

    // Utility for “click edge” selection (if you need it)
    public bool HitNearSegment(Point p, double maxDist)
    {
        var a = SourcePoint; var b = TargetPoint;
        Vector ap = p - a; 
        Vector ab = b - a;
        var ab2 = ab.X * ab.X + ab.Y * ab.Y;
        if (ab2 < 1e-6) 
            return ap.Length <= maxDist;

        var t = Math.Clamp((ap.X * ab.X + ap.Y * ab.Y) / ab2, 0, 1);
        var proj = new Point(a.X + t * ab.X, a.Y + t * ab.Y);
        Vector vector = p - proj;
        return vector.Length <= maxDist;
    }
}
