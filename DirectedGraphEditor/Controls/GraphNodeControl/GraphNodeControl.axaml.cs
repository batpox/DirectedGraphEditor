using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.VisualTree;
using DirectedGraphCore.Models;
using System;
using System.Linq;

namespace DirectedGraphEditor.Controls.GraphNodeControl;

public partial class GraphNodeControl : UserControl
{
    // Public events the adapter subscribes to:
    public event EventHandler<PinEventArgs>? PinDown;
    public event EventHandler<PinEventArgs>? PinDrag;
    public event EventHandler<PinEventArgs>? PinUp;

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

    private void OnPinPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (!TryGetPinFromSender(sender, out var node, out var pin, out var localCenter, out var canvasCenter))
            return;

        PinDown?.Invoke(this, new PinEventArgs(node, pin, localCenter, canvasCenter));
        e.Handled = true;
    }

    private void OnPinPointerMoved(object? sender, PointerEventArgs e)
    {
        if (!TryGetPinFromSender(sender, out var node, out var pin, out var localCenter, out var canvasCenter))
            return;

        PinDrag?.Invoke(this, new PinEventArgs(node, pin, localCenter, canvasCenter));
    }

    private void OnPinPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (!TryGetPinFromSender(sender, out var node, out var pin, out var localCenter, out var canvasCenter))
            return;

        PinUp?.Invoke(this, new PinEventArgs(node, pin, localCenter, canvasCenter));
        e.Handled = true;
    }

    // ---- Utilities ----
    // NOTE: use sender (the Ellipse), not e.Source
    private bool TryGetPinFromSender(object? sender,
                                     out GraphNode node,
                                     out NodePin pin,
                                     out Point localCenter,
                                     out Point? canvasCenter)
    {
        node = null!;
        pin = null!;
        localCenter = default;
        canvasCenter = null;

        if (sender is not Ellipse ellipse) return false;
        if (ellipse.Tag is not NodePin taggedPin) return false;
        if (DataContext is not GraphNode nodeCtx) return false;

        pin = taggedPin;
        node = nodeCtx;

        // center of ellipse in control space
        localCenter = ellipse.TranslatePoint(
            new Point(ellipse.Bounds.Width / 2, ellipse.Bounds.Height / 2),
            this
        ) ?? default;

        // try convert to canvas space
        var canvas = this.GetVisualAncestors().OfType<Canvas>().FirstOrDefault();
        if (canvas != null)
            canvasCenter = this.TranslatePoint(localCenter, canvas);

        return true;
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
