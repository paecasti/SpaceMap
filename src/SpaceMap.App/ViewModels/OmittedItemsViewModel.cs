using System.Collections.ObjectModel;
using SpaceMap.Core.Application.Navigation;

namespace SpaceMap.App.ViewModels;

public sealed class OmittedItemsViewModel : ObservableObject
{
    public ObservableCollection<OmittedSummary> Items { get; } = [];
}
