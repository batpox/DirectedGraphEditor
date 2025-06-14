using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace DirectedGraphEditor.Controls;

public sealed partial class ProgressIndicatorView : UserControl
{
    public ProgressIndicatorView()
    {
        InitializeComponent();
    }

    void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}