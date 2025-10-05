using DirectedGraphEditor.Common;
using DirectedGraphEditor.Pages.Editor;
using DirectedGraphEditor.Pages.Info;
using DirectedGraphEditor.Pages.Log;
using System;

namespace DirectedGraphEditor.Main;

public sealed class MainViewModel : BaseViewModel
{
    //readonly MqttClientService _mqttClientService;

    int _counter;
    object? _overlayContent;

    public MainViewModel(
        EditorPageViewModel editorPage,
        InfoPageViewModel infoPage,
        LogPageViewModel logPage)
        //MqttClientService mqttClientService)
    {
        //_mqttClientService = mqttClientService ?? throw new ArgumentNullException(nameof(mqttClientService));

        EditorPage = AttachEvents(editorPage);
        InfoPage = AttachEvents(infoPage);
        LogPage = AttachEvents(logPage);

        // Update the counter with a timer. There is no need to trigger a binding
        // for each counter increment.
        //DispatcherTimer.Run(UpdateCounter, TimeSpan.FromSeconds(1));
    }

    public MainViewModel()
    {
        EditorPage = AttachEvents(new EditorPageViewModel());
        InfoPage = AttachEvents(new InfoPageViewModel());
        LogPage = AttachEvents(new LogPageViewModel());

        // Optional: populate test nodes
        //EditorPage.LoadFromFiles()
    }

    public event EventHandler? ActivatePageRequested;


    public EditorPageViewModel EditorPage { get; }

    public InfoPageViewModel InfoPage { get; }

    public LogPageViewModel LogPage { get; }

    ////public object? OverlayContent
    ////{
    ////    get => _overlayContent;
    ////    set => this.RaiseAndSetIfChanged(ref _overlayContent, value);
    ////}


    TPage AttachEvents<TPage>(TPage page) where TPage : BasePageViewModel
    {
        page.ActivationRequested += (_, __) => ActivatePageRequested?.Invoke(page, EventArgs.Empty);
        return page;
    }

}