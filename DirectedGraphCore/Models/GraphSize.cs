namespace DirectedGraphCore.Models;

public class GraphSize
{
    public double Width { get; set; }
    public double Height { get; set; }
    public double Depth { get; set; }
    public GraphSize() { }
    public GraphSize(double width, double height, double depth)
    {
        Width = width;
        Height = height;
        Depth = depth;
    }
}
