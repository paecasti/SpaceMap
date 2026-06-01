namespace SpaceMap.App.ViewModels;

public sealed class ScanWorkspaceViewModel : ObservableObject
{
    private string _scopePath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
    private string _statusText = "Ready";
    private long _entriesProcessed;
    private bool _hasActiveScan;

    public string ScopePath
    {
        get => _scopePath;
        set => SetProperty(ref _scopePath, value);
    }

    public string StatusText
    {
        get => _statusText;
        set => SetProperty(ref _statusText, value);
    }

    public long EntriesProcessed
    {
        get => _entriesProcessed;
        set => SetProperty(ref _entriesProcessed, value);
    }

    public bool HasActiveScan
    {
        get => _hasActiveScan;
        set => SetProperty(ref _hasActiveScan, value);
    }
}
