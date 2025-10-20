using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using DirectedGraphCore.Commands;
using DirectedGraphCore.Controllers;
using DirectedGraphCore.Models;
using DirectedGraphCore.Persistence;
using DirectedGraphEditor.Adapters;
using DirectedGraphEditor.Controls.GraphNodeControl;
using DirectedGraphEditor.Services;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;

namespace DirectedGraphEditor.Pages.Editor;


public partial class EditorPageView : UserControl
{
    // fields
    private readonly CommandStack _commands = new();
    private readonly GraphController _controller;
    private AvaloniaGraphAdapter _adapter;

    private IGraphPersistence _persistence = new GraphPersistence();
    private IFileDialogService _fileDialogs = new StorageProviderFileDialogService();

    public EditorPageView()
    {
        InitializeComponent();

        // however you get/create your model:
        var model = new GraphModel();
        _controller = new GraphController(model);

        // ✅ pass CommandStack here
        _adapter = new AvaloniaGraphAdapter(_controller, _commands);


        // Create adapter when the view is ready to render
        this.AttachedToVisualTree += OnAttachedToVisualTree;
        this.DetachedFromVisualTree += OnDetachedFromVisualTree;

        // Subscribe to controller events to create/remove views
        _controller.NodeAdded += OnNodeAdded;
        _controller.NodeRemoved += OnNodeRemoved;
        _controller.NodeMoved += OnNodeMoved; // optional but good to have

        OpenMenuItem.Click += OnOpenGraph;
        SaveMenuItem.Click += OnSaveGraph;
        SaveAsMenuItem.Click += OnSaveGraphAs;

    }

    private void OnAttachedToVisualTree(object? sender, VisualTreeAttachmentEventArgs e)
    {
        if (DataContext is not EditorPageViewModel vm)
            return;

        // GraphCanvas is the <Canvas x:Name="GraphCanvas"/> in .axaml
        _adapter = new AvaloniaGraphAdapter(vm.Controller, _commands);

    }

    private void OnDetachedFromVisualTree(object? sender, VisualTreeAttachmentEventArgs e)
    {
        // Dispose / unsubscribe when view is removed
        ////_adapter?.Dispose();
        _adapter = null;
    }

    // ─── Canvas event hooks ───────────────────────────────────────
    private void OnCanvasPointerPressed(object? sender, PointerPressedEventArgs e)
        => _adapter?.HandleCanvasPointerPressed(e);

    private void OnCanvasPointerMoved(object? sender, PointerEventArgs e)
        => _adapter?.HandleCanvasPointerMoved(e);

    private void OnCanvasPointerReleased(object? sender, PointerReleasedEventArgs e)
        => _adapter?.HandleCanvasPointerReleased(e);

    ////private void OnLoaded(object? sender, RoutedEventArgs e)
    ////{
    ////    // Ensure the canvas can receive pointer events
    ////    if (GraphCanvas.Background is null)
    ////        GraphCanvas.Background = Brushes.Transparent;


    ////    _adapter?.HandleCanvasLoaded(GraphCanvas);
    ////}

    private void OnLoaded(object? sender, RoutedEventArgs e)
    {
        if (GraphCanvas.Background is null) GraphCanvas.Background = Brushes.Transparent;
        _adapter.HandleCanvasLoaded(GraphCanvas);

        // If model already has nodes (e.g., after you called ReloadFrom), paint them:
        foreach (var node in _controller.Model.Nodes.Values) OnNodeAdded(node);
    }


    private void OnNodeAdded(GraphNode node)
    {
        var view = new GraphNodeControl { DataContext = node };
        // position the control using the model Position:
        view.SetValue(Canvas.LeftProperty, node.Position.X);
        view.SetValue(Canvas.TopProperty, node.Position.Y);
        // optional size if you persist it:
        if (node.Size is GraphSize s) { view.Width = s.Width; view.Height = s.Height; }

        // put it on top and add to canvas
        view.SetValue(Panel.ZIndexProperty, 100);
        GraphCanvas.Children.Add(view);

        // let the adapter know so drag/snap works
        _adapter.RegisterView(node, view);
    }

    private void OnNodeRemoved(GraphNode node)
    {
        // find the view by DataContext (or keep a map if you prefer)
        var toRemove = GraphCanvas.Children
            .OfType<GraphNodeControl>()
            .FirstOrDefault(v => ReferenceEquals(v.DataContext, node));
        if (toRemove != null)
        {
            _adapter.UnregisterView(node);
            GraphCanvas.Children.Remove(toRemove);
        }
    }

    private void OnNodeMoved(GraphNode node)
    {
        var view = GraphCanvas.Children
            .OfType<GraphNodeControl>()
            .FirstOrDefault(v => ReferenceEquals(v.DataContext, node));
        if (view != null)
        {
            view.SetValue(Canvas.LeftProperty, node.Position.X);
            view.SetValue(Canvas.TopProperty, node.Position.Y);
        }
    }

    private async void OnOpenGraph(object? sender, RoutedEventArgs e)
    {
        var top = TopLevel.GetTopLevel(this);
        if (top is null) return;

        var path = await _fileDialogs.ShowOpenGraphAsync(top);
        if (string.IsNullOrWhiteSpace(path)) return;

        var gm = await _persistence.LoadDgmlAsync(path);
        _controller.ReloadFrom(gm);
    }

    private async void OnSaveGraph(object? sender, RoutedEventArgs e)
    {
        var top = TopLevel.GetTopLevel(this);
        if (top is null) return;

        var path = await _fileDialogs.ShowSaveGraphAsync(top);
        if (string.IsNullOrWhiteSpace(path)) return;

        await _persistence.SaveDgmlAsync(_controller.Model, path);
        _controller.Model.FilePath = path;
    }

    private async void OnSaveGraphAs(object? sender, RoutedEventArgs e)
    {

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


