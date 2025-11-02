namespace DirectedGraphCore.Models;

public class xxGraphSize
{
    public double Width { get; set; }
    public double Height { get; set; }
    public double Depth { get; set; }
    public xxGraphSize() { }
    public xxGraphSize(double width, double height, double depth)
    {
        Width = width;
        Height = height;
        Depth = depth;
    }
}
