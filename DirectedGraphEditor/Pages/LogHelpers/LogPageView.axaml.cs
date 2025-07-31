using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace DirectedGraphEditor.Pages.Log;

public sealed partial class LogPageView : UserControl
{
    public LogPageView()
    {
        InitializeComponent();
    }

    void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}