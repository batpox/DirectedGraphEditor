// Controls/NodeControl.cs
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;
using DirectedGraphCore.Models;
using System;
using System.Xml.Linq;

namespace DirectedGraphEditor.Controls;

public sealed class NodeControl : GraphElementControl
{
    public static readonly StyledProperty<string?> TitleProperty =
        AvaloniaProperty.Register<NodeControl, string?>(nameof(Title));

    public static readonly StyledProperty<double> CornerRadiusProperty =
        AvaloniaProperty.Register<NodeControl, double>(nameof(CornerRadius), 8);

    public static readonly StyledProperty<double> HeaderHeightProperty =
        AvaloniaProperty.Register<NodeControl, double>(nameof(HeaderHeight), 15);

    public static readonly StyledProperty<double> PinRadiusProperty =
        AvaloniaProperty.Register<NodeControl, double>(nameof(PinRadius), 8);

    public string? Title { get => GetValue(TitleProperty); set => SetValue(TitleProperty, value); }
    public double CornerRadius { get => GetValue(CornerRadiusProperty); set => SetValue(CornerRadiusProperty, value); }
    public double HeaderHeight { get => GetValue(HeaderHeightProperty); set => SetValue(HeaderHeightProperty, value); }
    public double PinRadius { get => GetValue(PinRadiusProperty); set => SetValue(PinRadiusProperty, value); }

    public NodeControl()
    {
        // If you want element-level events, uncomment:
        // WirePointerHandlers();
    }

    private TextLayout? _titleLayout;

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == TitleProperty)
            _titleLayout = null;
    }

    public override void Render(DrawingContext drawContext)
    {
        base.Render(drawContext);

        var rect = new Rect(Bounds.Size);
        var roundedRect = new RoundedRect(rect, CornerRadius);

        // Use darker fills for dark app backgrounds and light text for contrast
        var bodyFill = IsSelected ? Brushes.DodgerBlue : Brushes.DimGray; // dark gray
        var borderPen = new Pen(IsSelected ? Brushes.DodgerBlue : Brushes.Gray, IsSelected ? 2 : 1);

        // Body
        //var headerBrush = IsSelected ? Brushes.AliceBlue : Brushes.Gainsboro;
        drawContext.DrawRectangle(bodyFill, borderPen, roundedRect);

        // Header strip (optional)
        if (!string.IsNullOrWhiteSpace(Title))
        {
            var headerRect = new Rect(0, 0, rect.Width, Math.Min(HeaderHeight, rect.Height));
            var headerFill = IsSelected ? Brushes.MidnightBlue : Brushes.DarkSlateGray;
            drawContext.DrawRectangle(headerFill, null, headerRect);

            var layout = new TextLayout(
                text: Title!,
                typeface: Typeface.Default,
                fontSize: 10,
                foreground: Brushes.White,
                textAlignment: TextAlignment.Center,
                textWrapping: TextWrapping.NoWrap,
                maxWidth: headerRect.Width);

            var margin = 6;
            var textPt = new Point(margin, headerRect.Y + (headerRect.Height - layout.Height) / 2);
            layout.Draw(drawContext, textPt);
        }

        // Draw simple pin markers if DataContext is a GraphNode
        if (DataContext is GraphNode gnode)
        {
            RenderNodePins(drawContext, gnode);
        }

    }

    /// <summary> Draw the pins areas (from pins: left, to pins: right </summary>
    private void RenderNodePins(DrawingContext drawContext, GraphNode gnode)
    {

        Rect? referenceRect = new Rect(0, 0, gnode.Size.Value.Width, gnode.Size.Value.Height); // gnode.BoundingRect

        // area for pins at the left and right
        //var boundingRect = gnode.BoundingRect;

        var pinRectIncoming = new Rect(
            referenceRect?.X ?? 0,
            referenceRect?.Y ?? 0,
            PinRadius,
            referenceRect?.Height ?? 0);

        var pinRectOutgoing = new Rect(
            (referenceRect?.X ?? 0) + (referenceRect?.Width ?? 0) - PinRadius,
            referenceRect?.Y ?? 0,
            PinRadius,
            referenceRect?.Height ?? 0);

        var onePinWidth = PinRadius;
        var onePinHeight = PinRadius;

        const double pinRadius = 5.0;
        const double pinMargin = 6.0;

        // Inputs on left
        var inputs = gnode.Inputs.Count;
        if (inputs > 0)
        {
            var x = pinRectIncoming.X; 
            var y = pinRectIncoming.Y;

            var cx = x + onePinWidth / 2.0; // canvas x
            var fraction = 1.0 / (inputs + 1.0) * onePinHeight;

            for (int ii = 0; ii < inputs; ii++)
            {
                double frac = (ii + 1.0) / (inputs + 1.0); // fraction of total height
                y += frac * pinRectIncoming.Height;
                var cy = y;
                drawContext.DrawEllipse(Brushes.White, new Pen(Brushes.Gray, 1), new Point(cx, cy), pinRadius, pinRadius);
            }
        }

        // Outputs on right
        var outputs = gnode.Outputs.Count;
        if (outputs > 0)
        {
            var x = pinRectOutgoing.X; 
            var y = pinRectOutgoing.Y;

            var cx = x + onePinWidth / 2.0; // canvas x
            var fraction = 1.0 / (outputs + 1.0) * onePinHeight;

            for (int ii = 0; ii < outputs; ii++)
            {
                double frac = (ii + 1.0) / (outputs + 1.0);
                y += frac * pinRectOutgoing.Height;
                var cy = y;
                drawContext.DrawEllipse(Brushes.White, new Pen(Brushes.Gray, 1), new Point(cx, cy), pinRadius, pinRadius);
            }
        }


    }

    // Convenience hit-test for external controllers
    public bool Hit(Point canvasPoint, Visual? parentForTransform = null)
    {
        var pt = parentForTransform is null ? canvasPoint : parentForTransform.TranslatePoint(canvasPoint, this) ?? canvasPoint;
        return new Rect(Bounds.Size).Contains(pt);
    }
}
