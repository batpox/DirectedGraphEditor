using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.VisualTree;
using AvaloniaEdit.Utils;
using DirectedGraphCore;
using DirectedGraphEditor.Controls.GraphNodeControl;
using DirectedGraphEditor.Helpers;

namespace DirectedGraphEditor.Pages.Editor;

public sealed partial class EditorPageView : UserControl
{

    private GraphNodeViewModel? tempStartNode;
    private Point tempStartPoint;

    private Line? tempLine;

    private GraphNodeViewModel? dragStartNode;
    private Point dragStartPoint;
    private GraphNodeViewModel? draggedNode;
    private Point dragOffset;

    ////private Line? TempLine;
    private Ellipse? startSlotEllipse;

    private readonly List<Ellipse> inputSlotEllipses = new();


    public EditorPageView()
    {
        InitializeComponent();
        GraphCanvas = this.FindControl<Canvas>("GraphCanvas");

        GraphCanvas!.AttachedToVisualTree += OnCanvasLoaded;

    }

    private void OnCanvasLoaded(object? sender, VisualTreeAttachmentEventArgs e)
    {
        // Only run once
        GraphCanvas.AttachedToVisualTree -= OnCanvasLoaded;

        InitializeGraph();
    }



    ////// Node‐drag vs. slot‐drag distinction:
    ////private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    ////{
    ////    // If clicked on an Ellipse (slot), start rubber‐band:
    ////    if (e.Source is Ellipse ellipse && ellipse.DataContext is EditorPageViewModel vmNode)
    ////    {
    ////        dragStartNode = vmNode;
    ////        dragStartPoint = e.GetPosition(GraphCanvas);

    ////        edgeLine = new Line
    ////        {
    ////            Stroke = Brushes.Yellow,
    ////            StrokeThickness = 2,
    ////            StartPoint = dragStartPoint,
    ////            EndPoint = dragStartPoint
    ////        };

    ////        GraphCanvas.Children.Add(edgeLine);
    ////        GraphCanvas.PointerMoved += OnCanvasPointerMoved;
    ////        GraphCanvas.PointerReleased += OnCanvasPointerReleased;
    ////        e.Handled = true;
    ////        return;
    ////    }

    ////    // Otherwise, treat as node‐drag:
    ////    if (sender is Control ctrl && ctrl.DataContext is GraphNodeViewModel node)
    ////    {
    ////        var p = e.GetPosition(GraphCanvas);
    ////        dragOffset = new Point(p.X - node.X, p.Y - node.Y);
    ////        draggedNode = node;
    ////        ctrl.CapturePointer(e.Pointer);
    ////    }
    ////}

    ////private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    ////{
    ////    if (sender is Control control)
    ////    {
    ////        if (control.DataContext is GraphNodeViewModel node)
    ////        {
    ////            var pointerPos = e.GetPosition(GraphCanvas);
    ////            dragOffset = new Point((int)(pointerPos.X - node.X), (int)(pointerPos.Y - node.Y));
    ////            draggedNode = node;
    ////            e.Pointer.Capture(null);
    ////        }
    ////    }
    ////}

    private void InitializeGraph()
    {
        if (GraphCanvas is null || tempLine is null)
        {
            Console.WriteLine("Still waiting for GraphCanvas/TempLine...");
            return;
        }

        var vm = DataContext as EditorPageViewModel;
        if (vm == null) return;

        GraphCanvas.Children.Clear();
        vm.CreateNodeData();      // load or refresh your nodes
        RenderEdges(vm.Edges);
        RenderNodes(vm.NodesVm);
    }


    private void GraphCanvas_AttachedToVisualTree(object? sender, VisualTreeAttachmentEventArgs e)
    {
        throw new NotImplementedException();
    }

    private void OnLoaded(object? sender, RoutedEventArgs e)
    {
        if (DataContext is EditorPageViewModel vm)
        {
            GraphCanvas!.Children.Clear();
            vm.CreateNodeData(); // optional
            RenderEdges(vm.Edges);
            RenderNodes(vm.NodesVm);
        }
    }
    void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    static void Launch(string fileName)
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = fileName,
            UseShellExecute = true
        });
    }

    void OnLatestVersionClicked(object? sender, PointerPressedEventArgs e)
    {
        ////((EditorPageViewModel)DataContext!).OpenReleasesUrl();
    }

    void OnOpenHomepage(object? sender, RoutedEventArgs e)
    {
        Launch("https://github.com/chkr1011/DirectedGraphEditor");
    }

    void OnReportBug(object? sender, RoutedEventArgs e)
    {
        Launch("https://github.com/chkr1011/DirectedGraphEditor/issues/new");
    }

    void OnRequestFeature(object? sender, RoutedEventArgs e)
    {
        Launch("https://github.com/chkr1011/DirectedGraphEditor/issues/new");
    }

    void OpenUrlFromButtonContent(object? sender, RoutedEventArgs e)
    {
        Launch((sender as Button)?.Content as string ?? string.Empty);
    }

    static string ReadEmbeddedMarkdown()
    {
        var assembly = Assembly.GetExecutingAssembly();
        using var stream = assembly.GetManifestResourceStream("DirectedGraphEditor.Pages.Info.Readme.md");

        if (stream == null)
        {
            return string.Empty;
        }

        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }


    ////private void OnPointerPressed1(object? sender, PointerPressedEventArgs e)
    ////{
    ////    if (e.Source is Ellipse ellipse && ellipse.DataContext is GraphNodeViewModel node)
    ////    {
    ////        dragStartNode = node;
    ////        dragStartPoint = e.GetPosition(GraphCanvas);

    ////        if (tempStartNode != null)
    ////        {
    ////            edgeLine.StartPoint = tempStartPoint;
    ////            edgeLine.EndPoint = tempStartPoint;
    ////            edgeLine.IsVisible = true;
    ////            e.Handled = true;
    ////        }
    ////    }
    ////}



    ////private void OnPointerMoved1(object? sender, PointerEventArgs e)
    ////{
    ////    if (edgeLine == null)
    ////        return;

    ////    if (edgeLine.IsVisible)
    ////    {
    ////        edgeLine.EndPoint = e.GetPosition(GraphCanvas);
    ////    }
    ////}

    ////private void OnPointerMoved(object? sender, PointerEventArgs e)
    ////{
    ////    if (draggedNode != null)
    ////    {
    ////        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
    ////        {
    ////            var pointerPos = e.GetPosition(GraphCanvas);
    ////            draggedNode.X = pointerPos.X - dragOffset.X;
    ////            draggedNode.Y = pointerPos.Y - dragOffset.Y;
    ////        }
    ////    }
    ////}


    ////private void OnPointerReleased1(object? sender, PointerReleasedEventArgs e)
    ////{
    ////    if (edgeLine == null)
    ////        return;

    ////    if (!edgeLine.IsVisible || tempStartNode is null)
    ////        return;

    ////    edgeLine.IsVisible = false;
    ////    var pointerPos = e.GetPosition(GraphCanvas);
    ////    tempStartNode = null;

    ////    if (DataContext is not EditorPageViewModel vm)
    ////        return;

    ////    // Simplified hit test
    ////    var targetNode = vm.NodesVm.FirstOrDefault(n =>
    ////        Math.Abs(n.X - pointerPos.X) < 60 &&
    ////        Math.Abs(n.Y - pointerPos.Y) < 30);

    ////    if (targetNode != null && targetNode != tempStartNode)
    ////    {
    ////        vm.Edges.Add(new GraphEdgeViewModel(tempStartNode, targetNode));
    ////    }
    ////}

    ////private void OnPointerReleased(object? sender, PointerReleasedEventArgs e)
    ////{
    ////    if (sender is Control control)
    ////    {
    ////        e.Pointer.Capture(null);
    ////    }
    ////    draggedNode = null;
    ////}


    private void RenderNodes(IEnumerable<GraphNodeViewModel> nodes)
    {
        //GraphCanvas.Children.Clear();
        inputSlotEllipses.Clear();

        foreach (var node in nodes)
        {
            DrawNode(node);
        }
    }

    private void RenderEdges(IEnumerable<GraphEdgeViewModel> edgeViewModels)
    {
        foreach (var edge in edgeViewModels)
        {
            var line = new Line
            {
                Stroke = Brushes.Yellow,
                StrokeThickness = 2
            };

            line.StartPoint = edge.StartPoint;
            line.EndPoint = edge.EndPoint;
            line.Tag = edge;

            GraphCanvas.Children.Add(line);
        }
    }

    /////// <summary>
    /////// Draw a UI GNVM node (GraphNodeViewModel)
    /////// </summary>
    /////// <param name="gnvm"></param>
    ////private void DrawNode(GraphNodeViewModel gnvm)
    ////{
    ////    var nodeContainer = new Canvas
    ////    {
    ////        Width = 120,
    ////        Height = 60,
    ////        [Canvas.LeftProperty] = gnvm.X,
    ////        [Canvas.TopProperty] = gnvm.Y
    ////    };

    ////    var border = new Border
    ////    {
    ////        Width = 120,
    ////        Height = 60,
    ////        Background = Brushes.DarkSlateGray,
    ////        BorderBrush = Brushes.White,
    ////        BorderThickness = new Thickness(2),
    ////        CornerRadius = new CornerRadius(5),
    ////        Child = new TextBlock
    ////        {
    ////            Text = gnvm.Node.Name,
    ////            Foreground = Brushes.White,
    ////            FontSize = 14,
    ////            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
    ////            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
    ////        },
    ////        DataContext = gnvm
    ////    };

    ////    border.PointerPressed += OnNodeBorderPressed;

    ////    Canvas.SetLeft(border, gnvm.X);
    ////    Canvas.SetTop(border, gnvm.Y);

    ////    // Add anchors for inputs (left and top)
    ////    for (int i = 0; i < gnvm.Node.Inputs.Count; i++)
    ////    {
    ////        var anchor = CreateAnchorEllipse(gnvm.Node.Inputs[i], -4, 8 + i * 12);
    ////        nodeContainer.Children.Add(anchor);
    ////        inputSlotEllipses.Add(anchor);

    ////    }

    ////    // Add anchors for outputs (right and bottom)
    ////    for (int i = 0; i < gnvm.Node.Outputs.Count; i++)
    ////    {
    ////        var anchor = CreateAnchorEllipse(gnvm.Node.Outputs[i], border.Width - 4, 8 + i * 12);
    ////        anchor.PointerPressed += OnOutputSlotPressed;
    ////        nodeContainer.Children.Add(anchor);
    ////    }
    ////    GraphCanvas.Children.Add(border);
    ////    GraphCanvas.Children.Add(nodeContainer);
    ////}

    private void OnOutputSlotPressed(object? sender, PointerPressedEventArgs e)
    {
        //if (sender is not Ellipse ellipse)
        //    return;

        //startSlotEllipse = ellipse;

        //var start = e.GetPosition(GraphCanvas);

        //edgeLine.StartPoint = start;
        //edgeLine.EndPoint = start;
        //edgeLine.IsVisible = true;

        //////GraphCanvas.Children.Add(TempLine);
        //GraphCanvas.PointerMoved += OnCanvasPointerMoved;
        //GraphCanvas.PointerReleased += OnCanvasPointerReleased;

        //e.Handled = true;
    }
    private void OnCanvasPointerMoved(object? sender, PointerEventArgs e)
    {

        if (tempLine != null)
        {
            tempLine.EndPoint = e.GetPosition(GraphCanvas);
            return;
        }

        // B) Node-drag update
        if (draggedNode != null &&
            e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            var p = e.GetPosition(GraphCanvas);
            draggedNode.X = p.X - dragOffset.X;
            draggedNode.Y = p.Y - dragOffset.Y;
            UpdateEdgeShapes();
        }
    }

    private void UpdateEdgeShapes()
    {
        foreach (var line in GraphCanvas.Children.OfType<Line>())
        {
            if (line.Tag is GraphEdgeViewModel vm)
            {
                line.StartPoint = vm.StartPoint;
                line.EndPoint = vm.EndPoint;
            }
        }
    }

    private void DrawNode(GraphNodeViewModel gnvm)
    {
        var nodeContainer = new Canvas
        {
            Width = 120,
            Height = 60,
            [Canvas.LeftProperty] = gnvm.X,
            [Canvas.TopProperty] = gnvm.Y,
            DataContext = gnvm
        };

        var border = new Border
        {
            Width = 120,
            Height = 60,
            Background = Brushes.DarkSlateGray,
            BorderBrush = Brushes.White,
            BorderThickness = new Thickness(2),
            CornerRadius = new CornerRadius(5),
            Child = new TextBlock
            {
                Text = gnvm.Node.Name,
                Foreground = Brushes.White,
                FontSize = 14,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
            },
            DataContext = gnvm
        };

        border.PointerPressed += OnCanvasPointerPressed;
        nodeContainer.Children.Add(border);

        // input slots
        for (int i = 0; i < gnvm.Node.Inputs.Count; i++)
        {
            var slot = gnvm.Node.Inputs[i];
            var ell = CreateSlotEllipse(slot, -4, 8 + i * 12);
            nodeContainer.Children.Add(ell);
        }

        // output slots
        for (int i = 0; i < gnvm.Node.Outputs.Count; i++)
        {
            var slot = gnvm.Node.Outputs[i];
            var ell = CreateSlotEllipse(slot, 120 - 6, 8 + i * 12);
            ell.PointerPressed += OnCanvasPointerPressed;
            nodeContainer.Children.Add(ell);
        }

        GraphCanvas.Children.Add(nodeContainer);
    }

    private Ellipse CreateSlotEllipse(GraphSlot slot, double x, double y)
    {
        var ellipse = new Ellipse
        {
            Width = 10,
            Height = 10,
            Fill = slot.Direction == GraphSlotDirection.Input
                                   ? Brushes.LightBlue
                                   : Brushes.LightGreen,
            Stroke = Brushes.Black,
            StrokeThickness = 1,
            Tag = slot
        };
        Canvas.SetLeft(ellipse, x);
        Canvas.SetTop(ellipse, y);
        ToolTip.SetTip(ellipse, slot.Name);
        return ellipse;
    }

    private void OnCanvasPointerPressed(object? s, PointerPressedEventArgs e)
    {
        // 1) Rubber-band start: if clicked an outgoing slot ellipse
        if (e.Source is Ellipse slotEllipse &&
            slotEllipse.DataContext is GraphNodeViewModel slotOwner)
        {
            tempStartNode = slotOwner;
            var start = e.GetPosition(GraphCanvas);

            tempLine = new Line
            {
                Stroke = Brushes.Yellow,
                StrokeThickness = 2,
                StartPoint = start,
                EndPoint = start
            };

            GraphCanvas.Children.Add(tempLine);
            e.Handled = true;
            return;
        }

        // 2) Node-drag start: if clicked the node border
        if (e.Source is Border border &&
            border.DataContext is GraphNodeViewModel nodeVm)
        {
            var p = e.GetPosition(GraphCanvas);
            draggedNode = nodeVm;
            dragOffset = new Point(p.X - nodeVm.X, p.Y - nodeVm.Y);
            border.CapturePointer(e.Pointer);
            e.Handled = true;
        }
    }


    private void OnCanvasPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (tempLine != null)
        {
            var end = e.GetPosition(GraphCanvas);
            GraphCanvas.Children.Remove(tempLine);

            if (tempLine != null
                && DataContext is EditorPageViewModel epvm)
            {
                var target = epvm.NodesVm.FirstOrDefault(n =>
                     Math.Abs(n.X - end.X) < 60 &&
                     Math.Abs(n.Y - end.Y) < 30);

                if (target != null && target != tempStartNode)
                    epvm.Edges.Add(new GraphEdgeViewModel(tempStartNode, target));

            }

            tempLine = null;
            tempStartNode = null;
            e.Handled = true;
            return;
        }

        // B: node-drag finish
        if ( draggedNode != null )
        {
            draggedNode = null;
            e.Handled = true;
        }

    }

    ////private Ellipse CreateAnchorEllipse(GraphSlot slot, double offsetX, double offsetY)
    ////{
    ////    var ellipse = new Ellipse
    ////    {
    ////        Width = 10,
    ////        Height = 10,
    ////        Fill = slot.Direction == GraphSlotDirection.Input ? Brushes.LightBlue : Brushes.LightGreen,
    ////        Stroke = Brushes.Black,
    ////        StrokeThickness = 1,
    ////        Tag = slot
    ////    };

    ////    ellipse.SetValue(Canvas.LeftProperty, offsetX);
    ////    ellipse.SetValue(Canvas.TopProperty, offsetY);

    ////    ToolTip.SetTip(ellipse, slot.Name);
    ////    return ellipse;
    ////}
    private void OnNodeBorderPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is Border border && border.DataContext is GraphNodeViewModel nodeVm)
        {
            Console.WriteLine($"Clicked node: {nodeVm.Name}");
            e.Handled = true;
        }
    }

    private void OnNodePointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is Control ctrl && ctrl.DataContext is GraphNodeViewModel graphVm)
        {
            Console.WriteLine($"Clicked node: {graphVm.Node.Name}");
        }
    }



}