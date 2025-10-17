using DirectedGraphCore.Commands;

namespace DirectedGraphEditor.Commands;

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
