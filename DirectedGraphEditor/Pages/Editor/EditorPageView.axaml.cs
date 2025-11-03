////using Avalonia.Controls;
////using Avalonia.Interactivity;
////using Avalonia.Media;
////using Avalonia.Platform.Storage;
////using DirectedGraphCore.Commands;
////using DirectedGraphCore.Controllers;
////using DirectedGraphCore.Geometry;
////using DirectedGraphCore.Models;
////using DirectedGraphCore.Persistence;
////using DirectedGraphEditor.Adapters;
////using DirectedGraphEditor.Interaction;
////using DirectedGraphEditor.Services;
////using System.Threading.Tasks;
////using System.Windows.Input;

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

    // keep references so we can unwire
    private EditorPageViewModel? _vm;
    private GraphController? _wiredController;

    private IFileDialogService _fileDialogs = new StorageProviderFileDialogService();
    private IGraphPersistence _persistence = new GraphPersistence();

    /// <summary>
    /// Constructor
    /// </summary>
    public EditorPageView()
    {
        InitializeComponent();

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

        // Prefer the ViewModel's controller/model when DataContext is an EditorPageViewModel.
        if (this.DataContext is EditorPageViewModel existingVm)
        {
            _vm = existingVm;
        }
        else
        {
            // Create VM and set DataContext from code-behind (no AXAML change required).
            _vm = new EditorPageViewModel();
            this.DataContext = _vm;
        }

        var controller = _vm.Controller;
        var model = controller.Model;

        // remember which controller we wired so we can Unwire on detach.
        _wiredController = controller;

        var commands = new CommandStack();

        _mc = new ModelContext
        {
            Model = model,
            Controller = controller,
            Commands = commands
        };

        // Editor context / adapter
        var adapter = new GraphAdapter(canvas: GraphCanvas);

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

        // DragController 
        _drag = new DragController(_mc, _ec);
        adapter.WireCanvasToDragController(drag: _drag);

        // Wire model events from the active controller so view reacts when VM/controller changes the model.
        WireModelEvents(controller);

        // Paint whatever the model currently contains
        PaintAll();
    }

    private void OnDetachedFromVisualTree(object? sender, VisualTreeAttachmentEventArgs e)
    {
        // Unwire events from the controller we originally wired
        if (_wiredController != null)
        {
            UnwireModelEvents(_wiredController);
            _wiredController = null;
        }

        // Dispose / release view-side objects
        if (_ec != null)
        {
            _ec.Adapter = null;
            _ec = null;
        }

        _drag = null;
        _mc = null;

        // Optionally keep DataContext (VM) alive; do not null it here unless you want to.
    }

    private void WireModelEvents(GraphController controller)
    {
        if (controller == null) return;
        controller.NodeAdded += OnNodeAdded;
        controller.NodeMoved += OnNodeMoved;
        controller.NodeRemoved += OnNodeRemoved;
        controller.EdgeAdded += OnEdgeAdded;
        controller.EdgeRemoved += OnEdgeRemoved;
    }

    private void UnwireModelEvents(GraphController controller)
    {
        if (controller == null) return;
        controller.NodeAdded -= OnNodeAdded;
        controller.NodeRemoved -= OnNodeRemoved;
        controller.NodeMoved -= OnNodeMoved;
        controller.EdgeAdded -= OnEdgeAdded;
        controller.EdgeRemoved -= OnEdgeRemoved;
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

        var size = (node.Size is Size3 s)
            ? new Size(width: s.Width, height: s.Height)
            : new Size(width: 140, height: 60);

        if (_ec.NodeViews.TryGetValue(node.Id, out var view))
        {
            Avalonia.Controls.Canvas.SetLeft(view, node.Position.X);
            Avalonia.Controls.Canvas.SetTop(view, node.Position.Y);
            view.Width = size.Width;
            view.Height = size.Height;
            view.InvalidateVisual();
        }
        else
        {
            _ec.Adapter.CreateNodeView(
                nodeId: node.Id,
                x: node.Position.X,
                y: node.Position.Y,
                size: size,
                dataContext: node);
        }
    }

    private void CreateOrUpdateEdgeView(GraphEdge edge)
    {
        if (_ec is null) return;

        var p0 = ResolvePinCanvasPoint(
            nodeId: edge.SourceNodeId,
            pinId: edge.SourcePinId,
            defaultSide: -1);

        var p1 = ResolvePinCanvasPoint(
            nodeId: edge.TargetNodeId,
            pinId: edge.TargetPinId,
            defaultSide: +1);

        if (_ec.EdgeViews.ContainsKey(edge.Id))
        {
            _ec.Adapter.UpdateEdgePoints(edgeId: edge.Id, p0: p0, p1: p1);
        }
        else
        {
            _ec.Adapter.CreateEdgeView(edgeId: edge.Id, p0: p0, p1: p1, dataContext: edge);
        }
    }

    private Point ResolvePinCanvasPoint(string nodeId, string pinId, int defaultSide)
    {
        if (_ec is null) return new Point(0, 0);
        if (!_ec.NodeViews.TryGetValue(nodeId, out var nodeView))
            return new Point(0, 0);

        var left = Avalonia.Controls.Canvas.GetLeft(nodeView);
        var top = Avalonia.Controls.Canvas.GetTop(nodeView);

        var px = defaultSide < 0 ? left : left + nodeView.Bounds.Width;
        var py = top + nodeView.Bounds.Height / 2;

        return new Point(px, py);
    }

    private async void OnOpenGraph(object? sender, RoutedEventArgs e)
    {
        var top = TopLevel.GetTopLevel(this);
        if (top is null) return;

        var path = await _fileDialogs.ShowOpenGraphAsync(top);
        if (string.IsNullOrWhiteSpace(path)) return;

        // Delegate to VM so model operations and events are centralized
        if (this.DataContext is EditorPageViewModel vm)
        {
            vm.CurrentFile = path;
            if (vm.LoadGraphCommand is ICommand loadCmd && loadCmd.CanExecute(null))
            {
                loadCmd.Execute(null);
                return;
            }

            // fallback: call method
            vm.OpenGraph(path);
            return;
        }

        // ultimate fallback: direct persistence -> controller
        var gm = await _persistence.LoadDgmlAsync(path);
        _mc?.Controller.ReloadFrom(gm);
    }

    private async void OnSaveGraph(object? sender, RoutedEventArgs e)
    {
        if (this.DataContext is EditorPageViewModel vm)
        {
            if (!string.IsNullOrWhiteSpace(vm.CurrentFile))
            {
                if (vm.SaveGraphCommand is ICommand saveCmd && saveCmd.CanExecute(null))
                {
                    saveCmd.Execute(null);
                    return;
                }
            }

            var top = TopLevel.GetTopLevel(this);
            if (top is null) return;
            var path = await _fileDialogs.ShowSaveGraphAsync(top);
            if (string.IsNullOrWhiteSpace(path)) return;

            if (vm.SaveGraphAsCommand is ICommand saveAsCmd && saveAsCmd.CanExecute(path))
            {
                saveAsCmd.Execute(path);
                return;
            }

            await _persistence.SaveDgmlAsync(_mc!.Controller.Model, path);
            _mc.Controller.Model.FilePath = path;
            return;
        }

        // Fallback: nothing to do
    }

    private async void OnSaveGraphAs(object? sender, RoutedEventArgs e)
    {
        var top = TopLevel.GetTopLevel(this);
        if (top is null) return;

        var path = await _fileDialogs.ShowSaveGraphAsync(top);
        if (string.IsNullOrWhiteSpace(path)) return;

        if (this.DataContext is EditorPageViewModel vm)
        {
            if (vm.SaveGraphAsCommand is ICommand saveAsCmd && saveAsCmd.CanExecute(path))
            {
                saveAsCmd.Execute(path);
                return;
            }

            vm.CurrentFile = path;
            if (vm.SaveGraphCommand is ICommand saveCmd && saveCmd.CanExecute(null))
            {
                saveCmd.Execute(null);
                return;
            }
        }

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