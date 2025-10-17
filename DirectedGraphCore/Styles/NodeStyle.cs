using System;
using System.Drawing;

public class NodeStyle
{
    public double Width { get; set; } = 120;
    public double Height { get; set; } = 60; // if needed
    public double PinRadius { get; set; } = 5;
    public double PinYOffset { get; set; } = 8;
    public double PinYPitch { get; set; } = 12;
    public double InputXOffset { get; set; } = -4;
    public double OutputXOffset { get; set; } = 114; // 120 - 6
    public Color FillColor { get; set; } = Color.SlateGray;
    public Color BorderColor { get; set; } = Color.Black;
}
