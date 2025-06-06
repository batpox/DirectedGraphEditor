using Avalonia;
using Avalonia.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DirectedGraphEditor.Helpers;

public static class HitTestHelper
{
    public static bool IsPointInTransformedBounds(Matrix transform, Rect bounds, Point point)
    {
        var topLeft = transform.Transform(bounds.TopLeft);
        var topRight = transform.Transform(bounds.TopRight);
        var bottomLeft = transform.Transform(bounds.BottomLeft);
        var bottomRight = transform.Transform(bounds.BottomRight);

        var minX = Math.Min(Math.Min(topLeft.X, topRight.X), Math.Min(bottomLeft.X, bottomRight.X));
        var maxX = Math.Max(Math.Max(topLeft.X, topRight.X), Math.Max(bottomLeft.X, bottomRight.X));
        var minY = Math.Min(Math.Min(topLeft.Y, topRight.Y), Math.Min(bottomLeft.Y, bottomRight.Y));
        var maxY = Math.Max(Math.Max(topLeft.Y, topRight.Y), Math.Max(bottomLeft.Y, bottomRight.Y));

        return point.X >= minX && point.X <= maxX &&
               point.Y >= minY && point.Y <= maxY;
    }

    public static bool IsPointerOver(Control target, Visual relativeTo, Point pointerPosition)
    {
        var transform = target.TransformToVisual(relativeTo);
        if (transform == null)
            return false;

        var transformedBounds = target.Bounds.TransformToAABB(transform.Value);
        return transformedBounds.Contains(pointerPosition);
    }
}
