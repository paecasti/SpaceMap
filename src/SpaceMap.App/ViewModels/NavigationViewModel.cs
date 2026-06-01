using System.Collections.ObjectModel;
using SpaceMap.Core.Application.Navigation;

namespace SpaceMap.App.ViewModels;

public sealed class NavigationViewModel : ObservableObject
{
    private string _currentPath = string.Empty;

    public ObservableCollection<BreadcrumbItem> BreadcrumbItems { get; } = [];

    public string CurrentPath
    {
        get => _currentPath;
        set => SetProperty(ref _currentPath, value);
    }
}
