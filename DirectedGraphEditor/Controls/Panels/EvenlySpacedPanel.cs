using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using System;

namespace DirectedGraphEditor.Controls.Panels;

/// <summary>Arranges children in a single column, spaced evenly vertically.
/// Child DesiredSize is honored for width; height is ignored and placed at the
/// midline of each slot.</summary>
public sealed class EvenlySpacedPanel : Panel
{
    protected override Size MeasureOverride(Size availableSize)
    {
        foreach (var child in Children)
            child.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
        return new Size(
            double.IsInfinity(availableSize.Width) ? DesiredWidth() : availableSize.Width,
            double.IsInfinity(availableSize.Height) ? DesiredHeight() : availableSize.Height);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        int n = Children.Count;
        if (n == 0) return finalSize;

        // n segments -> n+1 gaps; place each child at fraction k/(n+1)
        for (int i = 0; i < n; i++)
        {
            double frac = (i + 1.0) / (n + 1.0);
            double y = frac * finalSize.Height;
            var child = Children[i];
            var sz = child.DesiredSize;
            var rect = new Rect(
                x: 0,
                y: y - sz.Height / 2.0,
                width: finalSize.Width,
                height: sz.Height);
            child.Arrange(rect);
        }
        return finalSize;
    }

    private double DesiredWidth()
    {
        double w = 0;
        foreach (var c in Children) w = Math.Max(w, c.DesiredSize.Width);
        return w;
    }
    private double DesiredHeight() => 0; // let parent drive the height
}
