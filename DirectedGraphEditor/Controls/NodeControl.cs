// Controls/NodeControl.cs
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;
using System;

namespace DirectedGraphEditor.Controls;

public sealed class NodeControl : GraphElementControl
{
    public static readonly StyledProperty<string?> TitleProperty =
        AvaloniaProperty.Register<NodeControl, string?>(nameof(Title));

    public static readonly StyledProperty<double> CornerRadiusProperty =
        AvaloniaProperty.Register<NodeControl, double>(nameof(CornerRadius), 8);

    public static readonly StyledProperty<double> HeaderHeightProperty =
        AvaloniaProperty.Register<NodeControl, double>(nameof(HeaderHeight), 22);

    public string? Title { get => GetValue(TitleProperty); set => SetValue(TitleProperty, value); }
    public double CornerRadius { get => GetValue(CornerRadiusProperty); set => SetValue(CornerRadiusProperty, value); }
    public double HeaderHeight { get => GetValue(HeaderHeightProperty); set => SetValue(HeaderHeightProperty, value); }

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

    public override void Render(DrawingContext ctx)
    {
        base.Render(ctx);

        var r = new Rect(Bounds.Size);
        var rr = new RoundedRect(r, CornerRadius);

        // Use darker fills for dark app backgrounds and light text for contrast
        var fill = IsSelected ? Brushes.DodgerBlue : Brushes.DarkGray; // dark gray
        var border = new Pen(IsSelected ? Brushes.DodgerBlue : Brushes.Gray, IsSelected ? 2 : 1);


        // Body
        var headerBrush = IsSelected ? Brushes.AliceBlue : Brushes.Gainsboro;
        ctx.DrawRectangle(fill, border, rr);

        // Header strip (optional)
        if (!string.IsNullOrWhiteSpace(Title))
        {
            var headerRect = new Rect(0, 0, r.Width, Math.Min(HeaderHeight, r.Height));
            var headerFill = IsSelected ? Brushes.MidnightBlue : Brushes.DarkSlateGray;
            ctx.DrawRectangle(IsSelected ? Brushes.AliceBlue : Brushes.Gainsboro, null, headerRect);

            var layout = new TextLayout(
                text: Title!,
                typeface: Typeface.Default,
                fontSize: 12,
                foreground: Brushes.White,
                textAlignment: TextAlignment.Left,
                textWrapping: TextWrapping.NoWrap,
                maxWidth: headerRect.Width);
////                maxHeight: headerRect.Height);

            var margin = 6;
            var textPt = new Point(margin, headerRect.Y + (headerRect.Height - layout.Height) / 2);
            layout.Draw(ctx, textPt);
        }
    }

    // Convenience hit-test for external controllers
    public bool Hit(Point canvasPoint, Visual? parentForTransform = null)
    {
        var pt = parentForTransform is null ? canvasPoint : parentForTransform.TranslatePoint(canvasPoint, this) ?? canvasPoint;
        return new Rect(Bounds.Size).Contains(pt);
    }
}
