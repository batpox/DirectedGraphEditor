using CommunityToolkit.Mvvm.ComponentModel;
using DirectedGraphCore.DirectedGraph;
using DirectedGraphEditor.Common;
using DirectedGraphEditor.Controls.GraphNodeControl;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Xml.Linq;


namespace DirectedGraphEditor.Pages.Editor;

public partial class EditorPageViewModel : BasePageViewModel
{

    public GraphModel Graph { get; set; } = null;

    public ObservableCollection<GraphNodeViewModel> NodesVm { get; } = new();
    public ObservableCollection<GraphEdgeViewModel> Edges { get; } = new();

    //public GraphNode Node { get; }

    public EditorPageViewModel() { }

    public override string Name => "EditorPage";

    private GraphSlot? _activeSlot;
    private int _selectedEdgeIndex = -1;
    private readonly Dictionary<GraphSlot, List<GraphEdgeViewModel>> _edgesBySlot = new();


    [ObservableProperty]
    private int selectedTabIndex = 0;


    ////public bool ReadNodeData()
    ////{
    ////    if (NodesVm.Count > 0)
    ////        return false;

    ////    // Sample nodes
    ////    // Create node A with inputs and outputs
    ////    var nodeA = new GraphNode("nodeA", "Alpha");
    ////    nodeA.Inputs.Add(new GraphSlot("In1", GraphSlotDirection.Input, nodeA));
    ////    nodeA.Outputs.Add(new GraphSlot("Out1", GraphSlotDirection.Output, nodeA));
    ////    nodeA.Outputs.Add(new GraphSlot("Out2", GraphSlotDirection.Output, nodeA));

    ////    // Create node B with inputs and outputs
    ////    var nodeB = new GraphNode("nodeB", "Beta");
    ////    nodeB.Inputs.Add(new GraphSlot("In1", GraphSlotDirection.Input, nodeB));
    ////    nodeB.Inputs.Add(new GraphSlot("In2", GraphSlotDirection.Input, nodeB));
    ////    nodeB.Outputs.Add(new GraphSlot("Out1", GraphSlotDirection.Output, nodeB));

    ////    // Create ViewModels with positions
    ////    var vmA = new GraphNodeViewModel(nodeA, 100, 100);
    ////    var vmB = new GraphNodeViewModel(nodeB, 300, 200);

    ////    // Add to collection
    ////    NodesVm.Add(vmA);
    ////    NodesVm.Add(vmB);

    ////    return true;
    ////}



    ////public bool CreateNodeData()
    ////{
    ////    if (NodesVm.Count > 0)
    ////        return false;

    ////    // Sample nodes
    ////    // Create node A with inputs and outputs
    ////    var nodeA = new GraphNode("nodeA", "Alpha");
    ////    nodeA.Inputs.Add(new GraphSlot("In1", GraphSlotDirection.Input, nodeA));
    ////    nodeA.Outputs.Add(new GraphSlot("Out1", GraphSlotDirection.Output, nodeA));
    ////    nodeA.Outputs.Add(new GraphSlot("Out2", GraphSlotDirection.Output, nodeA));

    ////    // Create node B with inputs and outputs
    ////    var nodeB = new GraphNode("nodeB", "Beta");
    ////    nodeB.Inputs.Add(new GraphSlot("In1", GraphSlotDirection.Input, nodeB));
    ////    nodeB.Inputs.Add(new GraphSlot("In2", GraphSlotDirection.Input, nodeB));
    ////    nodeB.Outputs.Add(new GraphSlot("Out1", GraphSlotDirection.Output, nodeB));

    ////    // Create ViewModels with positions
    ////    var vmA = new GraphNodeViewModel(nodeA, 100, 100);
    ////    var vmB = new GraphNodeViewModel(nodeB, 300, 200);

    ////    // Add to collection
    ////    NodesVm.Add(vmA);
    ////    NodesVm.Add(vmB);

    ////    return true;
    ////}

    public bool LoadFromFiles(string basePath)
    {
        NodesVm.Clear();
        Edges.Clear();

        Graph = GraphModel.LoadFromFile(basePath); // uses DgmlSerializer
        if (Graph == null)
            return false;

        // Build node VMs
        foreach (var node in Graph.Nodes.Values)
        {
            var nodeVm = new GraphNodeViewModel(node);
            NodesVm.Add(nodeVm);
        }

        // Build edge VMs
        foreach (var edge in Graph.Edges.Values)
        {
            var sourceVm = NodesVm.First(n => n.Node.Id == edge.SourceNodeId);
            var targetVm = NodesVm.First(n => n.Node.Id == edge.TargetNodeId);

            // Map slot names to indices
            var srcIndex = edge.SourceSlotIndex;
            var tgtIndex = edge.TargetSlotIndex;

            if (srcIndex < 0) srcIndex = 0;
            if (tgtIndex < 0) tgtIndex = 0;

            var edgeVm = new GraphEdgeViewModel(sourceVm, srcIndex, targetVm, tgtIndex);
            Edges.Add(edgeVm);
        }


        ////    // Step 1: build base
        ////    var model = GraphModel.LoadFromFile(basePath); // uses DgmlSerializer

        ////    // Step 2: merge layout if exists
        ////    var layoutPath = Path.ChangeExtension(basePath, "dgml-layout");
        ////    if (File.Exists(layoutPath))
        ////    {
        ////        var layoutDoc = XDocument.Load(layoutPath);
        ////        XNamespace ns = "http://schemas.microsoft.com/vs/2009/dgml";

        ////        var nodes = layoutDoc.Root?.Element(ns + "Nodes");
        ////        if (nodes != null)
        ////        {
        ////            foreach (var node in nodes.Elements(ns + "Node"))
        ////            {
        ////                var id = node.Attribute("Id")?.Value;
        ////                if (id != null && model.Nodes.TryGetValue(id, out var gnode))
        ////                {
        ////                    var posAttr = node.Attribute("Position")?.Value;
        ////                    var sizeAttr = node.Attribute("Size")?.Value;

        ////                    if (!string.IsNullOrEmpty(posAttr))
        ////                    {
        ////                        var parts = posAttr.Split(',');
        ////                        if (parts.Length >= 2)
        ////                        {
        ////                            gnode.Position = new GraphPosition(
        ////                                double.Parse(parts[0]),
        ////                                double.Parse(parts[1]),
        ////                                parts.Length >= 3 ? double.Parse(parts[2]) : 0);
        ////                        }
        ////                    }
        ////                    // Size attribute. Size="width,height[,depth]"
        ////                    if (!string.IsNullOrEmpty(sizeAttr))
        ////                    {
        ////                        var parts = sizeAttr.Split(',');
        ////                        if (parts.Length >= 2)
        ////                        {
        ////                            gnode.Size = new GraphSize(
        ////                                double.Parse(parts[0]),
        ////                                double.Parse(parts[1]),
        ////                                parts.Length >= 3 ? double.Parse(parts[2]) : 0);
        ////                        }
        ////                    }

        ////                    // Look for <Slot> elements under the node element
        ////                    var slotElems = node.Elements(ns + "Slot").ToList();
        ////                    if (slotElems.Count > 0)
        ////                    {
        ////                        foreach (var s in slotElems)
        ////                        {
        ////                            var name = s.Attribute("Name")?.Value ?? "Slot";
        ////                            var dirStr = s.Attribute("Direction")?.Value ?? "Input";
        ////                            var dir = dirStr.Equals("Output", StringComparison.OrdinalIgnoreCase)
        ////                                ? GraphSlotDirection.Output
        ////                                : GraphSlotDirection.Input;
        ////                            if (dir == GraphSlotDirection.Input)
        ////                                gnode.Inputs.Add(new GraphSlot(name, dir, gnode));
        ////                            else
        ////                                gnode.Outputs.Add(new GraphSlot(name, dir, gnode));
        ////                        }
        ////                    }

        ////                } // look for the node id
        ////            } // foreach node element
        ////        }
        ////    }



        ////    // Step 3: build VMs
        ////    foreach (var node in model.Nodes.Values)
        ////    {
        ////        NodesVm.Add(new GraphNodeViewModel(node));
        ////    }

        ////    foreach (var edge in model.Edges)
        ////    {
        ////        var src = NodesVm.First(n => n.Node.Id == edge.SourceNodeId);
        ////        var dst = NodesVm.First(n => n.Node.Id == edge.TargetNodeId);

        ////        // assume first input/output for now; later extend
        ////        var vmEdge = new GraphEdgeViewModel(src, 0, dst, 0);
        ////        Edges.Add(vmEdge);
        ////    }

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
