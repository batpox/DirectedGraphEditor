using DirectedGraphCore.Controllers;
using DirectedGraphCore.Models;

namespace DirectedGraphCore.Commands;
public class AddEdgeCommand : IUndoable
{
    private readonly GraphController ctl; private readonly string sN, sP, tN, tP;
    private GraphEdge? edge;
    public AddEdgeCommand(GraphController ctl, GraphNode sNode, NodePin sPin, GraphNode tNode, NodePin tPin)
    { this.ctl = ctl; sN = sNode.Id; sP = sPin.Id; tN = tNode.Id; tP = tPin.Id; }
    public void Do() => edge = ctl.AddEdge(sN, sP, tN, tP);
    public void Undo() { if (edge != null) ctl.RemoveEdge(edge); }
}
