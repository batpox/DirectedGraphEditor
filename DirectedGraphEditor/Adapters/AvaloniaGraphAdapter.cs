using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Media;
using Avalonia.VisualTree;
using DirectedGraphCore.Commands;
using DirectedGraphCore.Controllers;
using DirectedGraphCore.Models;
using DirectedGraphEditor.Commands;
using DirectedGraphEditor.Controls.GraphNodeControl;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DirectedGraphEditor.Adapters
{
    public sealed class AvaloniaGraphAdapter
    {
        private readonly GraphController controller;
        private readonly CommandStack commands;

        private Canvas? canvas;

        // NodeId -> its view (GraphNodeControl)
        private readonly Dictionary<string, GraphNodeControl> nodeViews = new();

        // Drag state
        private GraphNode? dragSourceNode;
        private NodePin? dragSourcePin;
        private Line? rubber;

        // Snap constants
        private const double PinRadiusPx = 5.0;  // Ellipse 10x10
        private const double SnapDistance = PinRadiusPx * 4;  // two diameters

        public AvaloniaGraphAdapter(GraphController controller, CommandStack commands)
        {
            this.controller = controller;
            this.commands = commands;
        }

        // ─────────────────────────────────────────────────────────
        // Canvas / View registration
        // ─────────────────────────────────────────────────────────
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
        // Public API if you prefer calling directly from the control
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
            var list = new List<(NodePin, double)>();
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
