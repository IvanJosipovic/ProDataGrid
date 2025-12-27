using Avalonia.Collections;
using DataGridSample.Models;

namespace DataGridSample.ViewModels
{
    public class StateSampleGroupingViewModel : StateSampleViewModel
    {
        public StateSampleGroupingViewModel()
        {
            ItemsView.GroupDescriptions.Add(new DataGridPathGroupDescription(nameof(StateSampleItem.Category)));
            ItemsView.GroupDescriptions.Add(new DataGridPathGroupDescription(nameof(StateSampleItem.Group)));
        }
    }
}
