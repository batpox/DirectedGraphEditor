// Controls/GraphElementControl.cs
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace DirectedGraphEditor.Controls;

public abstract class GraphElementControl : Control
{
    public static readonly StyledProperty<bool> IsSelectedProperty =
        AvaloniaProperty.Register<GraphElementControl, bool>(nameof(IsSelected));

    public bool IsSelected
    {
        get => GetValue(IsSelectedProperty);
        set => SetValue(IsSelectedProperty, value);
    }

    protected GraphElementControl()
    {
        Focusable = true;
        IsHitTestVisible = true;
    }

    // Optional helpers if you ever want element-level pointer handling.
    protected void WirePointerHandlers()
    {
        AddHandler(PointerPressedEvent, OnPointerPressed, RoutingStrategies.Tunnel | RoutingStrategies.Bubble);
        AddHandler(PointerReleasedEvent, OnPointerReleased, RoutingStrategies.Tunnel | RoutingStrategies.Bubble);
        AddHandler(PointerMovedEvent, OnPointerMoved, RoutingStrategies.Tunnel | RoutingStrategies.Bubble);
    }

    protected virtual void OnPointerPressed(object? s, PointerPressedEventArgs e) { }
    protected virtual void OnPointerReleased(object? s, PointerReleasedEventArgs e) { }
    protected virtual void OnPointerMoved(object? s, PointerEventArgs e) { }
}
