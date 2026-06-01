using System.Collections.ObjectModel;

namespace SpaceMap.App.ViewModels;

public sealed class ScanIssuePanelViewModel : ObservableObject
{
    public ObservableCollection<string> Items { get; } = [];
}
