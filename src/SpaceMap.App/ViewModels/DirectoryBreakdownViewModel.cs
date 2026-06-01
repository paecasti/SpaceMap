using System.Collections.ObjectModel;
using SpaceMap.Core.Application.Navigation;
using SpaceMap.Core.Domain;

namespace SpaceMap.App.ViewModels;

public sealed class DirectoryBreakdownViewModel : ObservableObject
{
    private ListChildItem? _selectedItem;
    private long? _minimumSizeBytes;
    private SortMode _sortMode = SortMode.RealDesc;

    public ObservableCollection<ListChildItem> Items { get; } = [];

    public ListChildItem? SelectedItem
    {
        get => _selectedItem;
        set => SetProperty(ref _selectedItem, value);
    }

    public long? MinimumSizeBytes
    {
        get => _minimumSizeBytes;
        set => SetProperty(ref _minimumSizeBytes, value);
    }

    public SortMode SortMode
    {
        get => _sortMode;
        set => SetProperty(ref _sortMode, value);
    }
}
