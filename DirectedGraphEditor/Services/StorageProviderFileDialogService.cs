using Avalonia;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DirectedGraphEditor.Services;

public sealed class StorageProviderFileDialogService : IFileDialogService
{
    private static readonly FilePickerFileType DgmlType = new("DGML graph (*.dgml)")
    {
        Patterns = new[] { "*.dgml" },
        AppleUniformTypeIdentifiers = new[] { "public.xml" } // harmless fallback
    };

    private static readonly FilePickerFileType DgmlLayoutType = new("DGML layout (*.dgml-layout)")
    {
        Patterns = new[] { "*.dgml-layout" }
    };

    private static readonly FilePickerFileType AllType = new("All files")
    {
        Patterns = new[] { "*.*" }
    };

    public async Task<string?> ShowSaveGraphAsync(TopLevel owner)
    {
        var sp = owner.StorageProvider;
        if (sp is null) return null;

        var result = await sp.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Save Graph",
            SuggestedFileName = "graph.dgml",
            DefaultExtension = "dgml",
            ShowOverwritePrompt = true,
            FileTypeChoices = new List<FilePickerFileType> { DgmlType, AllType }
        });

        if (result is null) return null;

        // Prefer local filesystem path (desktop). For sandboxed platforms you’d stream instead.
        return result.Path?.LocalPath;
    }

    public async Task<string?> ShowOpenGraphAsync(TopLevel owner)
    {
        var sp = owner.StorageProvider;
        if (sp is null) return null;

        var results = await sp.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Open Graph",
            AllowMultiple = false,
            FileTypeFilter = new List<FilePickerFileType> { DgmlType, DgmlLayoutType, AllType }
        });

        var file = results?.FirstOrDefault();
        if (file is null) return null;

        return file.Path?.LocalPath;
    }
}
