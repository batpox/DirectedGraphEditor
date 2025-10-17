using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Media;
using System;

namespace DirectedGraphEditor.Controls.GraphNodeControl;

public partial class GraphNodeControl : UserControl
{
    public Ellipse sourcePin => this.FindControl<Ellipse>("SourcePin");
    public Ellipse targetPin => this.FindControl<Ellipse>("TargetPin");

    public GraphNodeControl()
    {
        InitializeComponent();

        // Optional: hover effects
        PointerEntered += (_, _) => NodeBody.Background = Brushes.LightSteelBlue;
        PointerExited += (_, _) => NodeBody.Background = Brushes.LightGray;
    }

    public static readonly StyledProperty<bool> IsSelectedProperty =
            AvaloniaProperty.Register<GraphNodeControl, bool>(
                nameof(IsSelected), defaultValue: false);

    public bool IsSelected
    {
        get => GetValue(IsSelectedProperty);
        set => SetValue(IsSelectedProperty, value);
    }



    private void OnOutputPinPressed(object? sender, PointerPressedEventArgs e)
    {
        Console.WriteLine("Start connection from this pin");
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
