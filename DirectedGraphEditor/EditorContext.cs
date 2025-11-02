// Editor/EditorContext.cs
using Avalonia.Controls;
using DirectedGraphEditor.Adapters;
using DirectedGraphEditor.Controls;
using DirectedGraphEditor.Interaction;
using System.Collections.Generic;

public class EditorContext
{
    public Canvas? Canvas { get; set; }
    public GraphAdapter? Adapter { get; set; }
    // Visual registries (owned by Adapter; passed for convenience)
    public IDictionary<string, NodeControl>? NodeViews { get; set; }
    public IDictionary<string, EdgeControl>? EdgeViews { get; set; }

    // Interaction services exposed by Adapter
    public IHitTester HitTester;
    public IPinResolver PinResolver;
    public IRubberHost Rubber;
}
