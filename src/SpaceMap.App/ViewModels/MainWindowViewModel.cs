using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using SpaceMap.App.Services;
using SpaceMap.Core.Application.Contracts;
using SpaceMap.Core.Application.Navigation;
using SpaceMap.Core.Application.Scanning;
using SpaceMap.Core.Domain;

namespace SpaceMap.App.ViewModels;

public sealed class MainWindowViewModel : ObservableObject
{
    private readonly IDiskScanService _diskScanService;
    private readonly StartupRestoreCoordinator _startupRestoreCoordinator;

    private string? _scanId;

    public MainWindowViewModel(IDiskScanService diskScanService, StartupRestoreCoordinator startupRestoreCoordinator)
    {
        _diskScanService = diskScanService;
        _startupRestoreCoordinator = startupRestoreCoordinator;

        ScanWorkspace = new ScanWorkspaceViewModel();
        ResultsSummary = new ResultsSummaryViewModel();
        Navigation = new NavigationViewModel();
        DirectoryBreakdown = new DirectoryBreakdownViewModel();
        RestoredSnapshotBanner = new RestoredSnapshotBannerViewModel();
        ScanIssuePanel = new ScanIssuePanelViewModel();
        ResultsActions = new ResultsActionsViewModel();
        NativeActionToast = new NativeActionToastViewModel();
        OmittedItems = new OmittedItemsViewModel();
        Results = new ResultsViewModel();

        StartScanCommand = new AsyncRelayCommand(StartScanAsync, () => !ScanWorkspace.HasActiveScan);
        CancelScanCommand = new AsyncRelayCommand(CancelScanAsync, () => ScanWorkspace.HasActiveScan);
        RefreshCommand = new AsyncRelayCommand(RefreshCurrentPathAsync, () => !string.IsNullOrWhiteSpace(_scanId) && !string.IsNullOrWhiteSpace(Navigation.CurrentPath));
        OpenSelectedPathCommand = new AsyncRelayCommand(OpenSelectedPathAsync, () => Results.SelectedItem is not null);
        CopySelectedPathCommand = new AsyncRelayCommand(CopySelectedPathAsync, () => Results.SelectedItem is not null);
        NavigateCommand = new AsyncRelayCommand<string>(NavigateAsync);
        SetRealSortCommand = new AsyncRelayCommand(() => ApplySortAsync(SortMode.RealDesc));
        SetLogicalSortCommand = new AsyncRelayCommand(() => ApplySortAsync(SortMode.LogicalDesc));
        SetNameSortCommand = new AsyncRelayCommand(() => ApplySortAsync(SortMode.NameAsc));

        _diskScanService.ScanProgressChanged += HandleProgressChanged;
        _diskScanService.PartialBreakdownPublished += HandlePartialBreakdownPublished;
        _diskScanService.ScanIssueReported += HandleScanIssueReported;
    }

    public string? ScanId
    {
        get => _scanId;
        private set => SetProperty(ref _scanId, value);
    }

    public ScanWorkspaceViewModel ScanWorkspace { get; }

    public ResultsSummaryViewModel ResultsSummary { get; }

    public NavigationViewModel Navigation { get; }

    public DirectoryBreakdownViewModel DirectoryBreakdown { get; }

    public RestoredSnapshotBannerViewModel RestoredSnapshotBanner { get; }

    public ScanIssuePanelViewModel ScanIssuePanel { get; }

    public ResultsActionsViewModel ResultsActions { get; }

    public NativeActionToastViewModel NativeActionToast { get; }

    public OmittedItemsViewModel OmittedItems { get; }

    public ResultsViewModel Results { get; }

    public ICommand StartScanCommand { get; }

    public ICommand CancelScanCommand { get; }

    public ICommand RefreshCommand { get; }

    public ICommand OpenSelectedPathCommand { get; }

    public ICommand CopySelectedPathCommand { get; }

    public ICommand NavigateCommand { get; }

    public ICommand SetRealSortCommand { get; }

    public ICommand SetLogicalSortCommand { get; }

    public ICommand SetNameSortCommand { get; }

    public bool HasActiveScan => ScanWorkspace.HasActiveScan;

    public async Task InitializeAsync()
    {
        var restored = await _startupRestoreCoordinator.RestoreAsync();
        if (restored is null)
        {
            return;
        }

        ScanId = restored.ScanId;
        Navigation.CurrentPath = restored.ViewState.CurrentPath;
        DirectoryBreakdown.SortMode = restored.ViewState.SortMode;
        DirectoryBreakdown.MinimumSizeBytes = restored.ViewState.MinimumSizeBytes;

        RunOnUiThread(
            () =>
            {
                OmittedItems.Items.Clear();
                foreach (var item in restored.OmittedItems)
                {
                    OmittedItems.Items.Add(item);
                }
            });

        RestoredSnapshotBanner.IsVisible = true;
        RestoredSnapshotBanner.Message = $"Restored snapshot {restored.ScanId}. Marked as potentially outdated.";
        await LoadListingAsync(restored.ViewState.CurrentPath);
    }

    private async Task StartScanAsync()
    {
        var scopePath = string.IsNullOrWhiteSpace(ScanWorkspace.ScopePath)
            ? Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)
            : ScanWorkspace.ScopePath;

        var response = await _diskScanService.StartScanAsync(new ScanScope(ScopeMode.SinglePath, [scopePath]));
        ScanId = response.ScanId;
        ScanWorkspace.HasActiveScan = true;
        ScanWorkspace.StatusText = "Running";
        Navigation.CurrentPath = scopePath;
        RestoredSnapshotBanner.IsVisible = false;
        NotifyCommands();
    }

    private async Task CancelScanAsync()
    {
        if (ScanId is null)
        {
            return;
        }

        await _diskScanService.CancelScanAsync(ScanId);
    }

    private async Task RefreshCurrentPathAsync()
    {
        if (!string.IsNullOrWhiteSpace(Navigation.CurrentPath))
        {
            await LoadListingAsync(Navigation.CurrentPath);
        }
    }

    private async Task NavigateAsync(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        await LoadListingAsync(path);
    }

    private async Task ApplySortAsync(SortMode sortMode)
    {
        DirectoryBreakdown.SortMode = sortMode;
        await RefreshCurrentPathAsync();
    }

    private async Task OpenSelectedPathAsync()
    {
        if (Results.SelectedItem is null)
        {
            return;
        }

        var result = await _diskScanService.OpenPathAsync(Results.SelectedItem.Path);
        NativeActionToast.Message = result.Message;
        NativeActionToast.IsError = !result.Succeeded;
    }

    private async Task CopySelectedPathAsync()
    {
        if (Results.SelectedItem is null)
        {
            return;
        }

        var result = await _diskScanService.CopyPathAsync(Results.SelectedItem.Path);
        NativeActionToast.Message = result.Message;
        NativeActionToast.IsError = !result.Succeeded;
    }

    private async Task LoadListingAsync(string path)
    {
        if (ScanId is null)
        {
            return;
        }

        var result = await _diskScanService.ListChildrenAsync(
            ScanId,
            path,
            DirectoryBreakdown.SortMode,
            DirectoryBreakdown.MinimumSizeBytes,
            500,
            0);

        Navigation.CurrentPath = result.Path;
        RunOnUiThread(
            () =>
            {
                DirectoryBreakdown.Items.Clear();
                ResultsSummary.TopItems.Clear();
                Navigation.BreadcrumbItems.Clear();

                foreach (var item in result.Items)
                {
                    DirectoryBreakdown.Items.Add(item);
                    if (item.Kind == NodeKind.Directory && ResultsSummary.TopItems.Count < 8)
                    {
                        ResultsSummary.TopItems.Add(item);
                    }
                }

                foreach (var breadcrumb in result.Breadcrumb)
                {
                    Navigation.BreadcrumbItems.Add(breadcrumb);
                }
            });

        await _startupRestoreCoordinator.SaveViewStateAsync(
            ScanId,
            result.Path,
            result.Breadcrumb.Select(x => x.Path).ToArray(),
            DirectoryBreakdown.SortMode,
            DirectoryBreakdown.MinimumSizeBytes,
            Results.SelectedItem?.Path);
        NotifyCommands();
    }

    private void HandleProgressChanged(object? sender, ScanProgressEvent progress)
    {
        RunOnUiThread(
            async () =>
            {
                if (ScanId is not null && !string.Equals(ScanId, progress.ScanId, StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }

                ScanId = progress.ScanId;
                ScanWorkspace.StatusText = progress.State.ToString();
                ScanWorkspace.EntriesProcessed = progress.EntriesProcessed;
                ScanWorkspace.HasActiveScan = progress.State is ScanStatus.Running or ScanStatus.Cancelling;
                if (progress.State is ScanStatus.Completed or ScanStatus.PartialCompleted)
                {
                    await LoadListingAsync(progress.CurrentRootPath);
                }

                NotifyCommands();
            });
    }

    private void HandlePartialBreakdownPublished(object? sender, PartialBreakdownEvent breakdown)
    {
        RunOnUiThread(
            () =>
            {
                ResultsSummary.TopItems.Clear();
                foreach (var item in breakdown.TopContributors)
                {
                    ResultsSummary.TopItems.Add(new ListChildItem(item.Path, item.Kind, item.RealSizeBytes, item.LogicalSizeBytes, item.Partial));
                }
            });
    }

    private void HandleScanIssueReported(object? sender, ScanIssueEvent issue)
    {
        RunOnUiThread(() => ScanIssuePanel.Items.Add($"{issue.ReasonCode}: {issue.Path}"));
    }

    private void NotifyCommands()
    {
        ((AsyncRelayCommand)StartScanCommand).NotifyCanExecuteChanged();
        ((AsyncRelayCommand)CancelScanCommand).NotifyCanExecuteChanged();
        ((AsyncRelayCommand)RefreshCommand).NotifyCanExecuteChanged();
        ((AsyncRelayCommand)OpenSelectedPathCommand).NotifyCanExecuteChanged();
        ((AsyncRelayCommand)CopySelectedPathCommand).NotifyCanExecuteChanged();
    }

    private static void RunOnUiThread(Action action)
    {
        if (Application.Current is null)
        {
            action();
            return;
        }

        if (Application.Current.Dispatcher.CheckAccess())
        {
            action();
            return;
        }

        Application.Current.Dispatcher.Invoke(action);
    }

    private static void RunOnUiThread(Func<Task> action)
    {
        if (Application.Current is null)
        {
            _ = action();
            return;
        }

        if (Application.Current.Dispatcher.CheckAccess())
        {
            _ = action();
            return;
        }

        Application.Current.Dispatcher.Invoke(() => _ = action());
    }
}

public sealed class AsyncRelayCommand<T>(Func<T?, Task> executeAsync, Func<T?, bool>? canExecute = null) : ICommand
{
    private bool _isExecuting;

    public event EventHandler? CanExecuteChanged;

    public bool CanExecute(object? parameter) => !_isExecuting && (canExecute?.Invoke((T?)parameter) ?? true);

    public async void Execute(object? parameter)
    {
        if (!CanExecute(parameter))
        {
            return;
        }

        _isExecuting = true;
        NotifyCanExecuteChanged();
        try
        {
            await executeAsync((T?)parameter);
        }
        finally
        {
            _isExecuting = false;
            NotifyCanExecuteChanged();
        }
    }

    public void NotifyCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
}
