using DirectedGraphCore.Models;

namespace DirectedGraphCore.Commands;

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
