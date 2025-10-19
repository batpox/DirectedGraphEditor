using Avalonia;
using Avalonia.Controls;
using System.Threading.Tasks;

namespace DirectedGraphEditor.Services
{
    public interface IFileDialogService
    {
        Task<string?> ShowSaveGraphAsync(TopLevel owner); // pick *.dgml (we’ll also write .dgml-layout)
        Task<string?> ShowOpenGraphAsync(TopLevel owner); // accept .dgml or .dgml-layout
    }
}
