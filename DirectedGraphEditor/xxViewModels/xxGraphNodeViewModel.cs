namespace DirectedGraphEditor.ViewModels;

using CommunityToolkit.Mvvm.ComponentModel;
using DirectedGraphCore;
using System.Collections.Generic;

/// <summary>
/// This was an experimental stub. Look now under Pages.Editor
/// </summary>
public partial class xxGraphNodeViewModel : xxViewModelBase
{
    public GraphNode Node { get; }

    [ObservableProperty]
    private double x;

    [ObservableProperty]
    private double y;

    public xxGraphNodeViewModel(GraphNode node, double initialX, double initialY)
    {
        Node = node;
        x = initialX;
        y = initialY;
    }

    public string Id => Node.Id;
    public string Name => Node.Name;
    public IEnumerable<GraphSlot> Inputs => Node.Inputs;
    public IEnumerable<GraphSlot> Outputs => Node.Outputs;
}
