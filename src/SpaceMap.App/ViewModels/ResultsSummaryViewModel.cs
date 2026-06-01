using System.Collections.ObjectModel;
using SpaceMap.Core.Application.Navigation;

namespace SpaceMap.App.ViewModels;

public sealed class ResultsSummaryViewModel : ObservableObject
{
    public ObservableCollection<ListChildItem> TopItems { get; } = [];
}
