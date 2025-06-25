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
    private readonly Dictionary<GraphNodeViewModel, Canvas> nodeContainers
    = new Dictionary<GraphNodeViewModel, Canvas>();

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



    private void InitializeGraph()
    {
        if (GraphCanvas is null || tempLine is null)
        {
            Console.WriteLine("Still waiting for GraphCanvas/TempLine...");
            return;
        }

        var vm = DataContext as EditorPageViewModel;
        if (vm == null) 
            return;

        GraphCanvas.Children.Clear();
        nodeContainers.Clear();

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
            }
        };

        var nodeContainer = new Canvas
        {
            Width = 120,
            Height = 60,
            DataContext = gnvm
        };

        Canvas.SetLeft(nodeContainer, gnvm.X);
        Canvas.SetTop(nodeContainer, gnvm.Y);

        // Add the border into the container
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

        // Add the fully populated container to the main canvas
        GraphCanvas.Children.Add(nodeContainer);

        // Keep track of it so dragging can reposition it
        nodeContainers[gnvm] = nodeContainer;
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

    private void DrawEdge(GraphEdgeViewModel edge)
    {
        var line = new Line
        {
            Stroke = Brushes.Yellow,
            StrokeThickness = 2,
            StartPoint = edge.StartPoint,
            EndPoint = edge.EndPoint,
            Tag = edge
        };
        GraphCanvas.Children.Add(line);
    }

    private void OnCanvasPointerPressed(object? s, PointerPressedEventArgs e)
    {
        var pt = e.GetPosition(GraphCanvas);

        // 1) Rubber-band start: if clicked an outgoing slot ellipse
        if (e.Source is Ellipse slotEllipse 
            && slotEllipse.Tag is GraphSlot slot
            && slotEllipse.DataContext is GraphNodeViewModel slotOwner
            )
        {
            tempStartNode = slotOwner;
            tempStartPoint = pt;

            tempLine = new Line
            {
                Stroke = Brushes.Yellow,
                StrokeThickness = 2,
                StartPoint = pt,
                EndPoint = pt
            };

            GraphCanvas.Children.Add(tempLine);
            e.Handled = true;
            return;
        }

        // 2) Node-drag start: if clicked the node border
        if (e.Source is Border hitControl
            && hitControl.DataContext is GraphNodeViewModel nodeVm)
        {
            draggedNode = nodeVm;
            dragOffset = new Point(pt.X - nodeVm.X, pt.Y - nodeVm.Y);
            GraphCanvas.CapturePointer(e.Pointer);
            e.Handled = true;
        }

        // 2) Node-drag start: if clicked the node border
        if (e.Source is TextBlock hitControl2 
            && hitControl2.DataContext is GraphNodeViewModel nodeVm2)
        {
            draggedNode = nodeVm2;
            dragOffset = new Point(pt.X - nodeVm2.X, pt.Y - nodeVm2.Y);
            GraphCanvas.CapturePointer(e.Pointer);
            e.Handled = true;
        }
    }

    private void OnCanvasPointerMoved(object? sender, PointerEventArgs e)
    {
        // A) rubber-band
        if (tempLine != null)
        {
            tempLine.EndPoint = e.GetPosition(GraphCanvas);
            return;
        }

        // B) Node-drag update
        if (draggedNode != null &&
            e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            var pt = e.GetPosition(GraphCanvas);
            draggedNode.X = pt.X - dragOffset.X;
            draggedNode.Y = pt.Y - dragOffset.Y;

            // update the nodecontainers as well:
            if (nodeContainers.TryGetValue(draggedNode, out var container))
            {
                Canvas.SetLeft(container, draggedNode.X);
                Canvas.SetTop(container, draggedNode.Y);
            }

            UpdateEdgeShapes();
        }
    }

    private void OnCanvasPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        var pt = e.GetPosition(GraphCanvas);

        // -- Finish rubber-band
        if (tempLine != null)
        {
            GraphCanvas.Children.Remove(tempLine);

            if (tempStartNode != null
                && DataContext is EditorPageViewModel epvm)
            {
                var target = epvm.NodesVm.FirstOrDefault(n =>
                     Math.Abs(n.X - pt.X) < 60 &&
                     Math.Abs(n.Y - pt.Y) < 30);

                if (target != null && target != tempStartNode)
                {
                    var newEdge = new GraphEdgeViewModel(tempStartNode, target);
                    epvm.Edges.Add(newEdge);
                    DrawEdge(newEdge);
                    UpdateEdgeShapes();
                }
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
            GraphCanvas.ReleasePointerCapture(e.Pointer);
            e.Handled = true;
        }

    }

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