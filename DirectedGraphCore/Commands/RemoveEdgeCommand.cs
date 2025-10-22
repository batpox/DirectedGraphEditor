// DirectedGraphCore/Commands/RemoveEdgeCommand.cs
using DirectedGraphCore.Controllers;
using DirectedGraphCore.Models;

namespace DirectedGraphCore.Commands
{
    public sealed class RemoveEdgeCommand : IUndoable
    {
        private readonly GraphController ctl;
        private readonly string edgeId;
        private GraphEdge? snapshot;

        public RemoveEdgeCommand(GraphController ctl, GraphEdge edge)
        {
            this.ctl = ctl;
            edgeId = edge.Id;
            snapshot = edge;
        }

        public void Do()
        {
            if (snapshot == null) return;
            ctl.RemoveEdge(snapshot);
        }

        public void Undo()
        {
            if (snapshot == null) return;
            ctl.AddEdge(snapshot.SourceNodeId, snapshot.SourcePinId, snapshot.TargetNodeId, snapshot.TargetPinId);
        }
    }
}
