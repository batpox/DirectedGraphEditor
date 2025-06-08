using System.IO;
using Xunit;
using DirectedGraphCore;

namespace DirectedGraphCore.Tests;

    public class DgmlSerializerTests
    {
        [Fact]
        public void SaveAndLoadGraph_RoundTrip_PreservesStructure()
        {
            var graph = new GraphModel();
            graph.AddEdge("A", "B");
            graph.AddEdge("B", "C");

            string tempFile = Path.GetTempFileName();

            graph.SaveToFile(tempFile);
            var loaded = GraphModel.LoadFromFile(tempFile);

            Assert.Equal(3, loaded.Nodes.Count);
            Assert.Equal(2, loaded.Edges.Count);
            Assert.Contains(loaded.Edges, e => e.SourceId == "A" && e.TargetId == "B");
            Assert.Contains(loaded.Edges, e => e.SourceId == "B" && e.TargetId == "C");
        }
    }

