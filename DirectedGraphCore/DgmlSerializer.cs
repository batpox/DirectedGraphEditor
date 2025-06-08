using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DirectedGraphCore;

using System.Xml;
using System.Xml.Linq;
using System.IO;

public static class DgmlSerializer
{
    private const string DgmlNs = "http://schemas.microsoft.com/vs/2009/dgml";

    public static void Save(GraphModel graph, string filePath)
    {
        var settings = new XmlWriterSettings { Indent = true };
        using var writer = XmlWriter.Create(filePath, settings);
        writer.WriteStartElement("DirectedGraph", DgmlNs);
        writer.WriteAttributeString("Title", "Saved Graph");

        writer.WriteStartElement("Nodes");
        foreach (var node in graph.Nodes.Values)
        {
            writer.WriteStartElement("Node");
            writer.WriteAttributeString("Id", node.Id);
            writer.WriteAttributeString("Label", node.Label ?? node.Id);
            writer.WriteEndElement();
        }
        writer.WriteEndElement(); // Nodes

        writer.WriteStartElement("Links");
        foreach (var edge in graph.Edges)
        {
            writer.WriteStartElement("Link");
            writer.WriteAttributeString("Source", edge.SourceId);
            writer.WriteAttributeString("Target", edge.TargetId);
            writer.WriteEndElement();
        }
        writer.WriteEndElement(); // Links

        writer.WriteEndElement(); // DirectedGraph
    }

    public static GraphModel Load(string filePath)
    {
        var graph = new GraphModel();
        var doc = XDocument.Load(filePath);
        XNamespace ns = DgmlNs;

        var nodes = doc.Root?.Element(ns + "Nodes");
        if (nodes != null)
        {
            foreach (var node in nodes.Elements(ns + "Node"))
            {
                var id = node.Attribute("Id")?.Value;
                var label = node.Attribute("Label")?.Value;
                if (!string.IsNullOrWhiteSpace(id))
                {
                    graph.AddNode(id, label);
                }
            }
        }

        var links = doc.Root?.Element(ns + "Links");
        if (links != null)
        {
            foreach (var link in links.Elements(ns + "Link"))
            {
                var source = link.Attribute("Source")?.Value;
                var target = link.Attribute("Target")?.Value;
                if (!string.IsNullOrWhiteSpace(source) && !string.IsNullOrWhiteSpace(target))
                {
                    graph.AddEdge(source, target);
                }
            }
        }

        return graph;
    }
}

