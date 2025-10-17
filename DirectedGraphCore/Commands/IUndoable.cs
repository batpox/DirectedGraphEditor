namespace DirectedGraphCore.Commands;

public interface IUndoable
{
    void Do();
    void Undo();
}