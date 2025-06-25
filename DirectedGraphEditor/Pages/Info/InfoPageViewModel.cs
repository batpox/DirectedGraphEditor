using Avalonia.Controls;
using Avalonia.Threading;
using System;
using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using DirectedGraphEditor.Common;
using DirectedGraphEditor.Services.Updates;

namespace DirectedGraphEditor.Pages.Info;

public partial class InfoPageViewModel : BasePageViewModel
{
    readonly AppUpdateService _appUpdateService;

    public override string Name => "LogPage";

    bool _isUpdateAvailable;
    string _latestAppVersion = string.Empty;

    public InfoPageViewModel()
    {

    }

    public InfoPageViewModel(AppUpdateService appUpdateService)
    {
        _appUpdateService = appUpdateService ?? throw new ArgumentNullException(nameof(appUpdateService));

        CurrentAppVersion = appUpdateService.CurrentVersion.ToString();
        DotNetVersion = Environment.Version.ToString();
        AvaloniaVersion = typeof(Label).Assembly.GetName().Version?.ToString() ?? "<unknown>";

        DispatcherTimer.Run(CheckForUpdates, TimeSpan.FromSeconds(1));
    }

    public string AvaloniaVersion { get; }

    public string CurrentAppVersion { get; }

    public string DotNetVersion { get; }

    [ObservableProperty]
    private bool isUpdateAvailable;

    [ObservableProperty]
    private string latestAppVersion = string.Empty;

    public void OpenReleasesUrl()
    {
        Launch("https://github.com/chkr1011/DirectedGraphEditor/releases");
    }

    private bool CheckForUpdates()
    {
        LatestAppVersion = _appUpdateService.LatestVersion?.ToString() ?? string.Empty;
        IsUpdateAvailable = _appUpdateService.IsUpdateAvailable;

        return true; // Keep timer running
    }


    static void Launch(string fileName)
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = fileName,
            UseShellExecute = true
        });
    }
}