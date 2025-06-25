using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using Avalonia.Styling;
using Microsoft.Extensions.DependencyInjection;
using DirectedGraphEditor.Main;
using DirectedGraphEditor.Pages.Editor;
using DirectedGraphEditor.Pages.Info;
using DirectedGraphEditor.Pages.Log;
using DirectedGraphEditor.Services;
using DirectedGraphEditor.Controls;
using DirectedGraphEditor.Services.Updates;
using DirectedGraphEditor.Services.State;
using DirectedGraphEditor.Services.Data;

namespace DirectedGraphEditor
{
    public partial class App : Application
    {
        private static MainViewModel? mainViewModel;

        private readonly StateService stateService;

        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public App()
        {
            var serviceProvider = new ServiceCollection()
                // Services
                .AddSingleton<JsonSerializerService>()
                .AddSingleton<StateService>()
                .AddSingleton<AppUpdateService>()
                // Pages
                .AddSingleton<EditorPageViewModel>()
                .AddSingleton<LogPageViewModel>()
                .AddSingleton<InfoPageViewModel>()
                .AddSingleton<MainViewModel>()
                .BuildServiceProvider();

            // Optional: Add ViewLocator support
            var viewLocator = new ViewLocator();
            DataTemplates.Add(viewLocator);

            // Kick off update checks if you want to
            serviceProvider.GetRequiredService<AppUpdateService>().EnableUpdateChecks();

            // Save references
            stateService = serviceProvider.GetRequiredService<StateService>();
            mainViewModel = serviceProvider.GetRequiredService<MainViewModel>();
        }
        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                // Line below is needed to remove Avalonia data validation.
                // Without this line you will get duplicate validations from both Avalonia and CT
                BindingPlugins.DataValidators.RemoveAt(0);
                desktop.MainWindow = new MainWindow
                {
                    DataContext = new MainViewModel()
                };

            }
            else if (ApplicationLifetime is ISingleViewApplicationLifetime singleViewPlatform)
            {
                singleViewPlatform.MainView = new MainView
                {
                    DataContext = new MainViewModel()
                };
            }

            base.OnFrameworkInitializationCompleted();
        }
    }
}