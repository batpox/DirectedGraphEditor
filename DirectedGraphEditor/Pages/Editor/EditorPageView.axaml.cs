using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using DirectedGraphCore.Commands;
using DirectedGraphCore.Controllers;
using DirectedGraphCore.Geometry;
using DirectedGraphCore.Models;
using DirectedGraphCore.Persistence;
using DirectedGraphEditor.Adapters;
using DirectedGraphEditor.Controls;
using DirectedGraphEditor.Interaction;
using DirectedGraphEditor.Services;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;

namespace DirectedGraphEditor.Pages.Editor;


public partial class EditorPageView : UserControl
{
    // fields
    private ModelContext? _mc;
    private EditorContext? _ec;
    private DragController? _drag;

    private IFileDialogService _fileDialogs = new StorageProviderFileDialogService();
    private IGraphPersistence _persistence = new GraphPersistence();

    /// <summary>
    /// Constructor
    /// </summary>
    public EditorPageView()
    {
        InitializeComponent();

        // View lifecycle
        AttachedToVisualTree += OnAttachedToVisualTree;
        DetachedFromVisualTree += OnDetachedFromVisualTree;

        // Wire lifecycle
        AttachedToVisualTree += OnAttachedToVisualTree;
        DetachedFromVisualTree += OnDetachedFromVisualTree;

        // Wire menu actions in code (no XAML changes required)
        var openMi = this.FindControl<MenuItem>("OpenMenuItem");
        if (openMi != null)
            openMi.Click += OnOpenGraph;

        var saveMi = this.FindControl<MenuItem>("SaveMenuItem");
        if (saveMi != null)
            saveMi.Click += OnSaveGraph;

        var saveAsMi = this.FindControl<MenuItem>("SaveAsMenuItem");
        if (saveAsMi != null)
            saveAsMi.Click += OnSaveGraphAs;
    }



    private void OnAttachedToVisualTree(object? sender, VisualTreeAttachmentEventArgs e)
    {
        // Ensure a visible hit target
        if (GraphCanvas.Background is null)
            GraphCanvas.Background = Brushes.Transparent;

        // Core context
        var model = new GraphModel();
        var controller = new GraphController(model);
        var commands = new CommandStack();

        _mc = new ModelContext
        {
            Model = model,
            Controller = controller,
            Commands = commands
        };

        // ✅ Editor context
        var adapter = new GraphAdapter(canvas: GraphCanvas);
        ////var rubber = new IRubberHost(GraphCanvas);

        _ec = new EditorContext
        {
            Canvas = GraphCanvas,
            Adapter = adapter,
            NodeViews = adapter.NodeViews,
            EdgeViews = adapter.EdgeViews,
            HitTester = adapter,
            PinResolver = adapter,
            Rubber = adapter
        };


        // DragControll 
        _drag = new DragController(_mc, _ec);

        adapter.WireCanvasToDragController(drag: _drag );

        ////// Create adapter when the view is ready to render
        ////this.AttachedToVisualTree += OnAttachedToVisualTree;
        ////this.DetachedFromVisualTree += OnDetachedFromVisualTree;

        WireModelEvents(controller);

        PaintAll();

    }

    private void OnDetachedFromVisualTree(object? sender, VisualTreeAttachmentEventArgs e)
    {
        // Dispose / unsubscribe when view is removed
        ////_ec.Adapter?.Dispose();
        _ec.Adapter = null;
        _drag = null;
    }

    private void WireModelEvents(GraphController controller)
    {
        controller.NodeAdded += OnNodeAdded;
        controller.NodeMoved += OnNodeMoved;
        controller.NodeRemoved += OnNodeRemoved;
        controller.EdgeAdded += OnEdgeAdded;
        //controller.EdgeChanged += OnEdgeChanged;
        controller.EdgeRemoved += OnEdgeRemoved;
    }
    private void UnwireModelEvents(GraphController controller)
    {
        controller.NodeAdded -= OnNodeAdded;
        controller.NodeRemoved -= OnNodeRemoved;
        controller.NodeMoved -= OnNodeMoved;

        controller.EdgeAdded -= OnEdgeAdded;
        controller.EdgeRemoved -= OnEdgeRemoved;
        // controller.EdgeChanged -= OnEdgeChanged;
    }

    // Paint helpers
    private void PaintAll()
    {
        if (_mc is null || _ec is null) return;

        foreach (var n in _mc.Model.Nodes.Values)
            CreateOrUpdateNodeView(n);

        foreach (var eEdge in _mc.Model.Edges.Values)
            CreateOrUpdateEdgeView(eEdge);
    }




    // ───────────────────────── Model → View (through adapter) ─────────────────────────

    private void OnNodeAdded(GraphNode node) => CreateOrUpdateNodeView(node);
    private void OnNodeMoved(GraphNode node)
    {
        // Move the visual; if edges depend on node anchors, refresh those edges too.
        CreateOrUpdateNodeView(node);
        if (_mc is null) return;

        foreach (var edge in _mc.Model.Edges.Values)
        {
            if (edge.SourceNodeId == node.Id || edge.TargetNodeId == node.Id)
                CreateOrUpdateEdgeView(edge);
        }
    }
    private void OnNodeRemoved(GraphNode node)
    {
        if (_ec is null) return;
        _ec.Adapter.RemoveNodeView(nodeId: node.Id);
    }

    private void OnEdgeAdded(GraphEdge edge) => CreateOrUpdateEdgeView(edge);
    private void OnEdgeRemoved(GraphEdge edge)
    {
        if (_ec is null) return;
        _ec.Adapter.RemoveEdgeView(edgeId: edge.Id);
    }


    // ─────────────────────────────────────────────────────────────────────────────
    // View creation / updates
    // ─────────────────────────────────────────────────────────────────────────────

    private void CreateOrUpdateNodeView(GraphNode node)
    {
        if (_ec is null) return;

        // Default size if model doesn't carry one
        var size = (node.Size is Size3 s)
            ? new Size(width: s.Width, height: s.Height)
            : new Size(width: 140, height: 60);

        if (_ec.NodeViews.TryGetValue(node.Id, out var view))
        {
            // Update position/size/title in place
            Avalonia.Controls.Canvas.SetLeft(view, node.Position.X);
            Avalonia.Controls.Canvas.SetTop(view, node.Position.Y);
            view.Width = size.Width;
            view.Height = size.Height;
            //view.Title = node.Label; // Adapt to your model naming
            view.InvalidateVisual();
        }
        else
        {
            _ec.Adapter.CreateNodeView(
                nodeId: node.Id,
                x: node.Position.X,
                y: node.Position.Y,
                size: size,
                //title: node.Label,
                dataContext: node);
        }
    }

    private void CreateOrUpdateEdgeView(GraphEdge edge)
    {
        if (_ec is null) return;

        // Compute endpoints in canvas space (swap in your real pin-anchor math)
        var p0 = ResolvePinCanvasPoint(
            nodeId: edge.SourceNodeId,
            pinId: edge.SourcePinId,
            defaultSide: -1); // left by default

        var p1 = ResolvePinCanvasPoint(
            nodeId: edge.TargetNodeId,
            pinId: edge.TargetPinId,
            defaultSide: +1); // right by default

        if (_ec.EdgeViews.ContainsKey(edge.Id))
        {
            _ec.Adapter.UpdateEdgePoints(
                edgeId: edge.Id,
                p0: p0,
                p1: p1);
        }
        else
        {
            _ec.Adapter.CreateEdgeView(
                edgeId: edge.Id,
                p0: p0,
                p1: p1,
                dataContext: edge);
        }
    }

    /// <summary>
    /// Resolves a pin's canvas anchor. Replace with your precise per-pin layout.
    /// </summary>
    private Point ResolvePinCanvasPoint(string nodeId, string pinId, int defaultSide)
    {
        if (_ec is null) return new Point(x: 0, y: 0);
        if (!_ec.NodeViews.TryGetValue(nodeId, out var nodeView))
            return new Point(x: 0, y: 0);

        var left = Avalonia.Controls.Canvas.GetLeft(nodeView);
        var top = Avalonia.Controls.Canvas.GetTop(nodeView);

        // Default: mid-left or mid-right of the node card
        var px = defaultSide < 0 ? left : left + nodeView.Bounds.Width;
        var py = top + nodeView.Bounds.Height / 2;

        // TODO: If your model stores per-pin Y offsets, apply them here using pinId.

        return new Point(x: px, y: py);
    }
    ////private async void OnOpenGraph(object? sender, RoutedEventArgs e)
    ////{
    ////    var top = TopLevel.GetTopLevel(this);
    ////    if (top is null) return;

    ////    var path = await _fileDialogs.ShowOpenGraphAsync(top);
    ////    if (string.IsNullOrWhiteSpace(path)) return;

    ////    var gm = await _persistence.LoadDgmlAsync(path);
    ////    _mc.Controller.ReloadFrom(gm);
    ////}
    private async void OnOpenGraph(object? sender, RoutedEventArgs e)
    {
        var top = TopLevel.GetTopLevel(this);
        if (top is null) return;

        var path = await _fileDialogs.ShowOpenGraphAsync(top);
        if (string.IsNullOrWhiteSpace(path)) return;

        // If DataContext is the VM, prefer MVVM path: set CurrentFile and invoke LoadGraphCommand
        if (this.DataContext is EditorPageViewModel vm)
        {
            vm.CurrentFile = path;

            if (vm.LoadGraphCommand is ICommand loadCmd && loadCmd.CanExecute(null))
            {
                loadCmd.Execute(null);
                return;
            }
        }

        // Fallback: direct persistence -> controller (keeps previous behavior)
        var gm = await _persistence.LoadDgmlAsync(path);
        _mc?.Controller.ReloadFrom(gm);
    }


    private async void OnSaveGraph(object? sender, RoutedEventArgs e)
    {
        // Prefer VM command path when DataContext is set
        if (this.DataContext is EditorPageViewModel vm)
        {
            // If VM already has a file, call SaveGraphCommand
            if (!string.IsNullOrWhiteSpace(vm.CurrentFile))
            {
                if (vm.SaveGraphCommand is ICommand saveCmd && saveCmd.CanExecute(null))
                {
                    saveCmd.Execute(null);
                    return;
                }
            }

            // Otherwise show Save dialog and call SaveGraphAsCommand if available
            var top = TopLevel.GetTopLevel(this);
            if (top is null) return;

            var path = await _fileDialogs.ShowSaveGraphAsync(top);
            if (string.IsNullOrWhiteSpace(path)) return;

            if (vm.SaveGraphAsCommand is ICommand saveAsCmd && saveAsCmd.CanExecute(path))
            {
                saveAsCmd.Execute(path);
                return;
            }

            // Fallback: use persistence directly
            await _persistence.SaveDgmlAsync(_mc!.Controller.Model, path);
            _mc.Controller.Model.FilePath = path;
            return;
        }
    }


    ////private async void OnSaveGraphAs(object? sender, RoutedEventArgs e)
    ////{

    ////}

    private async void OnSaveGraphAs(object? sender, RoutedEventArgs e)
    {
        var top = TopLevel.GetTopLevel(this);
        if (top is null) return;

        var path = await _fileDialogs.ShowSaveGraphAsync(top);
        if (string.IsNullOrWhiteSpace(path)) return;

        // Prefer MVVM path when DataContext is the VM
        if (this.DataContext is EditorPageViewModel vm)
        {
            if (vm.SaveGraphAsCommand is ICommand saveAsCmd && saveAsCmd.CanExecute(path))
            {
                saveAsCmd.Execute(path);
                return;
            }

            // Otherwise, set CurrentFile and invoke SaveGraph
            vm.CurrentFile = path;
            if (vm.SaveGraphCommand is ICommand saveCmd && saveCmd.CanExecute(null))
            {
                saveCmd.Execute(null);
                return;
            }
        }

        // Fallback to direct persistence
        await _persistence.SaveDgmlAsync(_mc!.Controller.Model, path);
        _mc.Controller.Model.FilePath = path;
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


