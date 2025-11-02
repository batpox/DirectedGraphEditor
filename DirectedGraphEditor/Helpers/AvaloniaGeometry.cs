// DirectedGraphEditor/Adapters/GeometryExtensions.cs
using Avalonia;                           // Point, Vector (double-based)
using DirectedGraphCore.Geometry;         // Point3, Vector3 (float-based)

namespace DirectedGraphEditor.Helpers;

public static class GeometryExtensions
{
    // ───────────── Avalonia → Core (float) ─────────────

    /// <summary>Convert Avalonia Point (double) to Core Point3 (float). Default z=0.</summary>
    public static Point3 ToPoint3(this Point p)
        => new Point3(x: (float)p.X, y: (float)p.Y, z: 0f);

    /// <summary>Convert Avalonia Vector (double) to Core Vector3 (float). Default z=0.</summary>
    public static Vector3 ToVector3(this Vector v)
        => new Vector3(x: (float)v.X, y: (float)v.Y, z: 0f);

    // ───────────── Core (float) → Avalonia (double) ─────────────

    /// <summary>Convert Core Point3 (float) to Avalonia Point (drops z).</summary>
    public static Point ToAvaloniaPoint(this Point3 p)
        => new Point(x: p.X, y: p.Y);

    /// <summary>Convert Core Vector3 (float) to Avalonia Vector (drops z).</summary>
    public static Vector ToAvaloniaVector(this Vector3 v)
        => new Vector(x: v.X, y: v.Y);
}
