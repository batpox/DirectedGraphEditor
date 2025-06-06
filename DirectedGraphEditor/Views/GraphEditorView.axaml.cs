using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Media;
using DirectedGraphCore;
using DirectedGraphEditor.Helpers;
using DirectedGraphEditor.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
//using System.Drawing;

namespace DirectedGraphEditor.Views;


public partial class GraphEditorView : UserControl
{
    private GraphNodeViewModel? draggedNode = null;
    private Point dragOffset;

    private Line? tempConnectionLine;
    private Ellipse? startSlotEllipse;

    private readonly List<Ellipse> inputSlotEllipses = new();

    public GraphEditorView()
    {
        InitializeComponent();
        this.AttachedToVisualTree += (_, __) =>
        {
            if (DataContext is MainViewModel vm)
                RenderNodes(vm.NodesVm);
        };
    }


    private void RenderNodes(IEnumerable<GraphNodeViewModel> nodeViewModels)
    {
        GraphCanvas.Children.Clear();
        inputSlotEllipses.Clear();

        foreach (var nodeVm in nodeViewModels)
        {
            DrawNode(nodeVm);
        }
    }

    /// <summary>
    /// Draw a UI GNVM node (GraphNodeViewModel)
    /// </summary>
    /// <param name="gnvm"></param>
    private void DrawNode(GraphNodeViewModel gnvm)
    {
        var nodeContainer = new Canvas
        {
            Width = 120,
            Height = 60,
            [Canvas.LeftProperty] = gnvm.X,
            [Canvas.TopProperty] = gnvm.Y
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

        border.PointerPressed += OnNodeBorderPressed;

        Canvas.SetLeft(border, gnvm.X);
        Canvas.SetTop(border, gnvm.Y);

        // Add anchors for inputs (left and top)
        for (int i = 0; i < gnvm.Node.Inputs.Count; i++)
        {
            var anchor = CreateAnchorEllipse(gnvm.Node.Inputs[i], -4, 8 + i * 12);
            nodeContainer.Children.Add(anchor);
            inputSlotEllipses.Add(anchor);

        }

        // Add anchors for outputs (right and bottom)
        for (int i = 0; i < gnvm.Node.Outputs.Count; i++)
        {
            var anchor = CreateAnchorEllipse(gnvm.Node.Outputs[i], border.Width - 4, 8 + i * 12);
            anchor.PointerPressed += OnOutputSlotPressed;
            nodeContainer.Children.Add(anchor);
        }
        GraphCanvas.Children.Add(border);
        GraphCanvas.Children.Add(nodeContainer);
    }

    private void OnOutputSlotPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is not Ellipse ellipse)
            return;

        startSlotEllipse = ellipse;

        var start = e.GetPosition(GraphCanvas);

        tempConnectionLine = new Line
        {
            Stroke = Brushes.Yellow,
            StrokeThickness = 2,
            StartPoint = start,
            EndPoint = start // same as start initially
        };

        GraphCanvas.Children.Add(tempConnectionLine);
        GraphCanvas.PointerMoved += OnCanvasPointerMoved;
        GraphCanvas.PointerReleased += OnCanvasPointerReleased;

        e.Handled = true;
    }
    private void OnCanvasPointerMoved(object? sender, PointerEventArgs e)
    {
        if (tempConnectionLine is null || startSlotEllipse is null)
            return;

        var pointerPos = e.GetPosition(GraphCanvas);
        tempConnectionLine.EndPoint = pointerPos;

        bool hoveringInput = inputSlotEllipses.Any(ellipse =>
        {
            var bounds = ellipse.Bounds;
            var transform = ellipse.TransformToVisual(GraphCanvas);
            if (transform is null) 
                return false;

            bool isHit = HitTestHelper.IsPointerOver(ellipse, GraphCanvas, pointerPos);
            if ( isHit )
            {
                // debug actions 
            }
            return isHit;
        });

        tempConnectionLine.Stroke = hoveringInput ? Brushes.LimeGreen : Brushes.Yellow;
        
    }

    private void OnCanvasPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (tempConnectionLine != null)
        {
            GraphCanvas.Children.Remove(tempConnectionLine);
            tempConnectionLine = null;
            startSlotEllipse = null;
        }

        GraphCanvas.PointerMoved -= OnCanvasPointerMoved;
        GraphCanvas.PointerReleased -= OnCanvasPointerReleased;
    }
    private Ellipse CreateAnchorEllipse(GraphSlot slot, double offsetX, double offsetY)
    {
        var ellipse = new Ellipse
        {
            Width = 10,
            Height = 10,
            Fill = slot.Direction == GraphSlotDirection.Input ? Brushes.LightBlue : Brushes.LightGreen,
            Stroke = Brushes.Black,
            StrokeThickness = 1,
            Tag = slot
        };

        ellipse.SetValue(Canvas.LeftProperty, offsetX);
        ellipse.SetValue(Canvas.TopProperty, offsetY);

        ToolTip.SetTip(ellipse, slot.Name);
        return ellipse;
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
    private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is Control control)
        {
            if (control.DataContext is GraphNodeViewModel node)
            {
                var pointerPos = e.GetPosition(GraphCanvas);
                dragOffset = new Point((int)(pointerPos.X - node.X), (int)(pointerPos.Y - node.Y));
                draggedNode = node;
                e.Pointer.Capture(null);
            }
        }
    }

    private void OnPointerMoved(object? sender, PointerEventArgs e)
    {
        if (draggedNode != null)
        {
            if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
            {
                var pointerPos = e.GetPosition(GraphCanvas);
                draggedNode.X = pointerPos.X - dragOffset.X;
                draggedNode.Y = pointerPos.Y - dragOffset.Y;
            } 
        }
    }

    private void OnPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (sender is Control control)
        {
            e.Pointer.Capture(null);
        }
        draggedNode = null;
    }

}
