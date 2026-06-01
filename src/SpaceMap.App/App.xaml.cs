using System.Windows;
using SpaceMap.App.Composition;
using SpaceMap.App.ViewModels;
using SpaceMap.App.Views;

namespace SpaceMap.App;

public partial class App : Application
{
    private AppServices? _services;

    private async void OnStartup(object sender, StartupEventArgs e)
    {
        _services = ServiceCollectionExtensions.CreateAppServices();
        var viewModel = new MainWindowViewModel(_services.DiskScanService, _services.StartupRestoreCoordinator);
        var window = new MainWindow(viewModel, _services.WindowLifecycleService);
        MainWindow = window;
        window.Show();
        await viewModel.InitializeAsync();
    }
}
