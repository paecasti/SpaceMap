namespace SpaceMap.App.ViewModels;

public sealed class NativeActionToastViewModel : ObservableObject
{
    private string _message = string.Empty;
    private bool _isError;

    public string Message
    {
        get => _message;
        set
        {
            if (SetProperty(ref _message, value))
            {
                NotifyPropertyChanged(nameof(IsVisible));
            }
        }
    }

    public bool IsError
    {
        get => _isError;
        set => SetProperty(ref _isError, value);
    }

    public bool IsVisible => !string.IsNullOrWhiteSpace(Message);
}
