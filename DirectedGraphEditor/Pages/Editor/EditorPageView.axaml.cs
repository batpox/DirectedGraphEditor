using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Avalonia.VisualTree;
using AvaloniaEdit.Utils;
using DirectedGraphCore.Models;
using DirectedGraphEditor.Adapters;
using DirectedGraphEditor.Controls.GraphNodeControl;
using DirectedGraphEditor.Helpers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace DirectedGraphEditor.Pages.Editor;


public partial class EditorPageView : UserControl
{
    private AvaloniaGraphAdapter? adapter;

    public EditorPageView()
    {
        InitializeComponent();

        // Create adapter when the view is ready to render
        this.AttachedToVisualTree += OnAttachedToVisualTree;
        this.DetachedFromVisualTree += OnDetachedFromVisualTree;

        OpenMenuItem.Click += OnOpenGraph;
        SaveMenuItem.Click += OnSaveGraph;
        SaveAsMenuItem.Click += OnSaveGraphAs;

    }

    private void OnAttachedToVisualTree(object? sender, VisualTreeAttachmentEventArgs e)
    {
        if (DataContext is not EditorPageViewModel vm)
            return;

        // GraphCanvas is the <Canvas x:Name="GraphCanvas"/> in .axaml
        adapter = new AvaloniaGraphAdapter(vm.Controller, GraphCanvas);
    }

    private void OnDetachedFromVisualTree(object? sender, VisualTreeAttachmentEventArgs e)
    {
        // Dispose / unsubscribe when view is removed
        adapter?.Dispose();
        adapter = null;
    }

    // ─── Canvas event hooks ───────────────────────────────────────
    private void OnCanvasPointerPressed(object? sender, PointerPressedEventArgs e)
        => adapter?.HandleCanvasPointerPressed(e);

    private void OnCanvasPointerMoved(object? sender, PointerEventArgs e)
        => adapter?.HandleCanvasPointerMoved(e);

    private void OnCanvasPointerReleased(object? sender, PointerReleasedEventArgs e)
        => adapter?.HandleCanvasPointerReleased(e);

    private void OnLoaded(object? sender, RoutedEventArgs e)
            => adapter?.HandleCanvasLoaded(e);


    private async void OnOpenGraph(object? sender, RoutedEventArgs e)
    {
        string? path = await ShowOpenDialogAsync();
        if (path == null) return;

        if (DataContext is EditorPageViewModel vm)
        {
            vm.OpenGraph(path);
        }
    }

    private async void OnSaveGraph(object? sender, RoutedEventArgs e)
    {
        if (adapter != null)
            await adapter.SaveGraphUsingDialogAsync();
    }

    private async void OnSaveGraphAs(object? sender, RoutedEventArgs e)
    {
        if (adapter != null)
            await adapter.SaveGraphUsingDialogAsync(forceNewFile: true);
    }

    private async Task<string?> ShowOpenDialogAsync()
    {
        var top = TopLevel.GetTopLevel(this);
        if (top?.StorageProvider == null) return null;

        var files = await top.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Open Graph",
            AllowMultiple = false,
            FileTypeFilter = new[] { new FilePickerFileType("DirectedGraphMarkup") { Patterns = new[] { "*.dgml" } } }
        });

        return files.Count == 1 ? files[0].Path.LocalPath : null;
    }

}


