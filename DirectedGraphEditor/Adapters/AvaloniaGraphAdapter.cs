using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Avalonia.VisualTree;
using DirectedGraphCore.Controllers;
using DirectedGraphCore.Models;
using DirectedGraphEditor.Controls.GraphNodeControl;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;

namespace DirectedGraphEditor.Adapters;

public class AvaloniaGraphAdapter : IDisposable
{
    private readonly GraphController controller;
    private readonly Canvas canvas;
    private bool disposed;

    private GraphNode? dragTarget;
    private Vector2 lastPointerPos;

    public enum EditorMode { Select, AddNode, AddEdge }

    private EditorMode currentMode = EditorMode.Select;

    private bool isResetting;
    private readonly List<GraphNode> pendingNodes = new();
    private readonly List<GraphEdge> pendingEdges = new();


    public AvaloniaGraphAdapter(GraphController controller, Canvas canvas)
    {
        this.controller = controller;
        this.canvas = canvas;

        controller.NodeAdded += OnNodeAdded;
        controller.NodeMoved += OnNodeMoved;
        controller.SelectionChanged += OnSelectionChanged;
        controller.EdgeAdded += OnEdgeAdded;
        controller.EdgeRemoved += OnEdgeRemoved;
        controller.SelectionChanged += OnSelectionChanged;
        controller.NodeMoved += OnNodeMoved;
        controller.GraphReset += OnGraphReset;
        controller.GraphResetCompleted += OnGraphResetCompleted;
    }

    /// <summary>A full reset of the canvas</summary>
    private void OnGraphReset()
    {
        isResetting = true;
        pendingNodes.Clear();
        pendingEdges.Clear();

        // 🧠 Avalonia 11 Canvas clear before new model is drawn
        canvas.Children.Clear();
    }

    private void OnGraphResetCompleted()
    {
        isResetting = false;

        // Add everything collected during reset
        foreach (var node in pendingNodes)
            OnNodeAdded(node);
        foreach (var edge in pendingEdges)
            OnEdgeAdded(edge);

        pendingNodes.Clear();
        pendingEdges.Clear();
    }

    public void RenderAll()
    {
        canvas.Children.Clear();

        // Draw edges first (behind nodes)
        foreach (var edge in controller.Model.Edges.Values)
            OnEdgeAdded(edge);

        // Draw nodes
        foreach (var node in controller.Model.Nodes.Values)
            OnNodeAdded(node);
    }

    private void OnEdgeRemoved(GraphEdge edge)
    {
        // Find and remove the corresponding line
        var line = canvas.Children
            .OfType<Line>()
            .FirstOrDefault(l => ReferenceEquals(l.DataContext, edge));

        if (line != null)
            canvas.Children.Remove(line);
    }
    private void OnEdgeAdded(GraphEdge edge)
    {
        // Look up source and target nodes
        if (!controller.Model.Nodes.TryGetValue(edge.SourceNodeId, out var source)) 
            return;
        if (!controller.Model.Nodes.TryGetValue(edge.TargetNodeId, out var target)) 
            return;

        if (isResetting)
        {
            pendingEdges.Add(edge);
            return;
        }
        // Create a simple connecting line
        var line = new Line
        {
            StartPoint = new Avalonia.Point(source.Position.X, source.Position.Y),
            EndPoint = new Avalonia.Point(target.Position.X, target.Position.Y),
            Stroke = Brushes.Gray,
            StrokeThickness = 1.5,
            DataContext = edge
        };

        // Place behind node visuals
        //Canvas.SetZIndex(line, 0);
        canvas.Children.Add(line);
    }

    private void OnNodeAdded(GraphNode node)
    {
        var view = new GraphNodeControl { DataContext = node };
        Canvas.SetLeft(view, node.Position.X);
        Canvas.SetTop(view, node.Position.Y);

        // Pointer events
        view.PointerPressed += (_, e) =>
        {
            if (e.GetCurrentPoint(view).Properties.IsLeftButtonPressed)
            {
                controller.SelectNode(node, multiSelect: e.KeyModifiers.HasFlag(KeyModifiers.Control));
                dragTarget = node;
                lastPointerPos = e.GetPosition(canvas).ToVector2();

                // capture pointer explicitly to continue receiving events
                e.Pointer.Capture(view);
            }
        };

        view.PointerReleased += (_, e) =>
        {
            dragTarget = null;

            // release pointer capture
            e.Pointer.Capture(null);
        };

        view.PointerMoved += (_, e) =>
        {
            if (dragTarget != null && e.GetCurrentPoint(canvas).Properties.IsLeftButtonPressed)
            {
                var newPosition = e.GetPosition(canvas).ToVector2();

                Vector2 delta = newPosition - lastPointerPos;
                controller.MoveSelectedBy(delta);
                lastPointerPos = newPosition;
            }
        };

        canvas.Children.Add(view);
    }

    private void OnNodeRemoved(GraphNode node)
    {
        // 1. Remove any edges connected to this node
        var connectedEdges = controller.Model.Edges.Values
            .Where(e => e.SourceNodeId == node.Id || e.TargetNodeId == node.Id)
            .ToList();

        foreach (var edge in connectedEdges)
        {
            // Remove edge from model first
            controller.Model.Edges.Remove(edge.Id);

            // Remove its visual line (if any)
            var line = canvas.Children
                .OfType<Line>()
                .FirstOrDefault(l => ReferenceEquals(l.DataContext, edge));

            if (line != null)
                canvas.Children.Remove(line);
        }

        // 2. Remove the node’s visual itself
        var nodeView = canvas.Children
            .OfType<GraphNodeControl>()
            .FirstOrDefault(v => ReferenceEquals(v.DataContext, node));

        if (nodeView != null)
            canvas.Children.Remove(nodeView);
    }
    private void OnNodeMoved(GraphNode node)
    {
        var nodeView = canvas.Children.OfType<GraphNodeControl>()
            .FirstOrDefault(v => ReferenceEquals(v.DataContext, node));
        if (nodeView != null)
        {
            // Set directly, only if moved
            double left = Canvas.GetLeft(nodeView);
            double top = Canvas.GetTop(nodeView);

            if (left != node.Position.X || top != node.Position.Y)
            {
                Canvas.SetLeft(nodeView, node.Position.X);
                Canvas.SetTop(nodeView, node.Position.Y);
            }
        }

        // Find all edges connected to this node
        var connectedEdges = controller.Model.Edges.Values
            .Where(e => e.SourceNodeId == node.Id || e.TargetNodeId == node.Id)
            .ToList();

        foreach (var edge in connectedEdges)
        {
            var line = canvas.Children
                .OfType<Line>()
                .FirstOrDefault(l => ReferenceEquals(l.DataContext, edge));

            if (line == null) continue;

            if (controller.Model.Nodes.TryGetValue(edge.SourceNodeId, out var src) &&
                controller.Model.Nodes.TryGetValue(edge.TargetNodeId, out var dst))
            {
                line.StartPoint = new Avalonia.Point(src.Position.X, src.Position.Y);
                line.EndPoint = new Avalonia.Point(dst.Position.X, dst.Position.Y);
            }
        }

    }

    private void OnSelectionChanged()
    {
        foreach (var view in canvas.Children.OfType<GraphNodeControl>())
        {
            var node = (GraphNode)view.DataContext;
            view.IsSelected = controller.SelectedNodes.Contains(node);
        }
    }

    // ─── IDisposable Implementation ────────────────────────
    public void Dispose()
    {
        if (disposed) return;
        disposed = true;

        controller.NodeAdded -= OnNodeAdded;
        controller.NodeRemoved -= OnNodeRemoved;
        controller.EdgeAdded -= OnEdgeAdded;
        controller.EdgeRemoved -= OnEdgeRemoved;
        controller.SelectionChanged -= OnSelectionChanged;
        controller.NodeMoved -= OnNodeMoved;
    }

    private static Control? FindGraphElement(object? source)
    {
        // Avalonia 11 pointer event sources implement IVisual
        var visual = source as Visual;

        while (visual != null)
        {
            // stop when we hit a node or an edge visual
            if (visual is GraphNodeControl or Line)
                return visual as Control;

            visual = visual.GetVisualParent();
        }

        return null;
    }

    public void HandleCanvasPointerPressed(PointerPressedEventArgs e)
    {
        var hit = FindGraphElement(e.Source);
        if (hit == null) {
            Console.WriteLine("Clicked empty canvas.");
            return;
        }

        switch (hit)
            {
            case GraphNodeControl nodeControl:
                // Handled in node control itself
                return;
            case Line line:
                if (line.DataContext is GraphEdge edge)
                {
                    Console.WriteLine($"Clicked edge from {edge.SourceNodeId} to {edge.TargetNodeId}");
                    // Optionally select the edge or show context menu
                }
                return;
            default:
                return;
        }

        if (e.GetCurrentPoint(canvas).Properties.IsLeftButtonPressed)
        {
            var point = e.GetCurrentPoint(canvas);
            if (!point.Properties.IsLeftButtonPressed)
                return;

            var pos = e.GetPosition(canvas).ToVector2();

            switch (currentMode)
            {
                case EditorMode.AddNode:
                    //controller.AddNode(new System.Drawing.PointF(pos.X, pos.Y));
                    e.Handled = true;
                    break;

                case EditorMode.Select:


                    break;
                default:
                    // Clicking empty canvas clears selection
                    controller.ClearSelection();
                    break;
            }
        }
    }

    public void HandleCanvasPointerMoved(PointerEventArgs e)
    {
        // optional: could be used for box-selection or preview
    }

    public void HandleCanvasPointerReleased(PointerReleasedEventArgs e)
    {
        // optional for future drag/box logic
    }

    public void HandleCanvasLoaded(RoutedEventArgs e)
    {
        RenderAll();
    }

    /// <summary>
    /// Loads a graph from a DGML file using a file open dialog.
    /// </summary>
    /// <returns></returns>
    public async Task LoadGraphUsingDialogAsync()
    {
        var top = TopLevel.GetTopLevel(canvas);
        if (top?.StorageProvider == null) return;

        var files = await top.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Open Graph File",
            AllowMultiple = false,
            FileTypeFilter = new[]
            {
            new FilePickerFileType("DGML Graph") { Patterns = new[] { "*.dgml" } }
        }
        });

        if (files.Count == 1)
        {
            var path = files[0].Path.LocalPath;
            controller.Model.LoadFromDgmlFile(path);
        }
    }

    public async Task SaveGraphUsingDialogAsync(bool forceNewFile = false)
    {
        var top = TopLevel.GetTopLevel(canvas);
        if (top?.StorageProvider == null) return;

        string? path = null;

        if (forceNewFile)
        {
            var storagePath = await top.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = "Save DGML Graph As",
                DefaultExtension = "dgml",
                FileTypeChoices = new[]
                {
                new FilePickerFileType("DGML Graph") { Patterns = new[] { "*.dgml" } }
            }
            });
            path = storagePath?.Path.LocalPath;
        }

        if (!forceNewFile && controller.Model?.FilePath != null)
            path = controller.Model.FilePath;

        if (path != null)
        {
            controller.Model.SaveAsDgml(path);
        }
    }

}
