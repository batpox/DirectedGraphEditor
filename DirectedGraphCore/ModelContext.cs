// Core/ModelContext.cs
using DirectedGraphCore.Controllers;
using DirectedGraphCore.Models;
using DirectedGraphCore.Commands;

public class ModelContext
{
    public GraphModel? Model { get; set; }
    public GraphController? Controller { get; set; }
    public CommandStack? Commands { get; set; }
}
