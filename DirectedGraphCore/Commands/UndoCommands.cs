using DirectedGraphCore.Controllers;
using DirectedGraphCore.Geometry;
using DirectedGraphCore.Models;

namespace DirectedGraphCore.Commands;

public static class UndoCommands
{
    /// <summary>
    /// Reversible move of a single node. Calls back into GraphController
    /// so invariants and NodeMoved event fire in one place.
    /// </summary>
    public sealed class MoveNodeCommand : IUndoable
    {
        private readonly GraphController _controller;
        private readonly GraphNode _node;

        public string DisplayName { get; } = "Move Node";

        public Point3 OldPosition { get; }
        public Point3 NewPosition { get; private set; }    // mutable to allow coalescing

        public MoveNodeCommand(
            GraphController controller,
            GraphNode node,
            Point3 newPosition)
        {
            _controller = controller;
            _node = node;
            OldPosition = node.Position;
            NewPosition = newPosition;
        }

        public void Do()
        {
            _controller.SetNodePosition(
                node: _node,
                position: NewPosition);
        }

        public void Undo()
        {
            _controller.SetNodePosition(
                node: _node,
                position: OldPosition);
        }

        /// <summary>
        /// Optional: coalesce consecutive moves of the same node (e.g., during a drag).
        /// Returns true if merged.
        /// </summary>
        public bool TryMergeWith(IUndoable other)
        {
            if (other is MoveNodeCommand m &&
                ReferenceEquals(m._node, _node))
            {
                // Keep the original OldPosition; just advance the NewPosition.
                NewPosition = m.NewPosition;
                return true;
            }
            return false;
        }
    }
    public sealed class AddEdgeCommand : IUndoable
    {
        private readonly GraphController controller;
        private readonly string sN, sP, tN, tP;
        private GraphEdge? edge;

        public AddEdgeCommand(GraphController controller, GraphNode sNode, NodePin sPin, GraphNode tNode, NodePin tPin)
        {
            if (this.controller == null)
                return;

            this.controller = this.controller ?? throw new ArgumentNullException(nameof(AddEdgeCommand.controller));
            sN = sNode.Id; sP = sPin.Id; tN = tNode.Id; tP = tPin.Id;
        }

        public AddEdgeCommand(GraphController controller, string sNode, string sPin, string tNode, string tPin)
        {
            if (this.controller == null)
                return;

            this.controller = this.controller ?? throw new ArgumentNullException(nameof(AddEdgeCommand.controller));
            sN = sNode; sP = sPin; tN = tNode; tP = tPin;
        }

        public GraphEdge Edge => edge!;

        public void Do()
        {
            if (controller == null)
                return;
            edge = controller.AddEdge(sN, sP, tN, tP);
        }
        public void Undo() { if (edge != null) controller.RemoveEdge(edge); }
    }

    public sealed class RemoveEdgeCommand : IUndoable
    {
        private readonly GraphController controller;
        private readonly string edgeId;
        private GraphEdge? snapshot;

        public RemoveEdgeCommand(GraphController controller, GraphEdge edge)
        {
            this.controller = controller;
            edgeId = edge.Id;
            snapshot = edge;
        }

        public void Do()
        {
            if (snapshot == null) return;
            controller.RemoveEdge(snapshot);
        }

        public void Undo()
        {
            if (snapshot == null) return;
            controller.AddEdge(snapshot.SourceNodeId, snapshot.SourcePinId, snapshot.TargetNodeId, snapshot.TargetPinId);
        }
    }

    public sealed class InsertPinCommand : IUndoable
    {
        private readonly GraphNode node;
        private readonly EnumNodePinDirection dir;
        private readonly int index;
        private NodePin? created;

        public InsertPinCommand(GraphNode node, EnumNodePinDirection dir, int index)
            => (this.node, this.dir, this.index) = (node, dir, index);

        public NodePin Pin => created!;

        public void Do()
        {
            created = dir == EnumNodePinDirection.Input
                ? node.InsertInput(index)
                : node.InsertOutput(index);
        }

        public void Undo()
        {
            if (created != null)
                node.RemovePin(created);
        }

    }

    public sealed class CompositeCommand : IUndoable
    {
        private readonly IUndoable first;
        private readonly IUndoable second;
        public CompositeCommand(IUndoable first, IUndoable second) { this.first = first; this.second = second; }
        public void Do() { first.Do(); second.Do(); }
        public void Undo() { second.Undo(); first.Undo(); }
    }

    /// <summary>
    /// Composite: optionally inserts a pin, then adds an edge. Undo reverses the order.
    /// </summary>
    public sealed class ConnectWithAutoPinCommand : IUndoable
    {
        private readonly IUndoable? insertPin;  // may be null
        private readonly IUndoable addEdge;

        public ConnectWithAutoPinCommand(IUndoable? insertPin, IUndoable addEdge)
        {
            this.insertPin = insertPin;
            this.addEdge = addEdge;
        }

        public void Do()
        {
            insertPin?.Do();
            addEdge.Do();
        }

        public void Undo()
        {
            addEdge.Undo();
            insertPin?.Undo();
        }
    }

}
