using Avalonia.Controls;
using Avalonia.Input;
using System;

namespace DirectedGraphEditor.Controls.GraphNodeControl;

public partial class GraphNodeControl : UserControl
{

    public GraphNodeControl()
    {
        InitializeComponent();
    }

    private void OnOutputSlotPressed(object? sender, PointerPressedEventArgs e)
    {
        Console.WriteLine("Start connection from this slot");
        // Optionally raise a routed event to parent
    }

    private void OnNodeBorderPressed(object? sender, PointerPressedEventArgs e)
    {
        if (DataContext is not GraphNodeViewModel node)
            return;

        Console.WriteLine($"Pressed node surface: {node.Name}");

        // Dragging might begin here, or it might be handled at the canvas level
        e.Handled = true;
    }
}
