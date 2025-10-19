namespace DirectedGraphCore.Persistence;

using DirectedGraphCore.Models;
using System.Threading.Tasks;

public interface IGraphPersistence
{
    Task SaveDgmlAsync(GraphModel model, string path);
    Task<GraphModel> LoadDgmlAsync(string path);
}


