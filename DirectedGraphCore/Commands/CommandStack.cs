namespace DirectedGraphCore.Commands;

public sealed class CommandStack
{
    private readonly Stack<IUndoable> undo = new();
    private readonly Stack<IUndoable> redo = new();

    public bool CanUndo => undo.Count > 0;
    public bool CanRedo => redo.Count > 0;

    public void Exec(IUndoable cmd)
    {
        cmd.Do();
        undo.Push(cmd);
        redo.Clear();
    }

    public void Undo()
    {
        if (!CanUndo) return;
        var cmd = undo.Pop();
        cmd.Undo();
        redo.Push(cmd);
    }

    public void Redo()
    {
        if (!CanRedo) return;
        var cmd = redo.Pop();
        cmd.Do();
        undo.Push(cmd);
    }
}
