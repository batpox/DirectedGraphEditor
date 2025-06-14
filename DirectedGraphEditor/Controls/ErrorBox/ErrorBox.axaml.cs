using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

namespace DirectedGraphEditor.Controls;

public sealed partial class ErrorBox : UserControl
{
    public static readonly StyledProperty<string> MessageProperty = AvaloniaProperty.Register<ErrorBox, string>(nameof(Message));

    public ErrorBox()
    {
        InitializeComponent();

        if (DataContext is ErrorBoxViewModel vm)
        {
            vm.CopyRequested += async () =>
            {
                // Grab the IClipboard from the current TopLevel
                var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
                if (clipboard is { } && !string.IsNullOrWhiteSpace(vm.Message))
                {
                    await clipboard.SetTextAsync($"{vm.Message}\n\n{vm.Exception}");
                }
            };
        }
    }

    public event EventHandler? Closed;

    public string Message
    {
        get => GetValue(MessageProperty);
        set => SetValue(MessageProperty, value);
    }

    void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    void OnButtonCloseClicked(object? sender, RoutedEventArgs e)
    {
        Closed?.Invoke(this, EventArgs.Empty);
    }

}