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


////// Drag controller contract your existing DragController should expose
////public interface IDragController
////{
////    void HandleCanvasPointerPressed(PointerPressedEventArgs e);
////    void HandleCanvasPointerMoved(PointerEventArgs e);
////    void HandleCanvasPointerReleased(PointerReleasedEventArgs e);
////}


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

    public void UpdateEdgePoints(string edgeId, Point p0, Point p1)
    {
        if (EdgeViews.TryGetValue(edgeId, out var ec))
        {
            ec.SourcePoint = p0;
            ec.TargetPoint = p1;
            ec.InvalidateVisual();
        }
    }

    public void SetSelected(string? nodeId = null, string? edgeId = null, bool selected = false)
    {
        if (nodeId != null && NodeViews.TryGetValue(nodeId, out var nv)) { nv.IsSelected = selected; nv.InvalidateVisual(); }
        if (edgeId != null && EdgeViews.TryGetValue(edgeId, out var ev)) { ev.IsSelected = selected; ev.InvalidateVisual(); }
    }

    // ----- Event forwarding to DragController (single place for pointer logic)
    public void WireCanvasToDragController(IDragController drag)
    {
        ////_canvas.AddHandler(InputElement.PointerPressedEvent, (s, e) => { _canvas.CapturePointer(e.Pointer); drag.HandleCanvasPointerPressed(e); }, RoutingStrategies.Tunnel | RoutingStrategies.Bubble);
        ////_canvas.AddHandler(InputElement.PointerMovedEvent, (s, e) => { drag.HandleCanvasPointerMoved(e); }, RoutingStrategies.Tunnel | RoutingStrategies.Bubble);
        ////_canvas.AddHandler(InputElement.PointerReleasedEvent, (s, e) => { drag.HandleCanvasPointerReleased(e); _canvas.ReleasePointerCapture(e.Pointer); }, RoutingStrategies.Tunnel | RoutingStrategies.Bubble);
        ///
        _canvas.PointerPressed += (s, e) => drag.HandleCanvasPointerPressed(e);
        _canvas.PointerMoved += (s, e) => drag.HandleCanvasPointerMoved(e);
        _canvas.PointerReleased += (s, e) => drag.HandleCanvasPointerReleased(e);
    }

    // ----- IHitTester (uses the owned visuals so results match what’s on screen)
    public (NodeControl View, string NodeId)? NodeUnderPoint(Point canvasPt)
    {
        // Iterate in reverse z-order so “topmost” wins
        for (int i = _canvas.Children.Count - 1; i >= 0; i--)
        {
            if (_canvas.Children[i] is not NodeControl nc) continue;
            var id = FindNodeIdByView(nc);
            if (id is null) continue;
            var ptLocal = _canvas.TranslatePoint(canvasPt, nc) ?? canvasPt;
            if (new Rect(nc.Bounds.Size).Contains(ptLocal))
                return (nc, id);
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


    ////public GraphAdapter(GraphController controller, CommandStack commands)
    ////{
    ////    this.controller = controller;
    ////    this.commands = commands;

    ////    // Subscribe so we get notified after ReloadFromFile completes (GraphResetCompleted is raised)
    ////    this.controller.GraphResetCompleted += OnGraphResetCompleted;


    ////    ////// create DragController using the adapter's live dictionaries (cast NodeControl -> Control)
    ////    ////// Pass mutable dictionaries so DragController sees updates later when nodes/edges are added.
    ////    ////var nodeViewsAsControl = new Dictionary<string, Control>();
    ////    ////foreach (var kv in nodeViews) nodeViewsAsControl[kv.Key] = kv.Value;

    ////    ////// IMPORTANT: do NOT pass a .ToDictionary(...) of a dictionary that is filled later.
    ////    ////// The best is to pass the actual dictionaries. If DragController expects IDictionary<string,Control>,
    ////    ////// you can store nodeViews as IDictionary<string,Control> or pass a wrapper that maps to the real one.
    ////    ////// For now, create the DragController after initial visuals exist (see OnAttachedToVisualTree/OnLoaded below).

    ////}


    ////// When canvas is available (you already call _ec.Adapter.HandleCanvasLoaded(GraphCanvas) in the view),
    ////// ensure you construct DragController using the adapter dictionaries (not copies).
    ////internal void InitializeDragController(Canvas graphCanvas)
    ////{
    ////    ////// Ensure nodeViews has been populated with current views before creating controller.
    ////    ////// If nodeViews isn't filled yet, create the controller later after first nodes are created.
    ////    ////var nodeViewsAsControl = new Dictionary<string, Control>();
    ////    ////foreach (var kv in nodeViews) 
    ////    ////    nodeViewsAsControl[kv.Key] = kv.Value;

    ////    if (_dragController == null)
    ////    {
    ////        _dragController = new DragController(controller,
    ////                                             nodeViews,   // these are the live maps adapter maintains
    ////                                             edgeLines);           // edgeLines is already Dictionary<string, Line>
    ////    }

    ////    _dragController.HandleCanvasLoaded(graphCanvas);
    ////}

////    // Called when GraphController finishes reset/reload (e.g., after opening a DGML file).
////    // Reinitialize the drag controller so it sees newly-created nodeViews/edgeLines.
////    private void OnGraphResetCompleted()
////    {
////        // GraphResetCompleted might be raised on non-UI thread; ensure canvas access is safe.
////        // If you need to marshal to UI thread, use Dispatcher.UIThread.Post(...) here.
////        if (canvas != null)
////        {
////            // Re-initialize (InitializeDragController is idempotent and will reconnect canvas)
////            InitializeDragController(canvas);
////        }
////    }

////    // ─────────────────────────────────────────────────────────
////    // Canvas / View registration
////    // ─────────────────────────────────────────────────────────

////    public void HandleCanvasLoaded(Canvas c) => AttachCanvas(c);


////    // -------------------- canvas handlers (replace/extend yours) --------------------
////    public void HandleCanvasPointerPressed(PointerPressedEventArgs e)
////    {
////        if (canvas is null) return;
////        var p = e.GetPosition(canvas);

////        // 1) Try edge endpoint first
////        if (TryHitEdgeEndpoint(p, out var edge, out var isTargetEnd, out var fixedPt))
////        {
////            StartEndpointDrag(edge, isTargetEnd, fixedPt, p);
////            canvas.CapturePointer(e.Pointer);  // capture on canvas, not on node
////            e.Handled = true;
////            return;
////        }

////        // 2) Try node body
////        if (TryHitNodeBody(p, out var view, out var node))
////        {
////            StartNodeDrag(view, node, p);
////            canvas.CapturePointer(e.Pointer);
////            e.Handled = true;
////            return;
////        }
////    }


////    public void HandleCanvasPointerMoved(PointerEventArgs e)
////    {
////        if (canvas is null || rubber is null) return;              // only if a drag is active
////        UpdateEdgeDrag(e.GetPosition(canvas));                     // <- reuse your method
////    }


////    public void HandleCanvasPointerReleased(PointerReleasedEventArgs e)
////    {
////        if (canvas is null) return;
////        var p = e.GetPosition(canvas);

////        if (pressState == PressState.DragNode)
////        {
////            pressState = PressState.None;
////            dragNodeView = null;
////            dragNodeModel = null;
////            canvas.ReleasePointerCapture(e.Pointer);
////            return;
////        }

////        if (pressState == PressState.DragEndpoint)
////        {
////            // attempt to snap/attach
////            var targetNode = HitTestNode(p);
////            if (targetNode is null)
////            {
////                // revert to original edge
////                CancelEdgeDrag(); // also clears rubber
////                pressState = PressState.None;
////                dragEdge = null;
////                canvas.ReleasePointerCapture(e.Pointer);
////                return;
////            }

////            // Which side of the node?
////            var view = nodeViews[targetNode.Id];
////            var local = canvas.TranslatePoint(p, view);
////            if (local is null)
////            {
////                CancelEdgeDrag();
////                pressState = PressState.None;
////                dragEdge = null;
////                canvas.ReleasePointerCapture(e.Pointer);
////                return;
////            }
////            bool attachToInputSide = local.Value.X < view.Bounds.Width / 2.0;

////            CompleteEndpointReconnect(targetNode, p, attachToInputSide); // creates commands & executes
////            pressState = PressState.None;
////            dragEdge = null;
////            canvas.ReleasePointerCapture(e.Pointer);
////            return;
////        }

////        // default
////        canvas.ReleasePointerCapture(e.Pointer);
////    }

////    // -------------------- starting drags --------------------
////    private void StartNodeDrag(NodeControl view, GraphNode node, Point startCanvas)
////    {
////        pressState = PressState.DragNode;
////        dragNodeView = view;
////        dragNodeModel = node;
////        dragStartCanvas = startCanvas;
////        startLeft = view.GetValue(Canvas.LeftProperty);
////        startTop = view.GetValue(Canvas.TopProperty);
////    }

////    private void StartEndpointDrag(GraphEdge edge, bool draggingTargetEnd, Point anchoredCanvasPoint, Point initialDragPoint)
////    {
////        pressState = PressState.DragEndpoint;
////        dragEdge = edge;
////        dragEndpointIsTarget = draggingTargetEnd;
////        fixedEndCanvas = anchoredCanvasPoint;

////        // start rubber from fixed point to current pointer
////        EnsureRubber();
////        UpdateRubber(fixedEndCanvas, initialDragPoint);
////    }


////    // -------------------- endpoint reconnection --------------------
////    private void CompleteEndpointReconnect(GraphNode targetNode, Point dropCanvasPt, bool attachToInputSide)
////    {
////        if (dragEdge is null || dragSourceNode is null) { CancelEdgeDrag(); return; }

////        // Determine which end we’re reattaching and which is fixed
////        var sourceNodeId = dragEdge.SourceNodeId;
////        var sourcePinId = dragEdge.SourcePinId;
////        var targetNodeId = dragEdge.TargetNodeId;
////        var targetPinId = dragEdge.TargetPinId;

////        var fixedNode = dragEndpointIsTarget ? controller.Model.Nodes[sourceNodeId] : controller.Model.Nodes[targetNodeId];
////        var fixedPinId = dragEndpointIsTarget ? sourcePinId : targetPinId;
////        var movingWasTarget = dragEndpointIsTarget;

////        // Resolve snap vs insert on the drop node/pin
////        var view = nodeViews[targetNode.Id];
////        var dropInNode = canvas!.TranslatePoint(dropCanvasPt, view);
////        if (dropInNode is null) { CancelEdgeDrag(); return; }

////        var neededDir = attachToInputSide ? EnumNodePinDirection.Input : EnumNodePinDirection.Output;
////        var (resolvedPin, created, insertIndex) = ResolvePinForDrop(targetNode, neededDir, dropInNode.Value.Y);

////        // Build commands:
////        // 1) remove the old edge
////        var remove = new RemoveEdgeCommand(controller, dragEdge);

////        // 2) maybe insert a new pin on the drop node
////        InsertPinCommand? insertCmd = created ? new InsertPinCommand(targetNode, neededDir, insertIndex) : null;

////        // 3) add the new edge (fixed end stays the same, moving end becomes drop node/pin)
////        GraphNode fixedModelNode = fixedNode;
////        NodePin fixedModelPin = fixedModelNode.FindPinById(fixedPinId);

////        var add = movingWasTarget
////            ? new AddEdgeCommand(controller, fixedModelNode, fixedModelPin, targetNode, resolvedPin)   // source fixed → target moves
////            : new AddEdgeCommand(controller, targetNode, resolvedPin, fixedModelNode, fixedModelPin); // target fixed → source moves

////        var composite = new ConnectWithAutoPinCommand(insertCmd, new CompositeCommand(remove, add));
////        commands.Exec(composite);

////        CancelEdgeDrag();
////    }

////    // -------------------- hit testing --------------------
////    private bool TryHitNodeBody(Point canvasPoint, out NodeControl view, out GraphNode node)
////    {
////        view = null!;
////        node = null!;
////        foreach (var kv in nodeViews)
////        {
////            var v = kv.Value;
////            var local = canvas!.TranslatePoint(canvasPoint, v);
////            if (local is null) continue;
////            if (new Rect(v.Bounds.Size).Contains(local.Value))
////            {
////                view = v;
////                node = controller.Model.Nodes[kv.Key];
////                return true;
////            }
////        }
////        return false;
////    }

////    private bool TryHitEdgeEndpoint(Point canvasPoint, out GraphEdge edge, out bool isTargetEnd, out Point fixedEnd)
////    {
////        edge = null!;
////        isTargetEnd = false;
////        fixedEnd = default;

////        foreach (var kv in edgeLines)
////        {
////            var eId = kv.Key;
////            var line = kv.Value;
////            var sp = line.StartPoint;  // source end
////            var tp = line.EndPoint;    // target end

////            if (Distance(canvasPoint, sp) <= EndpointHitRadius)
////            {
////                // dragging SOURCE end → fixed is target
////                edge = controller.Model.Edges[eId];
////                isTargetEnd = false;
////                fixedEnd = tp;
////                return true;
////            }
////            if (Distance(canvasPoint, tp) <= EndpointHitRadius)
////            {
////                // dragging TARGET end → fixed is source
////                edge = controller.Model.Edges[eId];
////                isTargetEnd = true;
////                fixedEnd = sp;
////                return true;
////            }
////        }
////        return false;
////    }

////    private static double Distance(Point a, Point b)
////    {
////        var dx = a.X - b.X; var dy = a.Y - b.Y;
////        return Math.Sqrt(dx * dx + dy * dy);
////    }


////    public void AttachCanvas(Canvas canvas)
////    {
////        this.canvas = canvas;
////    }

////    public void RegisterView(GraphNode node, NodeControl view)
////    {
////        nodeViews[node.Id] = view;

////        // optional: hook control's events if you exposed them
////        view.PinDown += OnPinDown;
////        view.PinUp += OnPinUp;
////        view.PinDrag += OnPinDrag;
////    }

////    public void UnregisterView(GraphNode node)
////    {
////        if (!nodeViews.TryGetValue(node.Id, out var view))
////            return;

////        view.PinDown -= OnPinDown;
////        view.PinUp -= OnPinUp;
////        view.PinDrag -= OnPinDrag;
////        nodeViews.Remove(node.Id);
////    }

////    // ─────────────────────────────────────────────────────────
////    // Node dragging: Public API if you prefer calling directly from the control
////    // ─────────────────────────────────────────────────────────

////    ////public void StartNodeDrag(NodeControl view, GraphNode node, Point startCanvas)
////    ////{
////    ////    dragNodeView = view;
////    ////    dragNodeModel = node;
////    ////    dragStartCanvas = startCanvas;
////    ////    startLeft = view.GetValue(Canvas.LeftProperty);
////    ////    startTop = view.GetValue(Canvas.TopProperty);
////    ////}

////    ////public void HandleCanvasPointerMoved(PointerEventArgs e)
////    ////{
////    ////    if (canvas is null) return;

////    ////    if (rubber != null)
////    ////    {
////    ////        UpdateEdgeDrag(e.GetPosition(canvas));
////    ////        return;
////    ////    }

////    ////    if (dragNodeView != null && dragNodeModel != null)
////    ////    {
////    ////        var p = e.GetPosition(canvas);
////    ////        var dx = p.X - dragStartCanvas.X;
////    ////        var dy = p.Y - dragStartCanvas.Y;

////    ////        var newLeft = startLeft + dx;
////    ////        var newTop = startTop + dy;

////    ////        dragNodeView.SetValue(Canvas.LeftProperty, newLeft);
////    ////        dragNodeView.SetValue(Canvas.TopProperty, newTop);

////    ////        dragNodeModel.Position = new GraphPosition(newLeft, newTop, dragNodeModel.Position.Z);
////    ////        controller.RaiseNodeMoved(dragNodeModel); // or your existing event raiser
////    ////    }
////    ////}

////    ////public void HandleCanvasPointerReleased(PointerReleasedEventArgs e)
////    ////{
////    ////    if (canvas is null) return;

////    ////    if (rubber != null)
////    ////    {
////    ////        var pt = e.GetPosition(canvas);
////    ////        var target = HitTestNode(pt);
////    ////        if (target is null) { CancelEdgeDrag(); return; }
////    ////        var view = nodeViews[target.Id];
////    ////        var local = canvas.TranslatePoint(pt, view);
////    ////        if (local is null) { CancelEdgeDrag(); return; }
////    ////        bool targetIsInputSide = local.Value.X < view.Bounds.Width / 2.0;
////    ////        CompleteEdgeDragOverNode(target, pt, targetIsInputSide);
////    ////        return;
////    ////    }

////    ////    // finish node drag
////    ////    dragNodeView = null;
////    ////    dragNodeModel = null;
////    ////}

////    // ─────────────────────────────────────────────────────────
////    // Edge Dragging: Public API if you prefer calling directly from the control
////    // ─────────────────────────────────────────────────────────
////    public void StartEdgeDrag(GraphNode node, NodePin pin, Point startCanvasPt)
////    {
////        dragSourceNode = node;
////        dragSourcePin = pin;

////        EnsureRubber();
////        UpdateRubber(startCanvasPt, startCanvasPt);
////    }

////    public void UpdateEdgeDrag(Point currentCanvasPt)
////    {
////        if (rubber is null) return;
////        rubber.EndPoint = currentCanvasPt;
////    }

////    /// <summary>Complete over a target node (the control determines which node we’re over).</summary>
////    public void CompleteEdgeDragOverNode(GraphNode targetNode, Point dropCanvasPt, bool targetIsInputSide)
////    {
////        if (dragSourceNode is null || dragSourcePin is null) { CancelEdgeDrag(); return; }

////        // Determine which direction is needed at the target end
////        var neededDir = targetIsInputSide ? EnumNodePinDirection.Input : EnumNodePinDirection.Output;

////        // Convert drop to node space
////        var view = nodeViews[targetNode.Id];

////        var dropInNode = ToLocal(canvas, view, dropCanvasPt);
////        if (dropInNode is null)
////        {
////            CancelEdgeDrag();
////            return;
////        }

////        // Resolve or insert pin at the drop Y
////        var (resolvedPin, created, insertIndex) = ResolvePinForDrop(targetNode, neededDir, dropInNode.Value.Y);

////        // Build commands (insert pin if created) + add edge
////        InsertPinCommand? insertCmd = null;
////        if (created)
////            insertCmd = new InsertPinCommand(targetNode, neededDir, insertIndex);

////        var addEdge = targetIsInputSide
////            ? new AddEdgeCommand(controller, dragSourceNode, dragSourcePin, targetNode, resolvedPin)
////            : new AddEdgeCommand(controller, targetNode, resolvedPin, dragSourceNode, dragSourcePin);

////        var composite = new ConnectWithAutoPinCommand(insertCmd, addEdge);
////        commands.Exec(composite);

////        CancelEdgeDrag();
////    }

////    public void CancelEdgeDrag()
////    {
////        dragSourceNode = null;
////        dragSourcePin = null;
////        RemoveRubber();
////    }

////    // ─────────────────────────────────────────────────────────
////    // Helpers
////    // ─────────────────────────────────────────────────────────
////    private static Point? ToLocal(Visual from, Visual to, Point pt) => from.TranslatePoint(pt, to);
////    private static Point? ToCanvas(Visual from, Canvas c, Point pt) => from.TranslatePoint(pt, c);

////    // ─────────────────────────────────────────────────────────
////    // Control event hooks (if NodeControl exposes them)
////    // ─────────────────────────────────────────────────────────
////    private void OnPinDown(object? sender, PinEventArgs e)
////    {
////        // e.CanvasPoint is the pin center in canvas space (ideal); if you don’t have it, compute below.
////        var startPt = e.CanvasPoint ?? ToCanvasPoint(sender as Visual, e.LocalPoint);
////        StartEdgeDrag(e.Node, e.Pin, startPt);
////    }

////    private void OnPinDrag(object? sender, PinEventArgs e)
////    {
////        var pt = e.CanvasPoint ?? ToCanvasPoint(sender as Visual, e.LocalPoint);
////        UpdateEdgeDrag(pt);
////    }

////    private void OnPinUp(object? sender, PinEventArgs e)
////    {
////        var pt = e.CanvasPoint ?? ToCanvasPoint(sender as Visual, e.LocalPoint);

////        // Figure out which node we’re over (hit-test). If none, cancel.
////        var target = HitTestNode(pt);
////        if (target is null) { CancelEdgeDrag(); return; }

////        // Decide side by comparing X: if cursor is left half → input side; else output side.
////        // (Alternatively expose which side from NodeControl on hover.)
////        var view = nodeViews[target.Id];
////        var local = ToLocal(canvas, view, pt);
////        var targetIsInputSide = local.Value.X < view.Bounds.Width / 2.0;

////        CompleteEdgeDragOverNode(target, pt, targetIsInputSide);
////    }

////    // ─────────────────────────────────────────────────────────
////    // Pin resolution (snap or insert)
////    // ─────────────────────────────────────────────────────────
////    private (NodePin pin, bool created, int insertIndex) ResolvePinForDrop(
////        GraphNode node,
////        EnumNodePinDirection dir,
////        double dropYNodeSpace)
////    {
////        var view = nodeViews[node.Id];
////        var height = Math.Max(1.0, view.Bounds.Height);
////        var pinList = dir == EnumNodePinDirection.Input ? node.Inputs : node.Outputs;
////        var centers = GetPinCenters(view, dir);  // (pin,y) sorted by y

////        // Snap?
////        if (centers.Count > 0)
////        {
////            var nearest = centers
////                .Select(t => (t.pin, dy: Math.Abs(t.y - dropYNodeSpace)))
////                .OrderBy(t => t.dy)
////                .First();

////            if (nearest.dy <= SnapDistance)
////                return (nearest.pin, false, nearest.pin.Index);
////        }

////        // Insert by even-layout math
////        var count = pinList.Count;
////        int index;
////        if (count == 0)
////        {
////            index = 0;
////        }
////        else
////        {
////            var frac = Math.Clamp(dropYNodeSpace / height, 0.0, 1.0);
////            var slot = frac * (count + 1);          // 0..count+1
////            var k = (int)Math.Round(slot);       // nearest slot #
////            index = Math.Clamp(k - 1, 0, count); // 0..count
////        }

////        var created = dir == EnumNodePinDirection.Input
////            ? node.InsertInput(index)
////            : node.InsertOutput(index);

////        return (created, true, index);
////    }

////    private static List<(NodePin pin, double y)> GetPinCenters(NodeControl view, EnumNodePinDirection dir)
////    {
////        var list = new List<(NodePin pin, double y)>();
////        foreach (var e in view.GetVisualDescendants().OfType<Ellipse>())
////        {
////            if (!e.Classes.Contains("pin")) continue;
////            if (e.Tag is not NodePin pin) continue;
////            if (pin.Direction != dir) continue;

////            var center = e.TranslatePoint(new Point(e.Bounds.Width / 2, e.Bounds.Height / 2), view);
////            if (center is null) continue;
////            list.Add((pin, center.Value.Y));
////        }
////        list.Sort((a, b) => a.y.CompareTo(b.y));
////        return list;
////    }

////    // ─────────────────────────────────────────────────────────
////    // Rubber-band helpers
////    // ─────────────────────────────────────────────────────────
////    private void EnsureRubber()
////    {
////        if (canvas is null) return;
////        if (rubber != null) return;
////        rubber = new Line
////        {
////            Stroke = Brushes.Aqua,
////            StrokeThickness = 2,
////            IsHitTestVisible = false
////        };
////        rubber.SetValue(Panel.ZIndexProperty, int.MaxValue);
////        canvas.Children.Add(rubber);
////    }

////    private void UpdateRubber(Point start, Point end)
////    {
////        if (rubber is null) return;
////        rubber.StartPoint = start;
////        rubber.EndPoint = end;
////    }

////    private void RemoveRubber()
////    {
////        if (canvas is null || rubber is null) return;
////        canvas.Children.Remove(rubber);
////        rubber = null;
////    }

////    // ─────────────────────────────────────────────────────────
////    // Hit test and transforms
////    // ─────────────────────────────────────────────────────────
////    private GraphNode? HitTestNode(Point canvasPoint)
////    {
////        if (canvas is null) return null;

////        // Simple linear search; optimize if needed.
////        foreach (var kv in nodeViews)
////        {
////            var view = kv.Value;
////            var local = ToLocal(canvas, view, canvasPoint);
////            if (local is null) continue;
////            if (new Rect(view.Bounds.Size).Contains(local.Value))
////                return controller.Model.Nodes[kv.Key];
////        }
////        return null;
////    }

////    private Point ToCanvasPoint(Visual? from, Point local)
////    {
////        if (from is null || canvas is null) return default;
////        var pt = from.TransformToVisual(canvas)?.Transform(local);
////        return pt ?? default;
////    }
////}
