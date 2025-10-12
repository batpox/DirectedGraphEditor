using CommunityToolkit.Mvvm.Input;
using DirectedGraphCore.Controllers;
using DirectedGraphCore.Models;
using DirectedGraphEditor.Common;
using System;
using System.IO;
using System.Text.Json;


namespace DirectedGraphEditor.Pages.Editor;

public partial class EditorPageViewModel : BasePageViewModel
{
    private readonly GraphController controller;

    private string currentFile = string.Empty;
    public string CurrentFile
    {
        get => currentFile;
        set
        {
            if (value == currentFile) return;
            currentFile = value;
            OnPropertyChanged();
        }
    }

    public GraphController Controller => controller;

    public override string Name => "GraphEditor";

    public EditorPageViewModel()
    {
        // Create or load the underlying model
        var model = new GraphModel();
        controller = new GraphController(model);
    }

    // ─── Commands for the UI toolbar/menu ─────────────────────────────
    [RelayCommand]
    private void NewGraph()
    {
        controller.Model.Nodes.Clear();
        controller.Model.Edges.Clear();
        controller.ClearSelection();
        OnPropertyChanged(nameof(Controller));
    }

    public void OpenGraph(string path)
    {
        CurrentFile = path;
        LoadGraph();
    }

    [RelayCommand]
    private void LoadGraph()
    {
        if (string.IsNullOrWhiteSpace(CurrentFile) || !File.Exists(CurrentFile))
            return;

        try
        {
            controller.ReloadFromFile(CurrentFile); // uses DGML internally
        }
        catch (Exception ex)
        {
            throw new IOException($"Unable to load DGML graph from '{CurrentFile}'. {ex.Message}", ex);
        }
        if (!File.Exists(CurrentFile)) return;

    }


    [RelayCommand]
    private void SaveGraph()
    {
        if (string.IsNullOrWhiteSpace(CurrentFile))
            return;

        try
        {
            controller.Model.SaveAsDgml(CurrentFile);
        }
        catch (Exception ex)
        {
            throw new IOException($"Unable to save DGML graph to '{CurrentFile}'. {ex.Message}", ex);
        }
    }

    [RelayCommand]
    private void SaveGraphAs(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return;

        CurrentFile = path;
        SaveGraph();
    }
}

