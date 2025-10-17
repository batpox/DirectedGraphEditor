using System.Globalization;
using System.Reflection;
using System.Xml;
using System.Xml.Linq;

namespace DirectedGraphCore.Models;

/// <summary>
/// DGML Directed Graph Markup Language serializer/deserializer
/// </summary>
public static class DgmlSerializer
{
    private const string DgmlNs = "http://schemas.microsoft.com/vs/2009/dgml";

    /// <summary>
    /// Serialize into two files: semantic graph (.dgml) and layout (.dgml-layout)
    /// </summary>
    public static void SaveAsDgml(GraphModel graph, string filePathBase)
    {
        string ns = DgmlNs;

        // --- Save. Base semantic graph ---
        {
            var settings = new XmlWriterSettings { Indent = true };
            using var writer = XmlWriter.Create(filePathBase, settings);
            writer.WriteStartElement("DirectedGraph", ns);

            writer.WriteStartElement("Nodes");
            foreach (var node in graph.Nodes.Values)
            {
                writer.WriteStartElement("Node");
                writer.WriteAttributeString("Id", node.Id);
                writer.WriteAttributeString("Label", node.Name ?? node.Id);
                writer.WriteEndElement();
            }
            writer.WriteEndElement(); // Nodes

            writer.WriteStartElement("Links");
            foreach (var edge in graph.Edges)
            {
                writer.WriteStartElement("Link");
                writer.WriteAttributeString("Source", edge.Value.SourceNodeId);
                writer.WriteAttributeString("Target", edge.Value.TargetNodeId);
                writer.WriteEndElement();
            }
            writer.WriteEndElement(); // Links

            writer.WriteEndElement(); // DirectedGraph
        }

        // --- Save Layout overlay ---
        SaveAsDgmlLayout(ref graph, filePathBase);
    }

    /// <summary>
    /// Load the .dgml-layout overlay if present, and merge into the graph.
    /// </summary>
    /// <param name="graph"></param>
    /// <param name="basePath"></param>
    private static void SaveAsDgmlLayout(ref GraphModel graph, string basePath)
    {
        string ns = DgmlNs;

        // --- Load layout overlay (.dgml-layout) if present ---
        var layoutPath = Path.ChangeExtension(basePath, "dgml-layout");

        var settings = new XmlWriterSettings { Indent = true };
        using var writer = XmlWriter.Create(layoutPath, settings);
        writer.WriteStartElement("DirectedGraph", ns);

        writer.WriteStartElement("Nodes");
        foreach (var node in graph.Nodes.Values)
        {
            // Here we could look at node.Inputs/Outputs pins if you want per-pin geometry,
            // but for now treat node itself as having layout.
            ////var pos = node.Inputs.FirstOrDefault()?.Position ?? new GraphPosition(0, 0, 0);
            var size = node.Inputs.FirstOrDefault()?.Size ?? new GraphSize(0, 0, 0);

            writer.WriteStartElement("Node");
            writer.WriteAttributeString("Id", node.Id);
            writer.WriteAttributeString("Layout", "Fixed");
            writer.WriteAttributeString("Position", $"{node.Position.X},{node.Position.Y},{node.Position.Z}");
            writer.WriteAttributeString("Size", $"{size.Width},{size.Height},{size.Depth}");

            foreach (var pin in node.Inputs.Concat(node.Outputs))
            {
                writer.WriteStartElement("Pin");
                writer.WriteAttributeString("Id", pin.Id);
                writer.WriteAttributeString("Direction", pin.Direction.ToString());
                writer.WriteEndElement();
            }

            writer.WriteEndElement();
        }
        writer.WriteEndElement(); // Nodes

        writer.WriteEndElement(); // DGML layout

    }

    /// <summary>
    /// Read and deserialize from a DGML file (semantic + optional layout).
    /// Layout may come from legacy (e.g. Bounds), or overlay (e.g. Position+Size.
    /// </summary>
    public static GraphModel LoadFromDgml(string filePath)
    {
        var graph = new GraphModel();
        XNamespace ns = DgmlNs;

        // Load base graph (.dgml)
        var doc = XDocument.Load(filePath);
        var nodes = doc.Root?.Element(ns + "Nodes");
        if (nodes == null)
        {
            throw new Exception($"File={filePath} had no nodes.");
        }

        foreach (var node in nodes.Elements(ns + "Node"))
        {
            var id = node.Attribute("Id")?.Value;
            var label = node.Attribute("Label")?.Value ?? node.Attribute("Name")?.Value;

            if (!string.IsNullOrWhiteSpace(id))
            {
                var gNode = graph.FindOrAddNode(id);

                // If we have a DGML Bounds, convert to Position/Size
                var bounds = node.Attribute("Bounds")?.Value;
                if (!string.IsNullOrEmpty(bounds))
                {
                    // Format: x,y,width,height
                    var parts = bounds.Split(',');
                    if (parts.Length >= 4 &&
                        float.TryParse(parts[0], out var x) &&
                        float.TryParse(parts[1], out var y) &&
                        float.TryParse(parts[2], out var w) &&
                        float.TryParse(parts[3], out var h))
                    {
                        gNode.Inputs.Add(new NodePin(0, EnumNodePinDirection.Input, gNode)
                        {
                            ////Position = new GraphPosition(x, y, 0),
                            Size = new GraphSize(w, h, 0)
                        });
                    }
                } // check for Bounds
            } // Make sure we have an ID
        }


        var links = doc.Root?.Element(ns + "Links") ?? doc.Root?.Element(ns + "Edges");
        if (links != null)
        {
            foreach (var link in links.Elements())
            {
                var source = link.Attribute("Source")?.Value;
                var target = link.Attribute("Target")?.Value;
                if (!string.IsNullOrWhiteSpace(source) && !string.IsNullOrWhiteSpace(target))
                {
                    GraphEdge gEdge = graph.FindOrAddEdge(source, target);
                }
            }
        }

        LoadFromDgmlLayout(ref graph, filePath);

        return graph;
    }

    /// <summary>
    /// Load the .dgml-layout overlay if present, and merge into the graph.
    /// </summary>
    /// <param name="graph"></param>
    /// <param name="basePath"></param>
    private static void LoadFromDgmlLayout(ref GraphModel graph, string basePath)
    {
        XNamespace ns = DgmlNs;

        // --- Load layout overlay (.dgml-layout) if present ---
        var layoutPath = Path.ChangeExtension(basePath, "dgml-layout");
        if (File.Exists(layoutPath))
        {
            var layoutDoc = XDocument.Load(layoutPath);

            foreach (var node in layoutDoc.Root?.Element(ns + "Nodes")?.Elements(ns + "Node") ?? [])
            {
                var id = node.Attribute("Id")?.Value;
                if (id != null && graph.Nodes.TryGetValue(id, out var gnode))
                {
                    var bounds = node.Attribute("Bounds")?.Value;

                    // Position
                    var posAttr = node.Attribute("Position")?.Value;
                    if (!string.IsNullOrEmpty(posAttr))
                    {
                        var parts = posAttr.Split(',');
                        gnode.Position = new GraphPosition(
                            float.Parse(parts[0], CultureInfo.InvariantCulture),
                            float.Parse(parts[1], CultureInfo.InvariantCulture),
                            parts.Length > 2 ? float.Parse(parts[2]) : 0);
                    }

                    // Size
                    var sizeAttr = node.Attribute("Size")?.Value;
                    if (!string.IsNullOrEmpty(sizeAttr))
                    {
                        var parts = sizeAttr.Split(',');
                        gnode.Size = new GraphSize(
                            double.Parse(parts[0]),
                            double.Parse(parts[1]),
                            parts.Length > 2 ? double.Parse(parts[2]) : 0);
                    }

                    // Pins
                    var pinElems = node.Elements(ns + "Pin").ToList();
                    if (pinElems.Count > 0)
                    {
                        foreach (var s in pinElems)
                        {
                            var indexString = s.Attribute("Index")?.Value ?? "0";
                            int index = int.Parse(indexString);
                            var dirStr = s.Attribute("Direction")?.Value ?? "Input";
                            var dir = dirStr.Equals("Output", StringComparison.OrdinalIgnoreCase)
                                ? EnumNodePinDirection.Output
                                : EnumNodePinDirection.Input;
                            if (dir == EnumNodePinDirection.Input)
                                gnode.Inputs.Add(new NodePin(index, dir, gnode));
                            else
                                gnode.Outputs.Add(new NodePin(index, dir, gnode));
                        }
                    }
                }
            }

            // Add pins if missing
            foreach (var node in graph.Nodes.Values)
            {
                if (node.Inputs.Count == 0)
                {
                    var incoming = graph.Edges.Count(e => e.Value.TargetNodeId == node.Id);
                    for (int ii = 0; ii < incoming; ii++)
                        node.Inputs.Add(new NodePin(ii, EnumNodePinDirection.Input, node));
                }
                if (node.Outputs.Count == 0)
                {
                    var outgoing = graph.Edges.Count(e => e.Value.SourceNodeId == node.Id);
                    for (int ii = 0; ii < outgoing; ii++)
                        node.Outputs.Add(new NodePin(ii, EnumNodePinDirection.Output, node));
                }
            }
        }

    }
}


    /////// <summary>
    /////// DGML Directed Graph Markup Language serializer/deserializer
    /////// </summary>
    ////public static class DgmlSerializerOld
    ////{
    ////    private const string DgmlNs = "http://schemas.microsoft.com/vs/2009/dgml";

    ////    /// <summary>
    ////    /// Serialize and save to a path in DGML format
    ////    /// </summary>
    ////    /// <param name="graph"></param>
    ////    /// <param name="filePath"></param>
    ////    public static void SaveOld(GraphModel graph, string filePath)
    ////    {
    ////        var settings = new XmlWriterSettings { Indent = true };
    ////        using var writer = XmlWriter.Create(filePath, settings);
    ////        writer.WriteStartElement("DirectedGraph", DgmlNs);
    ////        writer.WriteAttributeString("Title", "Saved Graph");

    ////        writer.WriteStartElement("Nodes");
    ////        foreach (var node in graph.Nodes.Values)
    ////        {
    ////            writer.WriteStartElement("Node");
    ////            writer.WriteAttributeString("Id", node.Id);
    ////            writer.WriteAttributeString("Name", node.Name ?? node.Id);
    ////            writer.WriteEndElement();
    ////        }
    ////        writer.WriteEndElement(); // Nodes

    ////        writer.WriteStartElement("Edges");
    ////        foreach (var edge in graph.Edges.Values)
    ////        {
    ////            writer.WriteStartElement("Edge");
    ////            writer.WriteAttributeString("Source", edge.SourceNodeId);
    ////            writer.WriteAttributeString("Target", edge.TargetNodeId);
    ////            writer.WriteEndElement();
    ////        }
    ////        writer.WriteEndElement(); // Links

    ////        writer.WriteEndElement(); // DirectedGraph
    ////    }

    ////    /// <summary>
    ////    /// Read and deserialize from a DGML file.
    ////    /// Returns a new GraphModel instance.
    ////    /// </summary>
    ////    /// <param name="filePath"></param>
    ////    /// <returns></returns>
    ////    public static GraphModel LoadOld(string filePath)
    ////    {
    ////        var graph = new GraphModel();
    ////        var doc = XDocument.Load(filePath);
    ////        XNamespace ns = DgmlNs;

    ////        var nodes = doc.Root?.Element(ns + "Nodes");
    ////        if (nodes != null)
    ////        {
    ////            foreach (var node in nodes.Elements(ns + "Node"))
    ////            {
    ////                var id = node.Attribute("Id")?.Value;
    ////                var label = node.Attribute("Label")?.Value;
    ////                if (!string.IsNullOrWhiteSpace(id))
    ////                {
    ////                    graph.FindOrAddNode(id);
    ////                }
    ////            }
    ////        }

    ////        var links = doc.Root?.Element(ns + "Edges");
    ////        if (links != null)
    ////        {
    ////            foreach (var link in links.Elements(ns + "Edge"))
    ////            {
    ////                var source = link.Attribute("Source")?.Value;
    ////                var target = link.Attribute("Target")?.Value;
    ////                if (!string.IsNullOrWhiteSpace(source) && !string.IsNullOrWhiteSpace(target))
    ////                {
    ////                    graph.FindOrAddEdge(source, target);
    ////                }
    ////            }
    ////        }

    ////        return graph;
    ////    }
    ////}



