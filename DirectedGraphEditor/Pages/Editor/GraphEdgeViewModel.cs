using CommunityToolkit.Mvvm.ComponentModel;
using DirectedGraphEditor.Controls.GraphNodeControl;
using Avalonia;
using System.ComponentModel;

namespace DirectedGraphEditor.Pages.Editor;

public sealed class GraphEdgeViewModel : ObservableObject
{
    public GraphEdge Edge { get; } // link to model edge

    public GraphNodeViewModel SourceNode { get; set; }
    public GraphNodeViewModel TargetNode { get; set; }
    public int SourceOutputIndex { get; set; }
    public int TargetInputIndex { get; set; }

    public bool IsHighlighted { get; set; } = false;  
    
    // Node visuals: width=120, ellipse size=10, outputs at x=120-6, inputs at x=-4
    private const double NodeWidth = 120;
    private const double EllW = 10;
    private const double EllH = 10;
    private const double RowStartY = 8;
    private const double RowPitch = 12;

    public GraphEdgeViewModel(
        GraphEdge edge,
        GraphNodeViewModel source, int sourceOutputIndex,
        GraphNodeViewModel target, int targetInputIndex )
    {
        Edge = edge;
        SourceNode = source;
        TargetNode = target;
        SourceOutputIndex = sourceOutputIndex;
        TargetInputIndex = targetInputIndex;

        SubscribeToNodes();
    }

    private void SubscribeToNodes()
    {
        SourceNode.PropertyChanged += OnPositionChanged;
        TargetNode.PropertyChanged += OnPositionChanged;
    }

    private void OnPositionChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(SourceNode.X) or nameof(SourceNode.Y) 
            or nameof(TargetNode.X) or nameof(TargetNode.Y))
        {
            OnPropertyChanged(nameof(StartPoint));
            OnPropertyChanged(nameof(EndPoint));
        }
    }

    public void UpdateSource(GraphNodeViewModel newSource, int newPinIndex)
    {
        SourceNode.PropertyChanged -= OnPositionChanged;
        SourceNode = newSource;
        SourceOutputIndex = newPinIndex;
        SubscribeToNodes();
        NotifyEndpointsChanged();
    }

    public void UpdateTarget(GraphNodeViewModel newTarget, int newPinIndex)
    {
        TargetNode.PropertyChanged -= OnPositionChanged;
        TargetNode = newTarget;
        TargetInputIndex = newPinIndex;
        SubscribeToNodes();
        NotifyEndpointsChanged();
    }

    public void NotifyEndpointsChanged()
    {
        OnPropertyChanged(nameof(StartPoint));
        OnPropertyChanged(nameof(EndPoint));
    }

    // Center of output ellipse (right edge)
    public Point StartPoint
        => new(
    SourceNode.X + SourceNode.Style.OutputXOffset + SourceNode.Style.PinRadius,
    SourceNode.Y + SourceNode.Style.PinYOffset 
            + SourceOutputIndex * SourceNode.Style.PinYPitch + SourceNode.Style.PinRadius);


    // Center of input ellipse (left edge)
    public Point EndPoint
        => new(
    TargetNode.X + TargetNode.Style.InputXOffset + TargetNode.Style.PinRadius,
    TargetNode.Y + TargetNode.Style.PinYOffset 
            + TargetInputIndex * TargetNode.Style.PinYPitch + TargetNode.Style.PinRadius);

}
