using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using DirectedGraphCore.Commands;
using DirectedGraphCore.Controllers;
using DirectedGraphCore.Models;
using DirectedGraphEditor.Controls;
using DirectedGraphEditor.Interaction;
using System.Collections.Generic;

namespace DirectedGraphEditor.Adapters;

// Contracts DragController depends on:
public interface IHitTester
{
    (NodeControl View, string NodeId)? NodeUnderPoint(Point canvasPt);
    (EdgeControl View, string EdgeId, bool IsTargetEnd)? EdgeEndpointUnderPoint(Point canvasPt, double hitRadius);
}

public interface IPinResolver
{
    // Implement with your existing “snap or insert” logic against the model/VM.
    (string PinId, object? InsertPinCommand) ResolveForDrop(
        string nodeId,
        EnumNodePinDirection dir,
        double dropYNodeSpace);
}


public sealed class GraphAdapter : IHitTester, IPinResolver, IRubberHost
{
    private EdgeControl? _rubber;
    private readonly Canvas _canvas;

    private readonly GraphController controller;
    private readonly CommandStack commands;

    // NodeId -> its view (NodeControl)
    internal readonly Dictionary<string, NodeControl> NodeViews = new();
    internal readonly Dictionary<string, EdgeControl> EdgeViews = new();


    // Drag state
    private GraphNode? dragSourceNode;
    private NodePin? dragSourcePin;
    private Line? rubber;

    // Snap constants
    private const double PinRadiusPx = 5.0;  // Ellipse 10x10
    private const double SnapDistance = PinRadiusPx * 4;  // two diameters

    // -------------------- fields (add near your other fields) --------------------
    private enum PressState { None, DragNode, DragEndpoint }

    private PressState pressState = PressState.None;

    // Node drag state
    private NodeControl? dragNodeView;
    private GraphNode? dragNodeModel;
    private Point dragStartCanvas;
    private double startLeft, startTop;

    // Endpoint drag state
    private GraphEdge? dragEdge;
    private bool dragEndpointIsTarget; // true = dragging Target end, false = dragging Source end
    private Point fixedEndCanvas;      // canvas point of the anchored (non-dragging) end

    // Visuals
    private readonly Dictionary<string, Line> edgeLines = new();  // keep this if not present
    private const double EndpointHitRadius = 10; // px

    public GraphAdapter(Canvas canvas)
    {
        this._canvas = canvas;
    }

    // ----- View creation / disposal (called by controller that reacts to model events)

    public NodeControl CreateNodeView(string nodeId, double x, double y, Size size, string? title = null, object? dataContext = null)
    {
        var nc = new NodeControl
        {
            Width = size.Width,
            Height = size.Height,
            Title = title,
            DataContext = dataContext
        };
        Canvas.SetLeft(nc, x);
        Canvas.SetTop(nc, y);

        // Ensure nodes are on top of edges so they are visible and hit-testable.
        nc.SetValue(Panel.ZIndexProperty, 100);

        _canvas.Children.Add(nc);
        NodeViews[nodeId] = nc;
        return nc;
    }

    public void RemoveNodeView(string nodeId)
    {
        if (NodeViews.Remove(nodeId, out var nc))
            _canvas.Children.Remove(nc);
    }

    public EdgeControl CreateEdgeView(string edgeId, Point p0, Point p1, object? dataContext = null)
    {
        var ec = new EdgeControl
        {
            SourcePoint = p0,
            TargetPoint = p1,
            DataContext = dataContext
        };

        // Ensure edges render underneath nodes
        ec.SetValue(Panel.ZIndexProperty, 0);

        // EdgeControl draws in canvas coordinates; no Canvas.Left/Top needed.
        _canvas.Children.Add(ec);
        EdgeViews[edgeId] = ec;
        return ec;
    }

    public void RemoveEdgeView(string edgeId)
    {
        if (EdgeViews.Remove(edgeId, out var ec))
            _canvas.Children.Remove(ec);
    }

    public void UpdateEdgePoints(string edgeId, Point? p0, Point? p1)
    {
        if (EdgeViews.TryGetValue(edgeId, out var ec))
        {
            ec.SourcePoint = p0.Value;
            ec.TargetPoint = p1.Value;
            ec.InvalidateVisual();
        }
    }

    /// <summary>
    /// Find selected object in the order of nodes first, then edge endpoints, and then edges.
    /// </summary>
    public void SetSelected(string? nodeId = null, string? edgeId = null, bool selected = false)
    {
        // Clear-all request
        if (nodeId is null && edgeId is null && selected == false)
        {
            foreach (var nv in NodeViews.Values) { nv.IsSelected = false; nv.InvalidateVisual(); }
            foreach (var ev in EdgeViews.Values) { ev.IsSelected = false; ev.InvalidateVisual(); }
            return;
        }

        if (nodeId != null)  
        {
            foreach (var kv in NodeViews)
            {
                var isSel = kv.Key == nodeId && selected;
                kv.Value.IsSelected = isSel;
                kv.Value.InvalidateVisual();
            }
            // Clear edges when node selected
            if (selected)
            {
                foreach (var ev in EdgeViews.Values) { ev.IsSelected = false; ev.InvalidateVisual(); }
            }
            return;
        }
        if (edgeId != null) 
        {
            foreach (var kv in EdgeViews)
            {
                var isSel = kv.Key == edgeId && selected;
                kv.Value.IsSelected = isSel;
                kv.Value.InvalidateVisual();
            }
            // Clear nodes when edge selected
            if (selected)
            {
                foreach (var nv in NodeViews.Values) { nv.IsSelected = false; nv.InvalidateVisual(); }
            }
            return;
        }
    }

    // ----- Event forwarding to DragController (single place for pointer logic)
    public void WireCanvasToDragController(IDragController drag)
    {
        _canvas.PointerPressed += (s, e) => drag.HandleCanvasPointerPressed(e);
        _canvas.PointerMoved += (s, e) => drag.HandleCanvasPointerMoved(e);
        _canvas.PointerReleased += (s, e) => drag.HandleCanvasPointerReleased(e);
    }

    public (NodeControl View, string NodeId)? NodeUnderPoint(Point canvasPt)
    {
        // Iterate NodeViews in visual top-to-bottom order:
        // 1) by Panel.ZIndex (higher first), 2) by canvas child index (higher wins)
        var ordered = new List<(string Id, NodeControl View)>(NodeViews.Count);
        foreach (var kv in NodeViews)
            ordered.Add((kv.Key, kv.Value));

        ordered.Sort((a, b) =>
        {
            // Read the attached ZIndex value directly (avoid calling a missing static helper)
            var za = a.View.GetValue(Panel.ZIndexProperty);
            var zb = b.View.GetValue(Panel.ZIndexProperty);
            int iza = za is int ia ? ia : 0;
            int izb = zb is int ib ? ib : 0;
            if (iza != izb) 
                return izb.CompareTo(iza); // higher ZIndex first

            // fallback: whichever appears later in the canvas children is on top
            int iaIndex = _canvas.Children.IndexOf(a.View);
            int ibIndex = _canvas.Children.IndexOf(b.View);
            return 
                ibIndex.CompareTo(iaIndex);
        });

        foreach (var item in ordered)
        {
            var nc = item.View;
            if (nc is null) 
                continue;

            // Use the control's own hit test (translates point from canvas)
            if (nc.Hit(canvasPt, _canvas))
                return (nc, item.Id);
        }

        return null;
    }

    public (EdgeControl View, string EdgeId, bool IsTargetEnd)? EdgeEndpointUnderPoint(Point canvasPt, double hitRadius)
    {
        foreach (var kvp in EdgeViews)
        {
            var view = kvp.Value;
            if (view.HitEndpoint(canvasPt, hitRadius, out var isTarget))
                return (view, kvp.Key, isTarget);
        }
        return null;
    }

    // Helper: mirror the same anchor calculation used elsewhere (fallback-safe)
    internal Point ResolvePinCanvasPoint(EditorContext ec, string nodeId, int defaultSide)
    {
        if (ec is null) return new Point(0, 0);
        if (!ec.NodeViews.TryGetValue(nodeId, out var nodeView))
            return new Point(0, 0);

        var left = Canvas.GetLeft(nodeView);
        if (double.IsNaN(left)) left = 0;
        var top = Canvas.GetTop(nodeView);
        if (double.IsNaN(top)) top = 0;

        var width = nodeView.Bounds.Width;
        if (double.IsNaN(width) || width <= 0) width = nodeView.Width;
        var height = nodeView.Bounds.Height;
        if (double.IsNaN(height) || height <= 0) height = nodeView.Height;

        if (width <= 0) width = 140;
        if (height <= 0) height = 60;

        var px = defaultSide > 0 ? left + width : left;
        var py = top + height / 2;
        return new Point(px, py);
    }


    // ----- IPinResolver (stub to be connected to your real logic)
    public (string PinId, object? InsertPinCommand) ResolveForDrop(string nodeId, EnumNodePinDirection dir, double dropYNodeSpace)
    {
        // Hook this into your model/VM: try nearest pin; if none, return a command to insert.
        // For now, return a placeholder “existing” pin id and no insert command.
        return ($"{nodeId}:{dir}:nearest", null);
    }

    private string? FindNodeIdByView(NodeControl view)
    {
        // Optional: cache reverse map if you prefer O(1)
        foreach (var (id, v) in NodeViews)
            if (ReferenceEquals(v, view)) return id;
        return null;
    }

    public void ShowRubber(Point startCanvas, Point endCanvas)
    {
        if (_rubber is null)
        {
            _rubber = new EdgeControl
            {
                ShowEndpointHandles = false,
                IsSelected = false
            };
            _canvas.Children.Add(_rubber);
        }
        _rubber.SourcePoint = startCanvas;
        _rubber.TargetPoint = endCanvas;
        _rubber.InvalidateVisual();
    }

    public void UpdateRubberEnd(Point endCanvas)
    {
        if (_rubber is null) return;
        _rubber.TargetPoint = endCanvas;
        _rubber.InvalidateVisual();
    }

    public void HideRubber()
    {
        if (_rubber is null) return;
        _canvas.Children.Remove(_rubber);
        _rubber = null;
    }
}

