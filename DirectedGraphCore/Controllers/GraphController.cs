using DirectedGraphCore.Models;
using System.Numerics;

namespace DirectedGraphCore.Controllers
{
    public class GraphController
    {
        public GraphModel Model { get; }

        // ─── Events ──────────────────────────────────────────────
        public event Action<GraphNode>? NodeAdded;
        public event Action<GraphNode>? NodeRemoved;
        public event Action<GraphNode>? NodeMoved;

        public event Action<GraphEdge>? EdgeAdded;
        public event Action<GraphEdge>? EdgeRemoved;
        public event Action? SelectionChanged;
        public event Action? GraphReset;
        public event Action? GraphResetCompleted;

        private readonly HashSet<GraphNode> selectedNodes = new();
        public IReadOnlyCollection<GraphNode> SelectedNodes => selectedNodes;

        public GraphController(GraphModel model) => Model = model;

        // ─── Node Ops ────────────────────────────────────────────
        public GraphNode AddNode(string id, string name, GraphPosition position)
        {
            var node = new GraphNode(id, name) { Position = position };
            Model.Nodes.Add(id, node);
            OnNodeAdded(node);
            return node;
        }

        public void RemoveNode(GraphNode node)
        {
            if (selectedNodes.Remove(node))
                OnSelectionChanged();

            // Remove edges touching this node (raise events)
            var touching = Model.Edges.Values
                .Where(e => e.SourceNodeId == node.Id || e.TargetNodeId == node.Id)
                .ToList();
            foreach (var e in touching)
            {
                Model.Edges.Remove(e.Id);
                OnEdgeRemoved(e);
            }

            Model.Nodes.Remove(node.Id);
            OnNodeRemoved(node);
        }

        // ─── Edge Ops ────────────────────────────────────────────
        // Preferred: pass IDs directly
        public GraphEdge AddEdge(string sourceNodeId, string sourcePinId, string targetNodeId, string targetPinId)
        {
            var edge = Model.AddEdge(sourceNodeId, sourcePinId, targetNodeId, targetPinId);
            OnEdgeAdded(edge);
            return edge;
        }

        // Convenience: pass objects (nodes + pins)
        public GraphEdge AddEdge(GraphNode sourceNode, NodePin sourcePin, GraphNode targetNode, NodePin targetPin)
            => AddEdge(sourceNode.Id, sourcePin.Id, targetNode.Id, targetPin.Id);

        public void RemoveEdge(GraphEdge edge)
        {
            if (Model.Edges.Remove(edge.Id))
                OnEdgeRemoved(edge);
        }

        // ─── Selection ───────────────────────────────────────────
        public void SelectNode(GraphNode node, bool multiSelect = false)
        {
            if (!multiSelect) selectedNodes.Clear();
            if (selectedNodes.Add(node)) OnSelectionChanged();
        }

        public void ClearSelection()
        {
            if (selectedNodes.Count == 0) return;
            selectedNodes.Clear();
            OnSelectionChanged();
        }

        // ─── Movement ────────────────────────────────────────────
        public void MoveSelectedBy(Vector2 delta)
        {
            foreach (var node in selectedNodes)
            {
                node.Position = new GraphPosition(
                    node.Position.X + delta.X,
                    node.Position.Y + delta.Y, 0);
                OnNodeMoved(node);
            }
        }

        // ─── Reload ──────────────────────────────────────────────
        public void ReloadFromFile(string path)
        {
            GraphReset?.Invoke();
            try
            {
                foreach (var n in Model.Nodes.Values) OnNodeRemoved(n);
                foreach (var e in Model.Edges.Values) OnEdgeRemoved(e);

                Model.LoadFromDgmlFile(path);
                ClearSelection();

                foreach (var n in Model.Nodes.Values) OnNodeAdded(n);
                foreach (var e in Model.Edges.Values) OnEdgeAdded(e);
            }
            catch (Exception ex)
            {
                throw new Exception($"Path={path}. Err={ex.Message}");
            }
            finally
            {
                GraphResetCompleted?.Invoke();
            }
        }

        public void ReloadFrom(GraphModel src)
        {
            foreach (var n in Model.Nodes.Values) OnNodeRemoved(n);
            foreach (var e in Model.Edges.Values) OnEdgeRemoved(e);

            Model.Nodes.Clear();
            Model.Edges.Clear();
            foreach (var kv in src.Nodes) Model.Nodes[kv.Key] = kv.Value;
            foreach (var kv in src.Edges) Model.Edges[kv.Key] = kv.Value;

            ClearSelection();
            foreach (var n in Model.Nodes.Values) OnNodeAdded(n);
            foreach (var e in Model.Edges.Values) OnEdgeAdded(e);
        }

        public bool TryRemovePin(GraphNode node, NodePin pin, out string reason)
        {
            if (Model.HasEdgesOnPin(node.Id, pin.Id))
            {
                reason = "Pin has attached edges. Remove or reroute those edges first.";
                return false;
            }

            // Safe to remove
            bool removed = node.RemovePin(pin); // your GraphNode helper reindexes that side
            reason = removed ? string.Empty : "Pin not found on node.";
            return removed;
        }

        public int ForceRemovePin(GraphNode node, NodePin pin)
        {
            var toDelete = Model.EdgesOfPin(node.Id, pin.Id).ToList();
            foreach (var e in toDelete) { Model.Edges.Remove(e.Id); OnEdgeRemoved(e); }
            node.RemovePin(pin);
            return toDelete.Count;
        }

        // ─── Event raisers ───────────────────────────────────────
        protected virtual void OnNodeAdded(GraphNode node) => NodeAdded?.Invoke(node);
        protected virtual void OnNodeRemoved(GraphNode node) => NodeRemoved?.Invoke(node);
        protected virtual void OnEdgeAdded(GraphEdge edge) => EdgeAdded?.Invoke(edge);
        protected virtual void OnEdgeRemoved(GraphEdge edge) => EdgeRemoved?.Invoke(edge);
        protected virtual void OnSelectionChanged() => SelectionChanged?.Invoke();
        protected virtual void OnNodeMoved(GraphNode node) => NodeMoved?.Invoke(node);
    }
}
