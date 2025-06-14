using CommunityToolkit.Mvvm.ComponentModel;

namespace DirectedGraphEditor.Controls;

public partial class ProgressIndicatorViewModel : ObservableObject
{

    [ObservableProperty]
    private object? message;


    public static ProgressIndicatorViewModel Create(string message)
    {
        return new ProgressIndicatorViewModel
        {
            Message = message
        };
    }
}