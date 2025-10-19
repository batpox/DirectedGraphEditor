using DirectedGraphCore.Models;


namespace DirectedGraphCore.Persistence;



public sealed class GraphPersistence : IGraphPersistence
{
    private const string LayoutExt = ".dgml-layout";
    private const string StructExt = ".dgml";

    public string GetLayoutPathFor(string structurePath)
        => Path.ChangeExtension(structurePath, LayoutExt);

    public string GetStructurePathFor(string layoutPath)
        => Path.HasExtension(layoutPath) && layoutPath.EndsWith(LayoutExt, System.StringComparison.OrdinalIgnoreCase)
            ? Path.ChangeExtension(layoutPath, StructExt)
            : layoutPath; // if user passed .dgml already

    public Task SavePairAsync(GraphModel model, string structurePath)
    {
        // 1) write structure and optional layout
        DgmlSerializer.SaveAsDgml(model, structurePath);

        ////// 2) write layout to companion
        ////var layoutPath = GetLayoutPathFor(structurePath);
        ////DgmlSerializer.SaveLayout(model, layoutPath); // <- your existing layout writer

        return Task.CompletedTask;
    }
    public Task SaveDgmlAsync(GraphModel model, string path)
    {
        DgmlSerializer.SaveAsDgml(model, path);
        return Task.CompletedTask;
    }

    public Task<GraphModel> LoadDgmlAsync(string path)
    {
        var gm = DgmlSerializer.LoadFromDgml(path);
        return Task.FromResult(gm);
    }

    /// <summary>Restore the Directed Graph Model from a pair of files: structure + layout</summary>
    public Task<GraphModel> LoadPairAsync(string anyPath)
    {
        // Normalize to structure path
        var structurePath = anyPath.EndsWith(LayoutExt, System.StringComparison.OrdinalIgnoreCase)
            ? GetStructurePathFor(anyPath)
            : anyPath;

        var gm = DgmlSerializer.LoadFromDgml(structurePath);

        ////// If layout exists, apply it on top
        ////var layoutPath = GetLayoutPathFor(structurePath);
        ////if (File.Exists(layoutPath))
        ////{
        ////    DgmlSerializer.ApplyLayout(gm, layoutPath); // sets node.Position, node.Size, pin indices/order, z, etc.
        ////}

        gm.FilePath = structurePath; // remember the "main" path
        return Task.FromResult(gm);
    }
}
