namespace SpaceMap.App.ViewModels;

public sealed class RestoredSnapshotBannerViewModel : ObservableObject
{
    private bool _isVisible;
    private string _message = string.Empty;

    public bool IsVisible
    {
        get => _isVisible;
        set => SetProperty(ref _isVisible, value);
    }

    public string Message
    {
        get => _message;
        set => SetProperty(ref _message, value);
    }
}
