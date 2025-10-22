using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.VisualTree;
using AvaloniaEdit.Utils;
using DirectedGraphCore.Commands;
using DirectedGraphCore.Controllers;
using DirectedGraphCore.Models;
using DirectedGraphEditor.Commands;
using DirectedGraphEditor.Controls.GraphNodeControl;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Numerics;

namespace DirectedGraphEditor.Adapters
{
    public sealed class AvaloniaGraphAdapter
    {
        private readonly GraphController controller;
        private readonly CommandStack commands;

        private Canvas? canvas;

        // NodeId -> its view (GraphNodeControl)
        private readonly Dictionary<string, GraphNodeControl> nodeViews = new();
        private readonly Dictionary<string, GraphNodeControl> edgeViews = new();

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
        private GraphNodeControl? dragNodeView;
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


        public AvaloniaGraphAdapter(GraphController controller, CommandStack commands)
        {
            this.controller = controller;
            this.commands = commands;
        }

        // ─────────────────────────────────────────────────────────
        // Canvas / View registration
        // ─────────────────────────────────────────────────────────

        public void HandleCanvasLoaded(Canvas c) => AttachCanvas(c);


        // -------------------- canvas handlers (replace/extend yours) --------------------
        public void HandleCanvasPointerPressed(PointerPressedEventArgs e)
        {
            if (canvas is null) return;
            var p = e.GetPosition(canvas);

            // 1) Try edge endpoint first
            if (TryHitEdgeEndpoint(p, out var edge, out var isTargetEnd, out var fixedPt))
            {
                StartEndpointDrag(edge, isTargetEnd, fixedPt, p);
                canvas.CapturePointer(e.Pointer);  // capture on canvas, not on node
                e.Handled = true;
                return;
            }

            // 2) Try node body
            if (TryHitNodeBody(p, out var view, out var node))
            {
                StartNodeDrag(view, node, p);
                canvas.CapturePointer(e.Pointer);
                e.Handled = true;
                return;
            }
        }


        public void HandleCanvasPointerMoved(PointerEventArgs e)
        {
            if (canvas is null || rubber is null) return;              // only if a drag is active
            UpdateEdgeDrag(e.GetPosition(canvas));                     // <- reuse your method
        }

        ////public void HandleCanvasPointerMoved(PointerEventArgs e)
        ////{
        ////    if (canvas is null) return;
        ////    var p = e.GetPosition(canvas);

        ////    if (pressState == PressState.DragNode && dragNodeView is not null && dragNodeModel is not null)
        ////    {
        ////        var dx = p.X - dragStartCanvas.X;
        ////        var dy = p.Y - dragStartCanvas.Y;

        ////        var newLeft = startLeft + dx;
        ////        var newTop = startTop + dy;

        ////        dragNodeView.SetValue(Canvas.LeftProperty, newLeft);
        ////        dragNodeView.SetValue(Canvas.TopProperty, newTop);

        ////        dragNodeModel.Position = new GraphPosition(newLeft, newTop, dragNodeModel.Position.Z);
        ////        controller.RaiseNodeMoved(dragNodeModel); // expose a public raiser if needed
        ////        return;
        ////    }

        ////    if (pressState == PressState.DragEndpoint)
        ////    {
        ////        // rubber already created in StartEndpointDrag
        ////        UpdateEdgeDrag(p); // your existing method updates the rubber EndPoint
        ////        return;
        ////    }
        ////}

        public void HandleCanvasPointerReleased(PointerReleasedEventArgs e)
        {
            if (canvas is null) return;
            var p = e.GetPosition(canvas);

            if (pressState == PressState.DragNode)
            {
                pressState = PressState.None;
                dragNodeView = null;
                dragNodeModel = null;
                canvas.ReleasePointerCapture(e.Pointer);
                return;
            }

            if (pressState == PressState.DragEndpoint)
            {
                // attempt to snap/attach
                var targetNode = HitTestNode(p);
                if (targetNode is null)
                {
                    // revert to original edge
                    CancelEdgeDrag(); // also clears rubber
                    pressState = PressState.None;
                    dragEdge = null;
                    canvas.ReleasePointerCapture(e.Pointer);
                    return;
                }

                // Which side of the node?
                var view = nodeViews[targetNode.Id];
                var local = canvas.TranslatePoint(p, view);
                if (local is null)
                {
                    CancelEdgeDrag();
                    pressState = PressState.None;
                    dragEdge = null;
                    canvas.ReleasePointerCapture(e.Pointer);
                    return;
                }
                bool attachToInputSide = local.Value.X < view.Bounds.Width / 2.0;

                CompleteEndpointReconnect(targetNode, p, attachToInputSide); // creates commands & executes
                pressState = PressState.None;
                dragEdge = null;
                canvas.ReleasePointerCapture(e.Pointer);
                return;
            }

            // default
            canvas.ReleasePointerCapture(e.Pointer);
        }

        // -------------------- starting drags --------------------
        private void StartNodeDrag(GraphNodeControl view, GraphNode node, Point startCanvas)
        {
            pressState = PressState.DragNode;
            dragNodeView = view;
            dragNodeModel = node;
            dragStartCanvas = startCanvas;
            startLeft = view.GetValue(Canvas.LeftProperty);
            startTop = view.GetValue(Canvas.TopProperty);
        }

        private void StartEndpointDrag(GraphEdge edge, bool draggingTargetEnd, Point anchoredCanvasPoint, Point initialDragPoint)
        {
            pressState = PressState.DragEndpoint;
            dragEdge = edge;
            dragEndpointIsTarget = draggingTargetEnd;
            fixedEndCanvas = anchoredCanvasPoint;

            // start rubber from fixed point to current pointer
            EnsureRubber();
            UpdateRubber(fixedEndCanvas, initialDragPoint);
        }

        ////public void HandleCanvasPointerReleased(PointerReleasedEventArgs e)
        ////{
        ////    if (canvas is null || rubber is null) return;              // not dragging an edge

        ////    if (rubber != null)
        ////    {
        ////        var pt = e.GetPosition(canvas);

        ////        var target = HitTestNode(pt);
        ////        if (target is null) { CancelEdgeDrag(); return; }

        ////        var view = nodeViews[target.Id];

        ////        var local = canvas.TranslatePoint(pt, view);
        ////        if (local is null) { CancelEdgeDrag(); return; }

        ////        bool targetIsInputSide = local.Value.X < view.Bounds.Width / 2.0;
        ////        CompleteEdgeDragOverNode(target, pt, targetIsInputSide);   // <- reuse your method
        ////        return;
        ////    }

        ////    dragNodeView = null;
        ////    dragNodeModel = null;

        ////}

        // -------------------- endpoint reconnection --------------------
        private void CompleteEndpointReconnect(GraphNode targetNode, Point dropCanvasPt, bool attachToInputSide)
        {
            if (dragEdge is null || dragSourceNode is null) { CancelEdgeDrag(); return; }

            // Determine which end we’re reattaching and which is fixed
            var sourceNodeId = dragEdge.SourceNodeId;
            var sourcePinId = dragEdge.SourcePinId;
            var targetNodeId = dragEdge.TargetNodeId;
            var targetPinId = dragEdge.TargetPinId;

            var fixedNode = dragEndpointIsTarget ? controller.Model.Nodes[sourceNodeId] : controller.Model.Nodes[targetNodeId];
            var fixedPinId = dragEndpointIsTarget ? sourcePinId : targetPinId;
            var movingWasTarget = dragEndpointIsTarget;

            // Resolve snap vs insert on the drop node/pin
            var view = nodeViews[targetNode.Id];
            var dropInNode = canvas!.TranslatePoint(dropCanvasPt, view);
            if (dropInNode is null) { CancelEdgeDrag(); return; }

            var neededDir = attachToInputSide ? EnumNodePinDirection.Input : EnumNodePinDirection.Output;
            var (resolvedPin, created, insertIndex) = ResolvePinForDrop(targetNode, neededDir, dropInNode.Value.Y);

            // Build commands:
            // 1) remove the old edge
            var remove = new RemoveEdgeCommand(controller, dragEdge);

            // 2) maybe insert a new pin on the drop node
            InsertPinCommand? insertCmd = created ? new InsertPinCommand(targetNode, neededDir, insertIndex) : null;

            // 3) add the new edge (fixed end stays the same, moving end becomes drop node/pin)
            GraphNode fixedModelNode = fixedNode;
            NodePin fixedModelPin = fixedModelNode.FindPinById(fixedPinId);

            var add = movingWasTarget
                ? new AddEdgeCommand(controller, fixedModelNode, fixedModelPin, targetNode, resolvedPin)   // source fixed → target moves
                : new AddEdgeCommand(controller, targetNode, resolvedPin, fixedModelNode, fixedModelPin); // target fixed → source moves

            var composite = new ConnectWithAutoPinCommand(insertCmd, new CompositeCommand(remove, add));
            commands.Exec(composite);

            CancelEdgeDrag();
        }

        // -------------------- hit testing --------------------
        private bool TryHitNodeBody(Point canvasPoint, out GraphNodeControl view, out GraphNode node)
        {
            view = null!;
            node = null!;
            foreach (var kv in nodeViews)
            {
                var v = kv.Value;
                var local = canvas!.TranslatePoint(canvasPoint, v);
                if (local is null) continue;
                if (new Rect(v.Bounds.Size).Contains(local.Value))
                {
                    view = v;
                    node = controller.Model.Nodes[kv.Key];
                    return true;
                }
            }
            return false;
        }

        private bool TryHitEdgeEndpoint(Point canvasPoint, out GraphEdge edge, out bool isTargetEnd, out Point fixedEnd)
        {
            edge = null!;
            isTargetEnd = false;
            fixedEnd = default;

            foreach (var kv in edgeLines)
            {
                var eId = kv.Key;
                var line = kv.Value;
                var sp = line.StartPoint;  // source end
                var tp = line.EndPoint;    // target end

                if (Distance(canvasPoint, sp) <= EndpointHitRadius)
                {
                    // dragging SOURCE end → fixed is target
                    edge = controller.Model.Edges[eId];
                    isTargetEnd = false;
                    fixedEnd = tp;
                    return true;
                }
                if (Distance(canvasPoint, tp) <= EndpointHitRadius)
                {
                    // dragging TARGET end → fixed is source
                    edge = controller.Model.Edges[eId];
                    isTargetEnd = true;
                    fixedEnd = sp;
                    return true;
                }
            }
            return false;
        }

        private static double Distance(Point a, Point b)
        {
            var dx = a.X - b.X; var dy = a.Y - b.Y;
            return Math.Sqrt(dx * dx + dy * dy);
        }


        public void AttachCanvas(Canvas canvas)
        {
            this.canvas = canvas;
        }

        public void RegisterView(GraphNode node, GraphNodeControl view)
        {
            nodeViews[node.Id] = view;

            // optional: hook control's events if you exposed them
            view.PinDown += OnPinDown;
            view.PinUp += OnPinUp;
            view.PinDrag += OnPinDrag;
        }

        public void UnregisterView(GraphNode node)
        {
            if (!nodeViews.TryGetValue(node.Id, out var view))
                return;

            view.PinDown -= OnPinDown;
            view.PinUp -= OnPinUp;
            view.PinDrag -= OnPinDrag;
            nodeViews.Remove(node.Id);
        }

        // ─────────────────────────────────────────────────────────
        // Node dragging: Public API if you prefer calling directly from the control
        // ─────────────────────────────────────────────────────────

        ////public void StartNodeDrag(GraphNodeControl view, GraphNode node, Point startCanvas)
        ////{
        ////    dragNodeView = view;
        ////    dragNodeModel = node;
        ////    dragStartCanvas = startCanvas;
        ////    startLeft = view.GetValue(Canvas.LeftProperty);
        ////    startTop = view.GetValue(Canvas.TopProperty);
        ////}

        ////public void HandleCanvasPointerMoved(PointerEventArgs e)
        ////{
        ////    if (canvas is null) return;

        ////    if (rubber != null)
        ////    {
        ////        UpdateEdgeDrag(e.GetPosition(canvas));
        ////        return;
        ////    }

        ////    if (dragNodeView != null && dragNodeModel != null)
        ////    {
        ////        var p = e.GetPosition(canvas);
        ////        var dx = p.X - dragStartCanvas.X;
        ////        var dy = p.Y - dragStartCanvas.Y;

        ////        var newLeft = startLeft + dx;
        ////        var newTop = startTop + dy;

        ////        dragNodeView.SetValue(Canvas.LeftProperty, newLeft);
        ////        dragNodeView.SetValue(Canvas.TopProperty, newTop);

        ////        dragNodeModel.Position = new GraphPosition(newLeft, newTop, dragNodeModel.Position.Z);
        ////        controller.RaiseNodeMoved(dragNodeModel); // or your existing event raiser
        ////    }
        ////}

        ////public void HandleCanvasPointerReleased(PointerReleasedEventArgs e)
        ////{
        ////    if (canvas is null) return;

        ////    if (rubber != null)
        ////    {
        ////        var pt = e.GetPosition(canvas);
        ////        var target = HitTestNode(pt);
        ////        if (target is null) { CancelEdgeDrag(); return; }
        ////        var view = nodeViews[target.Id];
        ////        var local = canvas.TranslatePoint(pt, view);
        ////        if (local is null) { CancelEdgeDrag(); return; }
        ////        bool targetIsInputSide = local.Value.X < view.Bounds.Width / 2.0;
        ////        CompleteEdgeDragOverNode(target, pt, targetIsInputSide);
        ////        return;
        ////    }

        ////    // finish node drag
        ////    dragNodeView = null;
        ////    dragNodeModel = null;
        ////}

        // ─────────────────────────────────────────────────────────
        // Edge Dragging: Public API if you prefer calling directly from the control
        // ─────────────────────────────────────────────────────────
        public void StartEdgeDrag(GraphNode node, NodePin pin, Point startCanvasPt)
        {
            dragSourceNode = node;
            dragSourcePin = pin;

            EnsureRubber();
            UpdateRubber(startCanvasPt, startCanvasPt);
        }

        public void UpdateEdgeDrag(Point currentCanvasPt)
        {
            if (rubber is null) return;
            rubber.EndPoint = currentCanvasPt;
        }

        /// <summary>Complete over a target node (the control determines which node we’re over).</summary>
        public void CompleteEdgeDragOverNode(GraphNode targetNode, Point dropCanvasPt, bool targetIsInputSide)
        {
            if (dragSourceNode is null || dragSourcePin is null) { CancelEdgeDrag(); return; }

            // Determine which direction is needed at the target end
            var neededDir = targetIsInputSide ? EnumNodePinDirection.Input : EnumNodePinDirection.Output;

            // Convert drop to node space
            var view = nodeViews[targetNode.Id];

            var dropInNode = ToLocal(canvas, view, dropCanvasPt);
            if (dropInNode is null)
            {
                CancelEdgeDrag();
                return;
            }

            // Resolve or insert pin at the drop Y
            var (resolvedPin, created, insertIndex) = ResolvePinForDrop(targetNode, neededDir, dropInNode.Value.Y);

            // Build commands (insert pin if created) + add edge
            InsertPinCommand? insertCmd = null;
            if (created)
                insertCmd = new InsertPinCommand(targetNode, neededDir, insertIndex);

            var addEdge = targetIsInputSide
                ? new AddEdgeCommand(controller, dragSourceNode, dragSourcePin, targetNode, resolvedPin)
                : new AddEdgeCommand(controller, targetNode, resolvedPin, dragSourceNode, dragSourcePin);

            var composite = new ConnectWithAutoPinCommand(insertCmd, addEdge);
            commands.Exec(composite);

            CancelEdgeDrag();
        }

        public void CancelEdgeDrag()
        {
            dragSourceNode = null;
            dragSourcePin = null;
            RemoveRubber();
        }

        // ─────────────────────────────────────────────────────────
        // Helpers
        // ─────────────────────────────────────────────────────────
        private static Point? ToLocal(Visual from, Visual to, Point p) => from.TranslatePoint(p, to);
        private static Point? ToCanvas(Visual from, Canvas c, Point p) => from.TranslatePoint(p, c);

        // ─────────────────────────────────────────────────────────
        // Control event hooks (if GraphNodeControl exposes them)
        // ─────────────────────────────────────────────────────────
        private void OnPinDown(object? sender, PinEventArgs e)
        {
            // e.CanvasPoint is the pin center in canvas space (ideal); if you don’t have it, compute below.
            var startPt = e.CanvasPoint ?? ToCanvasPoint(sender as Visual, e.LocalPoint);
            StartEdgeDrag(e.Node, e.Pin, startPt);
        }

        private void OnPinDrag(object? sender, PinEventArgs e)
        {
            var pt = e.CanvasPoint ?? ToCanvasPoint(sender as Visual, e.LocalPoint);
            UpdateEdgeDrag(pt);
        }

        private void OnPinUp(object? sender, PinEventArgs e)
        {
            var pt = e.CanvasPoint ?? ToCanvasPoint(sender as Visual, e.LocalPoint);

            // Figure out which node we’re over (hit-test). If none, cancel.
            var target = HitTestNode(pt);
            if (target is null) { CancelEdgeDrag(); return; }

            // Decide side by comparing X: if cursor is left half → input side; else output side.
            // (Alternatively expose which side from GraphNodeControl on hover.)
            var view = nodeViews[target.Id];
            var local = ToLocal(canvas, view, pt);
            var targetIsInputSide = local.Value.X < view.Bounds.Width / 2.0;

            CompleteEdgeDragOverNode(target, pt, targetIsInputSide);
        }

        // ─────────────────────────────────────────────────────────
        // Pin resolution (snap or insert)
        // ─────────────────────────────────────────────────────────
        private (NodePin pin, bool created, int insertIndex) ResolvePinForDrop(
            GraphNode node,
            EnumNodePinDirection dir,
            double dropYNodeSpace)
        {
            var view = nodeViews[node.Id];
            var height = Math.Max(1.0, view.Bounds.Height);
            var pinList = dir == EnumNodePinDirection.Input ? node.Inputs : node.Outputs;
            var centers = GetPinCenters(view, dir);  // (pin,y) sorted by y

            // Snap?
            if (centers.Count > 0)
            {
                var nearest = centers
                    .Select(t => (t.pin, dy: Math.Abs(t.y - dropYNodeSpace)))
                    .OrderBy(t => t.dy)
                    .First();

                if (nearest.dy <= SnapDistance)
                    return (nearest.pin, false, nearest.pin.Index);
            }

            // Insert by even-layout math
            var count = pinList.Count;
            int index;
            if (count == 0)
            {
                index = 0;
            }
            else
            {
                var frac = Math.Clamp(dropYNodeSpace / height, 0.0, 1.0);
                var slot = frac * (count + 1);          // 0..count+1
                var k = (int)Math.Round(slot);       // nearest slot #
                index = Math.Clamp(k - 1, 0, count); // 0..count
            }

            var created = dir == EnumNodePinDirection.Input
                ? node.InsertInput(index)
                : node.InsertOutput(index);

            return (created, true, index);
        }

        private static List<(NodePin pin, double y)> GetPinCenters(GraphNodeControl view, EnumNodePinDirection dir)
        {
            var list = new List<(NodePin pin, double y)>();
            foreach (var e in view.GetVisualDescendants().OfType<Ellipse>())
            {
                if (!e.Classes.Contains("pin")) continue;
                if (e.Tag is not NodePin pin) continue;
                if (pin.Direction != dir) continue;

                var center = e.TranslatePoint(new Point(e.Bounds.Width / 2, e.Bounds.Height / 2), view);
                if (center is null) continue;
                list.Add((pin, center.Value.Y));
            }
            list.Sort((a, b) => a.y.CompareTo(b.y));
            return list;
        }

        // ─────────────────────────────────────────────────────────
        // Rubber-band helpers
        // ─────────────────────────────────────────────────────────
        private void EnsureRubber()
        {
            if (canvas is null) return;
            if (rubber != null) return;
            rubber = new Line
            {
                Stroke = Brushes.Aqua,
                StrokeThickness = 2,
                IsHitTestVisible = false
            };
            rubber.SetValue(Panel.ZIndexProperty, int.MaxValue);
            canvas.Children.Add(rubber);
        }

        private void UpdateRubber(Point start, Point end)
        {
            if (rubber is null) return;
            rubber.StartPoint = start;
            rubber.EndPoint = end;
        }

        private void RemoveRubber()
        {
            if (canvas is null || rubber is null) return;
            canvas.Children.Remove(rubber);
            rubber = null;
        }

        // ─────────────────────────────────────────────────────────
        // Hit test and transforms
        // ─────────────────────────────────────────────────────────
        private GraphNode? HitTestNode(Point canvasPoint)
        {
            if (canvas is null) return null;

            // Simple linear search; optimize if needed.
            foreach (var kv in nodeViews)
            {
                var view = kv.Value;
                var local = ToLocal(canvas, view, canvasPoint);
                if (local is null) continue;
                if (new Rect(view.Bounds.Size).Contains(local.Value))
                    return controller.Model.Nodes[kv.Key];
            }
            return null;
        }

        private Point ToCanvasPoint(Visual? from, Point local)
        {
            if (from is null || canvas is null) return default;
            var pt = from.TransformToVisual(canvas)?.Transform(local);
            return pt ?? default;
        }
    }

}
