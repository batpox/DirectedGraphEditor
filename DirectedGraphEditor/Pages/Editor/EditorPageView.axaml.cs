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

        // Create adapter when the view is ready to render
        this.AttachedToVisualTree += OnAttachedToVisualTree;
        this.DetachedFromVisualTree += OnDetachedFromVisualTree;

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




    ////// ─── Canvas event hooks ───────────────────────────────────────
    ////private void OnCanvasPointerPressed(object? sender, PointerPressedEventArgs e)
    ////    => _ec.Adapter?.HandleCanvasPointerPressed(e);

    ////private void OnCanvasPointerMoved(object? sender, PointerEventArgs e)
    ////    => _ec.Adapter?.HandleCanvasPointerMoved(e);

    ////private void OnCanvasPointerReleased(object? sender, PointerReleasedEventArgs e)
    ////    => _ec.Adapter?.HandleCanvasPointerReleased(e);


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

    // If present in your controller:
    // private void OnEdgeChanged(GraphEdge edge) => CreateOrUpdateEdgeView(edge);



    ////private void OnLoaded(object? sender, RoutedEventArgs e)
    ////{
    ////    if (GraphCanvas.Background is null) 
    ////        GraphCanvas.Background = Brushes.Transparent;

    ////    _ec.Adapter.HandleCanvasLoaded(GraphCanvas);

    ////    // If model already has nodes (e.g., after you called ReloadFrom), paint them:
    ////    foreach (var node in _mc.Controller.Model.Nodes.Values)
    ////        OnNodeAdded(node);

    ////    // NOW initialize the DragController — after nodeViews and edgeLines have been created.
    ////    // Ensure AvaloniaGraphAdapter exposes InitializeDragController(Canvas)
    ////    _ec.Adapter.WireCanvasToDragController(GraphCanvas);
    ////}


    ////private void OnNodeAdded(GraphNode node)
    ////{
    ////    var view = new NodeControl { DataContext = node };
    ////    // position the control using the model Position:
    ////    view.SetValue(Canvas.LeftProperty, node.Position.X);
    ////    view.SetValue(Canvas.TopProperty, node.Position.Y);
    ////    // optional size if you persist it:
    ////    if (node.Size is GraphSize s) { view.Width = s.Width; view.Height = s.Height; }

    ////    // put it on top and add to canvas
    ////    view.SetValue(Panel.ZIndexProperty, 100);
    ////    GraphCanvas.Children.Add(view);

    ////    // let the adapter know so drag/snap works
    ////    _ec.Adapter.RegisterView(node, view);
    ////}

    ////private void OnNodeRemoved(GraphNode node)
    ////{
    ////    // find the view by DataContext (or keep a map if you prefer)
    ////    var toRemove = GraphCanvas.Children
    ////        .OfType<NodeControl>()
    ////        .FirstOrDefault(v => ReferenceEquals(v.DataContext, node));
    ////    if (toRemove != null)
    ////    {
    ////        _ec.Adapter.UnregisterView(node);
    ////        GraphCanvas.Children.Remove(toRemove);
    ////    }
    ////}

    ////private void OnNodeMoved(GraphNode node)
    ////{
    ////    var view = GraphCanvas.Children
    ////        .OfType<NodeControl>()
    ////        .FirstOrDefault(v => ReferenceEquals(v.DataContext, node));
    ////    if (view != null)
    ////    {
    ////        view.SetValue(Canvas.LeftProperty, node.Position.X);
    ////        view.SetValue(Canvas.TopProperty, node.Position.Y);
    ////    }
    ////}

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
    private async void OnOpenGraph(object? sender, RoutedEventArgs e)
    {
        var top = TopLevel.GetTopLevel(this);
        if (top is null) return;

        var path = await _fileDialogs.ShowOpenGraphAsync(top);
        if (string.IsNullOrWhiteSpace(path)) return;

        var gm = await _persistence.LoadDgmlAsync(path);
        _mc.Controller.ReloadFrom(gm);
    }

    private async void OnSaveGraph(object? sender, RoutedEventArgs e)
    {
        var top = TopLevel.GetTopLevel(this);
        if (top is null) return;

        var path = await _fileDialogs.ShowSaveGraphAsync(top);
        if (string.IsNullOrWhiteSpace(path)) return;

        await _persistence.SaveDgmlAsync(_mc.Controller.Model, path);
        _mc.Controller.Model.FilePath = path;
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


