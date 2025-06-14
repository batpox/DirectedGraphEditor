using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;

namespace DirectedGraphEditor.Controls;

public partial class ErrorBoxViewModel : ObservableObject
{
    [ObservableProperty]
    private string exception = string.Empty;

    [ObservableProperty]
    private string message = string.Empty;

    public event Action? CloseRequested;
    public event Action? CopyRequested;

    [RelayCommand]
    private void CopyToClipboard()
    {
        if (!string.IsNullOrEmpty(Message))
            CopyRequested?.Invoke();
    }

    [RelayCommand]
    private void Close()
    {
        CloseRequested?.Invoke();
    }
}
