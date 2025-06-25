using Avalonia.Controls;
using DirectedGraphEditor.ViewModels;

namespace DirectedGraphEditor.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            //// Explicitly set the DataContext
            //var view = new GraphEditorView
            //{
                DataContext = new xxMainViewModel();
            //};

            //Content = view;
        }
    }
}