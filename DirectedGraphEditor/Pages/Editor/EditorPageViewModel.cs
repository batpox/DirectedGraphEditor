using CommunityToolkit.Mvvm.ComponentModel;
using DirectedGraphCore;
using DirectedGraphEditor.Common;
using DirectedGraphEditor.Controls.GraphNodeControl;
using System.Collections.ObjectModel;
using System.Diagnostics;


namespace DirectedGraphEditor.Pages.Editor;

public partial class EditorPageViewModel : BasePageViewModel
{

    public GraphModel Graph { get; } = new();

    public ObservableCollection<GraphNodeViewModel> NodesVm { get; } = new();
    public ObservableCollection<GraphEdgeViewModel> Edges { get; } = new();

    public GraphNode Node { get; }


    public EditorPageViewModel() { }

    public override string Name => "EditorPage";


    [ObservableProperty]
    private int selectedTabIndex = 0;


    public bool CreateNodeData()
    {
        if (NodesVm.Count > 0)
            return false;

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
        var vmA = new GraphNodeViewModel(nodeA, 100, 100);
        var vmB = new GraphNodeViewModel(nodeB, 300, 200);

        // Add to collection
        NodesVm.Add(vmA);
        NodesVm.Add(vmB);

        return true;
    }

    static void Launch(string fileName)
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = fileName,
            UseShellExecute = true
        });
    }
}