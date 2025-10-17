using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;

namespace DirectedGraphCore.Styles
{
    [XmlRoot("GraphStyles")]
    public class DirectedGraphStyles
    {
        [XmlElement("Global")]
        public GlobalGraphStyle Global { get; set; } = new();

        [XmlArray("NodeStyles")]
        [XmlArrayItem("NodeStyle")]
        public List<NodeStyle> NodeStyles { get; set; } = new();

        [XmlArray("EdgeStyles")]
        [XmlArrayItem("EdgeStyle")]
        public List<EdgeStyle> EdgeStyles { get; set; } = new();
    }

    public class GlobalGraphStyle
    {
        [XmlElement]
        public string BackgroundColor { get; set; } = "#222222";
    }

    public class NodeStyle
    {
        [XmlAttribute]
        public string Id { get; set; } = string.Empty;

        [XmlElement]
        public string FillColor { get; set; } = "#444";

        [XmlElement]
        public string BorderColor { get; set; } = "White";

        [XmlElement]
        public double BorderThickness { get; set; } = 1.0;

        [XmlElement]
        public double Width { get; set; } = 120;

        [XmlElement]
        public double Height { get; set; } = 60;

        [XmlElement]
        public double PinRadius { get; set; } = 6;
    }

    public class EdgeStyle
    {
        [XmlAttribute]
        public string Id { get; set; } = string.Empty;

        [XmlElement]
        public string Color { get; set; } = "Yellow";

        [XmlElement]
        public double Thickness { get; set; } = 1.5;

        [XmlElement]
        public bool HasArrowHead { get; set; } = false;
    }

    public static class DirectedGraphStyleProvider
    {
        public static void Save(string path, DirectedGraphStyles styles)
        {
            using var stream = File.Create(path);
            var serializer = new XmlSerializer(typeof(DirectedGraphStyles));
            serializer.Serialize(stream, styles);
        }

        public static DirectedGraphStyles Load(string path)
        {
            if (!File.Exists(path))
                throw new FileNotFoundException("Style file not found", path);

            using var stream = File.OpenRead(path);
            var serializer = new XmlSerializer(typeof(DirectedGraphStyles));
            return (DirectedGraphStyles)serializer.Deserialize(stream)!;
        }
    }
}
