using System.Collections.ObjectModel;
using Avalonia.Collections;
using DataGridSample.Models;

namespace DataGridSample.ViewModels
{
    public class StateSampleViewModel
    {
        public StateSampleViewModel()
        {
            Items = new ObservableCollection<StateSampleItem>(StateSampleItem.CreateSamples(120));
            ItemsView = new DataGridCollectionView(Items);
        }

        public ObservableCollection<StateSampleItem> Items { get; }

        public DataGridCollectionView ItemsView { get; }
    }
}
