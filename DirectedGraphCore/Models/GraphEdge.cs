
public sealed class GraphEdge
{
    public string Id { get; init; } = Guid.NewGuid().ToString("N");

    public string SourceNodeId { get; init; } = "";
    public string SourcePinId { get; init; } = "";

    public string TargetNodeId { get; init; } = "";
    public string TargetPinId { get; init; } = "";

    public static GraphEdge Create(string sN, string sP, string tN, string tP) =>
    new() { SourceNodeId = sN, SourcePinId = sP, TargetNodeId = tN, TargetPinId = tP };
}