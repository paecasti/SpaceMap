using SpaceMap.Core.Application.Navigation;

namespace SpaceMap.App.ViewModels;

public sealed class ResultsViewModel : ObservableObject
{
    private ListChildItem? _selectedItem;

    public ListChildItem? SelectedItem
    {
        get => _selectedItem;
        set => SetProperty(ref _selectedItem, value);
    }
}
