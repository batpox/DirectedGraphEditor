// Controls/EdgeControl.cs
using Avalonia;
using Avalonia.Media;
using DirectedGraphEditor.Rendering;
using DirectedGraphEditor.Services;
using System;

namespace DirectedGraphEditor.Controls;

public enum EdgeStyle
{
    Linear,
    QuadraticBezier,
    RoutedBezier,
    RoutedLinear
}

public sealed class EdgeControl : GraphElementControl
{
    public static readonly StyledProperty<Point> SourcePointProperty =
        AvaloniaProperty.Register<EdgeControl, Point>(nameof(SourcePoint));
    public static readonly StyledProperty<Point> TargetPointProperty =
        AvaloniaProperty.Register<EdgeControl, Point>(nameof(TargetPoint));
    public static readonly StyledProperty<bool> ShowEndpointHandlesProperty =
        AvaloniaProperty.Register<EdgeControl, bool>(nameof(ShowEndpointHandles), true);
    public static readonly StyledProperty<EdgeStyle> EdgeStyleProperty =
        AvaloniaProperty.Register<EdgeControl, EdgeStyle>(nameof(EdgeStyle), EdgeStyle.Linear);
    // (replace earlier EdgeControl.Render call with this snippet and add property)
    public static readonly StyledProperty<Point[]?> ControlPointsProperty =
        AvaloniaProperty.Register<EdgeControl, Point[]?>(nameof(ControlPoints), defaultValue: null);

    public Point[]? ControlPoints { get => GetValue(ControlPointsProperty); set => SetValue(ControlPointsProperty, value); }

    public Point SourcePoint { get => GetValue(SourcePointProperty); set => SetValue(SourcePointProperty, value); }
    public Point TargetPoint { get => GetValue(TargetPointProperty); set => SetValue(TargetPointProperty, value); }
    public bool ShowEndpointHandles { get => GetValue(ShowEndpointHandlesProperty); set => SetValue(ShowEndpointHandlesProperty, value); }

    public IEdgeRenderer Renderer { get; set; } = new LinearEdgeRenderer();

    // Expose style property for easy switching
    public EdgeStyle EdgeStyle
    {
        get => GetValue(EdgeStyleProperty);
        set => SetValue(EdgeStyleProperty, value);
    }

    public override void Render(DrawingContext ctx)
    {
        base.Render(ctx);
        Renderer.Render(ctx, SourcePoint, TargetPoint, ControlPoints, IsSelected);

        if (ShowEndpointHandles && IsSelected)
        {
            var fill = Brushes.White;
            var pen = new Pen(Brushes.DodgerBlue, 1);
            ctx.DrawEllipse(fill, pen, SourcePoint, HandleRadius, HandleRadius);
            ctx.DrawEllipse(fill, pen, TargetPoint, HandleRadius, HandleRadius);
        }
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == EdgeStyleProperty)
        {
            EnsureRenderer();
            InvalidateVisual();
        }
    }

    private void EnsureRenderer()
    {
        switch (EdgeStyle)
        {
            case EdgeStyle.QuadraticBezier:
                Renderer = new QuadraticBezierEdgeRenderer();
                break;
            case EdgeStyle.RoutedBezier:
                Renderer = new RoutedBezierEdgeRenderer();
                break;
            case EdgeStyle.RoutedLinear:
                Renderer = new RoutedLinearEdgeRenderer();
                break;
            default:
                Renderer = new LinearEdgeRenderer();
                break;
        }
    }
    // Handles: visual only — DragController does the logic.
    private const double HandleRadius = 5;


    public EdgeControl()
    {
        // Ensure renderer matches initial EdgeStyle
        EnsureRenderer();

        // Initialize from global setting
        this.EdgeStyle = EditorSettings.DefaultEdgeStyle;

        // Optional: update when global setting changes
        EditorSettings.EdgeStyleChanged += s =>
        {
            // apply only if control still alive
            this.EdgeStyle = s;
        };
    }


    public IEdgeRenderer RendererImpl
    {
        get => Renderer;
        set
        {
            Renderer = value ?? throw new ArgumentNullException(nameof(value));
            InvalidateVisual();
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
