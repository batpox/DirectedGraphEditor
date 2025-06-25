using CommunityToolkit.Mvvm.ComponentModel;
using DirectedGraphEditor.Controls.GraphNodeControl;
using Avalonia;
using System.ComponentModel;

namespace DirectedGraphEditor.Pages.Editor;

public sealed class GraphEdgeViewModel : ObservableObject
{
    public GraphNodeViewModel Source { get; }
    public GraphNodeViewModel Target { get; }

    public GraphEdgeViewModel(GraphNodeViewModel source, GraphNodeViewModel target)
    {
        Source = source;
        Target = target;

        Source.PropertyChanged += OnPositionChanged;
        Target.PropertyChanged += OnPositionChanged;
    }

    private void OnPositionChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(Source.X) or nameof(Source.Y) or
            nameof(Target.X) or nameof(Target.Y))
        {
            OnPropertyChanged(nameof(StartPoint));
            OnPropertyChanged(nameof(EndPoint));
        }
    }

    public Point StartPoint => new(Source.X + 60, Source.Y + 30); // offset to center right
    public Point EndPoint => new(Target.X + 0, Target.Y + 30); // offset to center left
}
