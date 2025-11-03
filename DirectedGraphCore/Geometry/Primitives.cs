using System.Numerics;

namespace DirectedGraphCore.Geometry;

public readonly struct Point2
{
    public float X { get; }
    public float Y { get; }
    public Point2(float x, float y) { X = x; Y = y; }

    public static Point2 operator +(Point2 a, Vector2 b) => new(x: a.X + b.X, y: a.Y + b.Y);
    public static Vector2 operator -(Point2 a, Point2 b) => new(x: a.X - b.X, y: a.Y - b.Y);

    public void Deconstruct(out float x, out float y) { x = X; y = Y; }
    public override string ToString() => $"({X:0.###}, {Y:0.###})";
}
public readonly struct Point3
{
    public float X { get; }
    public float Y { get; }
    public float Z { get; }
    public Point3(float x, float y, float z = 0) { X = x; Y = y; Z = z; }

    public static Point3 operator +(Point3 a, Vector3 b) => new(x: a.X + b.X, y: a.Y + b.Y, z: a.Z + b.Z);
    public static Vector3 operator -(Point3 a, Point3 b) => new(x: a.X - b.X, y: a.Y - b.Y, z: a.Z - b.Z);

    public void Deconstruct(out float x, out float y, out float z) { x = X; y = Y; z = Z; }
    public override string ToString() => $"({X:0.###}, {Y:0.###}, {Z:0.###})";
}


public readonly struct Vector2
{
    public float X { get; }
    public float Y { get; }
    public Vector2(float x, float y) { X = x; Y = y; }

    public float Length => (float) System.Math.Sqrt(X * X + Y * Y);

    public static Vector2 operator +(Vector2 a, Vector2 b) => new(x: a.X + b.X, y: a.Y + b.Y);
    public static Vector2 operator -(Vector2 a, Vector2 b) => new(x: a.X - b.X, y: a.Y - b.Y);
    public static Vector2 operator *(Vector2 a, float k) => new(x: a.X * k, y: a.Y * k);
    public static Vector2 operator /(Vector2 a, float k) => new(x: a.X / k, y: a.Y / k);
    public override string ToString() => $"<{X:0.###}, {Y:0.###}>";
}
public readonly struct Vector3
{
    public float X { get; }
    public float Y { get; }
    public float Z { get; }
    public Vector3(float x, float y, float z = 0) { X = x; Y = y; Z = z; }

    public float Length => (float) System.Math.Sqrt(X*X + Y*Y + Z*Z );

    public static Vector3 operator +(Vector3 a, Vector3 b) => new(x: a.X + b.X, y: a.Y + b.Y, z: a.Z + b.Z);
    public static Vector3 operator -(Vector3 a, Vector3 b) => new(x: a.X - b.X, y: a.Y - b.Y, z: a.Z + b.Z);
    public static Vector3 operator *(Vector3 a, float k) => new(x: a.X * k, y: a.Y * k, z: a.Z * k);
    public static Vector3 operator /(Vector3 a, float k) => new(x: a.X / k, y: a.Y / k, z: a.Z / k);
    public override string ToString() => $"<{X:0.###}, {Y:0.###}, {Z:0.###}>";
}

public readonly struct Size2
{
    public float Width { get; }
    public float Height { get; }
    public Size2(float width, float height) { Width = width; Height = height; }
    public void Deconstruct(out float w, out float h) { w = Width; h = Height; }
    public override string ToString() => $"{Width:0.###}×{Height:0.###}";
}
public readonly struct Size3
{
    public float Width { get; }
    public float Height { get; }
    public float Depth { get; }
    public Size3(float width, float height, float depth = 0) { Width = width; Height = height; Depth = depth; }
    public void Deconstruct(out float w, out float h, out float d ) { w = Width; h = Height; d = Depth; }
    public override string ToString() => $"{Width:0.###}×{Height:0.###}×{Depth:0.###}";
}
