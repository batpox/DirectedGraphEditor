using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DirectedGraphEditor.Common;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace DirectedGraphEditor.Pages.Log;

public partial class LogPageViewModel : BasePageViewModel
{
    [ObservableProperty]
    private ObservableCollection<string> logEntries = new();

    [ObservableProperty]
    private string currentLogEntry = string.Empty;

    public LogPageViewModel()
    {
        
    }

    public override string Name => "LogPage";

    [RelayCommand]
    private void ClearLog()
    {
        LogEntries.Clear();
    }

    [RelayCommand]
    private async Task<bool> CopyLogToClipboard()
    {
        var topLevel = TopLevel.GetTopLevel(Avalonia.Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop
            ? desktop.MainWindow
            : null);

        if (topLevel is null)
            return false;

        var text = string.Join(Environment.NewLine, LogEntries);
        await topLevel.Clipboard.SetTextAsync(text);
        return true;
    }

    public void AddLog(string message)
    {
        if (!string.IsNullOrWhiteSpace(message))
        {
            LogEntries.Add($"[{DateTime.Now:HH:mm:ss}] {message}");
        }
    }
}
