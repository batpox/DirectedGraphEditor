// DragController.cs
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using AvaloniaEdit.Utils;
using DirectedGraphCore.Commands;        // CommandStack, CompositeCommand, RemoveEdgeCommand, AddEdgeCommand, InsertPinCommand
using DirectedGraphCore.Geometry;
using DirectedGraphCore.Models;          // GraphNode, GraphEdge
using DirectedGraphEditor.Controls;
using DirectedGraphEditor.Helpers;
using System.Collections.Generic;
using System.Runtime.InteropServices.Marshalling;
using static DirectedGraphCore.Commands.UndoCommands;

namespace DirectedGraphEditor.Interaction;

/// <summary>
/// Minimal host for creating/updating/removing the temporary rubber edge on the canvas.
/// Implement this on your GraphAdapter (or a tiny adapter-owned helper).
/// </summary>
public interface IRubberHost
{
    /// <summary>Create rubber edge (if not present) and set its start & end in canvas space.</summary>
    void ShowRubber(Point startCanvas, Point endCanvas);

    /// <summary>Update the end point during drag.</summary>
    void UpdateRubberEnd(Point endCanvas);

    /// <summary>Remove/hide the rubber edge.</summary>
    void HideRubber();
}

public interface IDragController
{
    void HandleCanvasPointerPressed(PointerPressedEventArgs e);
    void HandleCanvasPointerMoved(PointerEventArgs e);
    void HandleCanvasPointerReleased(PointerReleasedEventArgs e);
}

public sealed class DragController : IDragController
{
    private readonly ModelContext _mc;
    private readonly EditorContext _ec;

    // ────────────── State ──────────────
    private enum DragKind { None, Node, EdgeEndpoint }
    private DragKind _dragKind = DragKind.None;

    private struct NodeDrag
    {
        public string NodeId;
        public Point PressCanvas;
        public Point NodeOriginCanvas;
        public Vector PressOffset; // press - nodeTopLeft
    }
    private NodeDrag _nodeDrag;

    private struct EdgeEndpointDrag
    {
        public string EdgeId;
        public bool MovingTargetEnd;
        public Point FixedEndCanvas;
        public Point StartCanvas;
    }
    private EdgeEndpointDrag _edgeDrag;

    // Tuning
    private const double EndpointHitRadius = 10.0;
    private const double EdgeHitDistance = 6.0;

    public DragController( ModelContext mc, EditorContext ec)
    {
        _mc = mc;
        _ec = ec;
    }

    // ───────────────────────── Pointer Handlers ─────────────────────────

    public void HandleCanvasPointerPressed(PointerPressedEventArgs e)
    {
        if (_ec?.Canvas == null) 
            return;
        var canvas = _ec.Canvas;

        var pos = e.GetPosition(relativeTo: canvas); // canvas space
        e.Handled = false;

        // 1) Try node press
        var nt = _ec.HitTester.NodeUnderPoint(canvasPt: pos);
        if (nt is { } nodeHit)
        {
            var view = nodeHit.View;
            var topLeft = new Point(x: Canvas.GetLeft(view), y: Canvas.GetTop(view));
            _dragKind = DragKind.Node;
            _nodeDrag = new NodeDrag
            {
                NodeId = nodeHit.NodeId,
                PressCanvas = pos,
                NodeOriginCanvas = topLeft,
                PressOffset = pos - topLeft
            };

            // Optionally: mark selection here, or let clicks elsewhere do it
            view.IsSelected = true;
            view.InvalidateVisual();
            canvas.CapturePointer(e.Pointer);

            e.Handled = true;
            return;
        }


        // 2) If no node hit fall back to edge endpoint
        var ep = _ec.HitTester.EdgeEndpointUnderPoint(canvasPt: pos, hitRadius: EndpointHitRadius);
        if (ep is { } endpointHit)
        {
            _dragKind = DragKind.EdgeEndpoint;
            _edgeDrag = new EdgeEndpointDrag
            {
                EdgeId = endpointHit.EdgeId,
                MovingTargetEnd = endpointHit.IsTargetEnd,
                FixedEndCanvas = endpointHit.IsTargetEnd
                                  ? _ec.EdgeViews[endpointHit.EdgeId].SourcePoint
                                  : _ec.EdgeViews[endpointHit.EdgeId].TargetPoint,
                StartCanvas = pos
            };

            // Show rubber from fixed to mouse
            _ec.Rubber.ShowRubber(startCanvas: _edgeDrag.FixedEndCanvas, endCanvas: pos);

            e.Handled = true;
            return;
        }

        // 3) Check for edge *segment* hit (middle of edge). If clicked near a segment, prefer edge selection
        var seg = FindTopmostEdgeUnderPoint(pos, EdgeHitDistance);
        if (seg is { } segHit)
        {
            // select the edge visually and do not fall through to node selection
            _ec.Adapter?.SetSelected(nodeId: null, edgeId: segHit.EdgeId, selected: true);
            e.Handled = true;
            return;
        }

        // 4) Clicked empty space: clear selection if you want (optional)
        e.Handled = false;
    }

    /// <summary>
    /// Find the visually topmost edge under the canvas point (by ZIndex then canvas child index).
    /// Returns null when none within hitRadius.
    /// </summary>
    private (EdgeControl View, string EdgeId)? FindTopmostEdgeUnderPoint(Point canvasPt, double hitRadius)
    {
        if (_ec is null) return null;

        // Build ordered list from EdgeViews
        var ordered = new List<(string Id, EdgeControl View)>(_ec.EdgeViews.Count);
        foreach (var kv in _ec.EdgeViews)
            ordered.Add((kv.Key, kv.Value));

        ordered.Sort((a, b) =>
        {
            var zaObj = a.View.GetValue(Panel.ZIndexProperty);
            var zbObj = b.View.GetValue(Panel.ZIndexProperty);
            int za = zaObj is int ia ? ia : 0;
            int zb = zbObj is int ib ? ib : 0;
            if (za != zb) return zb.CompareTo(za); // higher ZIndex first

            int iaIndex = _ec.Canvas.Children.IndexOf(a.View);
            int ibIndex = _ec.Canvas.Children.IndexOf(b.View);
            return ibIndex.CompareTo(iaIndex);
        });

        foreach (var item in ordered)
        {
            var view = item.View;
            if (view is null) continue;
            if (view.HitNearSegment(canvasPt, hitRadius))
                return (view, item.Id);
        }

        return null;
    }

    public void HandleCanvasPointerMoved(PointerEventArgs e)
    {
        if (_dragKind == DragKind.None || _ec?.Canvas == null)
            return;

        var canvas = _ec.Canvas;
        // Use canvas coordinate space consistently
        var pos = e.GetPosition(relativeTo: canvas);

        switch (_dragKind)
        {
            case DragKind.Node:
                {
                    // Move live view for immediate feedback; commit via command on release
                    if ( _ec.NodeViews.TryGetValue(_nodeDrag.NodeId, out var view))
                    {
                        var newTopLeft = pos - _nodeDrag.PressOffset;
                        Canvas.SetLeft(view, newTopLeft.X);
                        Canvas.SetTop(view, newTopLeft.Y);
                        view.InvalidateVisual();

                        // Optional: live edge endpoint updates if you maintain anchors dynamically
                        // (Usually you'd let model changes drive these after commit.)
                        // Live-update all incident edges so they follow as the node is dragged.
                        foreach (var edge in _mc.Model.Edges.Values)
                        {
                            if (edge.SourceNodeId == _nodeDrag.NodeId || edge.TargetNodeId == _nodeDrag.NodeId)
                            {
                                var p0 = _ec.Adapter?.ResolvePinCanvasPoint(_ec, edge.SourceNodeId, defaultSide: +1); // source → right
                                var p1 = _ec.Adapter?.ResolvePinCanvasPoint(_ec, edge.TargetNodeId, defaultSide: -1); // target → left
                                _ec.Adapter.UpdateEdgePoints(edge.Id, p0, p1);
                            }
                        }
                    }
                    e.Handled = true;
                    break;
                }

            case DragKind.EdgeEndpoint:
                {
                    // Update rubber band
                    _ec.Rubber.UpdateRubberEnd(endCanvas: pos);
                    e.Handled = true;
                    break;
                }
        }
    }

    public void HandleCanvasPointerReleased(PointerReleasedEventArgs e)
    {
        if (_ec?.Canvas == null) return;
        var canvas = _ec.Canvas;

        // Use canvas space for final position
        var pos = e.GetPosition(relativeTo: canvas);

        switch (_dragKind)
        {
            case DragKind.Node:
                {
                    CommitNodeMove(endCanvas: pos);
                    e.Handled = true;
                    break;
                }

            case DragKind.EdgeEndpoint:
                {
                    CommitEdgeReconnect(dropCanvas: pos);
                    e.Handled = true;
                    break;
                }
        }

        // Clear state
        _dragKind = DragKind.None;
    }

    // ───────────────────────── Node move ─────────────────────────

    private void CommitNodeMove(Point endCanvas)
    {
        var nodeId = _nodeDrag.NodeId;
        if (!_ec.NodeViews.TryGetValue(nodeId, out var view)) 
        { 
            _ec.Rubber.HideRubber(); 
            return; 
        }

        //var pressOffset = _nodeDrag.PressOffset.ToPoint3(z: 0f);
        var newTopLeft = endCanvas - _nodeDrag.PressOffset;
        var newPos = newTopLeft.ToPoint3();
        //           var newTopLeft = endCanvas - pressOffset;
        //var newPos = new Point3(x: newTopLeft.X, y: newTopLeft.Y);

        // If moved only a pixel or two, you may want to treat as click; here we always commit.
        // TODO: Replace with your actual move command
        var node = _mc.Model.Nodes[nodeId];
        var cmd = new MoveNodeCommand( //
            controller: _mc.Controller,
            node: node,
            newPosition: newPos);

        _mc.Commands.Exec(command: cmd);
    }

    // ───────────────────────── Edge endpoint reconnect ─────────────────────────

    private void CommitEdgeReconnect(Point dropCanvas)
    {
        try
        {
            if (!_mc.Controller.Model.Edges.TryGetValue(_edgeDrag.EdgeId, out var oldEdge))
                return;

            // Hide rubber regardless of outcome
            _ec.Rubber.HideRubber();

            // Did we drop over a node?
            var over = _ec.HitTester.NodeUnderPoint(canvasPt: dropCanvas);
            if (over is null)
            {
                // No-op: you can also revert visuals explicitly if you moved the live edge
                return;
            }

            var dropNodeId = over.Value.NodeId;
            var dropView = over.Value.View;

            // Decide pin direction by local X (left half => Input, right half => Output)
            var local = dropView.TranslatePoint(point: dropCanvas, relativeTo: dropView) ?? new Point(x: 0, y: 0);
            var direction = (local.X < dropView.Bounds.Width / 2.0)
                ? EnumNodePinDirection.Input
                : EnumNodePinDirection.Output;

            // Resolve pin (snap to nearest, or return an InsertPinCommand to create one)
            var (pinId, maybeInsert) = _ec.PinResolver.ResolveForDrop(
                nodeId: dropNodeId,
                dir: direction,
                dropYNodeSpace: local.Y);

            // Work out fixed end (source or target) based on which endpoint was dragged
            var movingTarget = _edgeDrag.MovingTargetEnd;

            var srcNodeId = movingTarget ? oldEdge.SourceNodeId : dropNodeId;
            var srcPinId = movingTarget ? oldEdge.SourcePinId : pinId;

            var dstNodeId = movingTarget ? dropNodeId : oldEdge.TargetNodeId;
            var dstPinId = movingTarget ? pinId : oldEdge.TargetPinId;

            // Build commands: remove old, maybe insert pin, then add new
            var remove = new RemoveEdgeCommand( 
                controller: _mc.Controller,
                edge: oldEdge);

            var add = new AddEdgeCommand(       
                controller:_mc.Controller,
                sNode: srcNodeId,
                sPin: srcPinId,
                tNode: dstNodeId,
                tPin: dstPinId);

            if (maybeInsert is InsertPinCommand insertCmd) 
            {
                var inner = new CompositeCommand(first: remove, second: add);
                var composite = new CompositeCommand(first: insertCmd, second: inner);
                _mc.Commands.Exec(command: composite);
            }
            else
            {
                var composite = new CompositeCommand(first: remove, second: add);
                _mc.Commands.Exec(command: composite);
            }
        }
        finally
        {
            _ec.Rubber.HideRubber();
        }
    }
}
