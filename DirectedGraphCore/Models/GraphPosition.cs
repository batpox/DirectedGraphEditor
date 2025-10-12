namespace DirectedGraphCore.Models;

public class GraphPosition
{
    public float X { get; set; }
    public float Y { get; set; }
    public float Z { get; set; }
    public GraphPosition() { }
    public GraphPosition(float x, float y, float z)
    {
        X = x;
        Y = y;
        Z = z;
    }
}
