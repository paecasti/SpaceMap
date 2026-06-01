using System.ComponentModel;
using System.Windows;
using SpaceMap.App.Services;
using SpaceMap.App.ViewModels;

namespace SpaceMap.App.Views;

public partial class MainWindow : Window
{
    private readonly MainWindowViewModel _viewModel;
    private readonly WindowLifecycleService _windowLifecycleService;

    public MainWindow(MainWindowViewModel viewModel, WindowLifecycleService windowLifecycleService)
    {
        InitializeComponent();
        _viewModel = viewModel;
        _windowLifecycleService = windowLifecycleService;
        DataContext = viewModel;
        Closing += OnClosing;
    }

    private void OnClosing(object? sender, CancelEventArgs e)
    {
        if (!_windowLifecycleService.ConfirmClose(this, _viewModel.HasActiveScan))
        {
            e.Cancel = true;
            return;
        }

        if (_viewModel.HasActiveScan && _viewModel.CancelScanCommand.CanExecute(null))
        {
            _viewModel.CancelScanCommand.Execute(null);
        }
    }
}
