using Avalonia.Collections;
using DataGridSample.Models;

namespace DataGridSample.ViewModels
{
    public class GroupingViewModel
    {
        public GroupingViewModel()
        {
            GroupedCountriesView = new DataGridCollectionView(Countries.All);
            GroupedCountriesView.GroupDescriptions.Add(new DataGridPathGroupDescription(nameof(Country.Region)));
        }

        public DataGridCollectionView GroupedCountriesView { get; }
    }
}
