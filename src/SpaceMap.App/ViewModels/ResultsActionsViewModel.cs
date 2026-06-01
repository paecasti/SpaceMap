namespace SpaceMap.App.ViewModels;

public sealed class ResultsActionsViewModel : ObservableObject
{
    private bool _canRunActions;

    public bool CanRunActions
    {
        get => _canRunActions;
        set => SetProperty(ref _canRunActions, value);
    }
}
