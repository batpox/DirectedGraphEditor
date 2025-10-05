namespace DirectedGraphCore.DirectedGraph;

public class GraphPosition
{
    public double X { get; set; }
    public double Y { get; set; }
    public double Z { get; set; }
    public GraphPosition() { }
    public GraphPosition(double x, double y, double z)
    {
        X = x;
        Y = y;
        Z = z;
    }
}
