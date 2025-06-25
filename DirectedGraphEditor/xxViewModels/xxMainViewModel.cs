using CommunityToolkit.Mvvm.ComponentModel;
using DirectedGraphCore;
using System.Collections.ObjectModel;
using System.Xml.Linq;

namespace DirectedGraphEditor.ViewModels;

public partial class xxMainViewModel : ObservableObject
{
    [ObservableProperty]
    private string _greeting = "Welcome to Avalonia!";

    [ObservableProperty]
    private int selectedTabIndex = 0;


    public ObservableCollection<xxGraphNodeViewModel> NodesVm { get; } = new();

    public GraphModel Graph { get; } = new();

    public xxMainViewModel()
    {
        // Sample nodes
        // Create node A with inputs and outputs
        var nodeA = new GraphNode("nodeA", "Alpha");
        nodeA.Inputs.Add(new GraphSlot("In1", GraphSlotDirection.Input, nodeA));
        nodeA.Outputs.Add(new GraphSlot("Out1", GraphSlotDirection.Output, nodeA));
        nodeA.Outputs.Add(new GraphSlot("Out2", GraphSlotDirection.Output, nodeA));

        // Create node B with inputs and outputs
        var nodeB = new GraphNode("nodeB", "Beta");
        nodeB.Inputs.Add(new GraphSlot("In1", GraphSlotDirection.Input, nodeB));
        nodeB.Inputs.Add(new GraphSlot("In2", GraphSlotDirection.Input, nodeB));
        nodeB.Outputs.Add(new GraphSlot("Out1", GraphSlotDirection.Output, nodeB));

        // Create ViewModels with positions
        var vmA = new xxGraphNodeViewModel(nodeA, 100, 100);
        var vmB = new xxGraphNodeViewModel(nodeB, 300, 200);

        // Add to collection
        NodesVm.Add(vmA);
        NodesVm.Add(vmB);
    }
}
