using Avalonia;
using System.Drawing;
using System.Numerics;

public static class PointExtensions
{
    public static PointF ToPointF(this Avalonia.Point p)
        => new PointF((float)p.X, (float)p.Y);

    public static Vector2 ToVector2(this Avalonia.Point p)
        => new Vector2((float)p.X, (float)p.Y);
}
