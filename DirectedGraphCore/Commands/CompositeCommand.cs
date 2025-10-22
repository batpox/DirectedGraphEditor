// DirectedGraphEditor/Commands/CompositeCommand.cs
using DirectedGraphCore.Commands;

namespace DirectedGraphEditor.Commands
{
    public sealed class CompositeCommand : IUndoable
    {
        private readonly IUndoable first;
        private readonly IUndoable second;
        public CompositeCommand(IUndoable first, IUndoable second) { this.first = first; this.second = second; }
        public void Do() { first.Do(); second.Do(); }
        public void Undo() { second.Undo(); first.Undo(); }
    }
}
