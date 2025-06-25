using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace DirectedGraphEditor.Common;

public abstract class BasePageViewModel : BaseViewModel
{
    object? _overlayContent;

    public abstract string Name { get; }

    public event EventHandler? ActivationRequested;

    public object? OverlayContent
    {
        get => _overlayContent;
        set => this.SetProperty(ref _overlayContent, value);
    }

    public void RequestActivation()
    {
        ActivationRequested?.Invoke(this, EventArgs.Empty);
    }
}