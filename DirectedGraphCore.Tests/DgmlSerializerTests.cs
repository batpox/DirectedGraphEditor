using System.IO;
using Xunit;
using DirectedGraphCore.Models;

namespace DirectedGraphCore.Tests;

    public class DgmlSerializerTests
    {
        [Fact]
        public void SaveAndLoadGraph_RoundTrip_PreservesStructure()
        {
            var graph = new GraphModel();
            graph.FindOrAddEdge("A", "B");
            graph.FindOrAddEdge("B", "C");

            string tempFile = Path.GetTempFileName();

            graph.SaveAsDgml(tempFile);
            var loaded = GraphModel.LoadFromFile(tempFile);

            Assert.Equal(3, loaded.Nodes.Count);
            Assert.Equal(2, loaded.Edges.Count);
            Assert.Contains(loaded.Edges.Values, e => e.SourceNodeId == "A" && e.TargetNodeId == "B");
            Assert.Contains(loaded.Edges.Values, e => e.SourceNodeId == "B" && e.TargetNodeId == "C");
        }
    }

