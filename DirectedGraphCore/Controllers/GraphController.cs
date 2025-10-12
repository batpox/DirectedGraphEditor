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

        // ─── Construction ────────────────────────────────────────
        public GraphController(GraphModel model) => Model = model;

        // ─── Node Operations ─────────────────────────────────────
        public GraphNode AddNode(string id, string name, GraphPosition position)
        {
            var node = new GraphNode( id, name ) {  Position = position };
            Model.Nodes.Add(id, node);
            OnNodeAdded(node);
            return node;
        }

        public void RemoveNode(GraphNode node)
        {
            if (selectedNodes.Remove(node))
                OnSelectionChanged();

            Model.Nodes.Remove(node.Id);
            OnNodeRemoved(node);
        }



        // ─── Edge Operations ─────────────────────────────────────
        public GraphEdge AddEdge(string sourceId, int sourceSlot, string targetId, int targetSlot)
        {
            var sourceNode = Model.FindOrAddNode(sourceId);
            var targetNode = Model.FindOrAddNode(targetId);
            var edge = new GraphEdge(sourceNode, sourceSlot, targetNode, targetSlot);
            Model.Edges.Add(edge.Id, edge);
            OnEdgeAdded(edge);
            return edge;
        }

        public void RemoveEdge(GraphEdge edge)
        {
            Model.Edges.Remove(edge.Id);
            OnEdgeRemoved(edge);
        }

        // ─── Selection ───────────────────────────────────────────
        public void SelectNode(GraphNode node, bool multiSelect = false)
        {
            if (!multiSelect)
                selectedNodes.Clear();

            if (selectedNodes.Add(node))
                OnSelectionChanged();
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

        /// <summary>Remove all children, reset from file using 'path', and then add back.</summary>
        /// <param name="path"></param>
        /// <exception cref="Exception"></exception>
        public void ReloadFromFile(string path)
        {
            GraphReset?.Invoke();

            try
            {
                // Notify removal of current visuals
                foreach (var n in Model.Nodes.Values) OnNodeRemoved(n);
                foreach (var e in Model.Edges.Values) OnEdgeRemoved(e);

                Model.LoadFromDgmlFile(path);

                ClearSelection();

                // Notify new visuals
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
            // notify removals
            foreach (var n in Model.Nodes.Values) OnNodeRemoved(n);
            foreach (var e in Model.Edges.Values) OnEdgeRemoved(e);

            Model.Nodes.Clear();
            Model.Edges.Clear();
            foreach (var kv in src.Nodes) 
                Model.Nodes[kv.Key] = kv.Value;
            foreach (var kv in src.Edges) 
                Model.Edges[kv.Key] = kv.Value;

            ClearSelection();
            foreach (var n in Model.Nodes.Values) OnNodeAdded(n);
            foreach (var e in Model.Edges.Values) OnEdgeAdded(e);
        }



        // ─── Protected Event Raisers ─────────────────────────────
        protected virtual void OnNodeAdded(GraphNode node) => NodeAdded?.Invoke(node);
        protected virtual void OnNodeRemoved(GraphNode node) => NodeRemoved?.Invoke(node);
        protected virtual void OnEdgeAdded(GraphEdge edge) => EdgeAdded?.Invoke(edge);
        protected virtual void OnEdgeRemoved(GraphEdge edge) => EdgeRemoved?.Invoke(edge);
        protected virtual void OnSelectionChanged() => SelectionChanged?.Invoke();
        protected virtual void OnNodeMoved(GraphNode node) => NodeMoved?.Invoke(node);
    }
}
