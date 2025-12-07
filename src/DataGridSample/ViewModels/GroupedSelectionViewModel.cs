using System.Collections.ObjectModel;
using Avalonia.Collections;
using Avalonia.Controls.Selection;
using DataGridSample.Models;
using DataGridSample.Mvvm;

namespace DataGridSample.ViewModels;

public class GroupedSelectionViewModel : ObservableObject
{
    public GroupedSelectionViewModel()
    {
        Items = new ObservableCollection<Country>(Countries.All);
        GroupedView = new DataGridCollectionView(Items);
        GroupedView.GroupDescriptions.Add(new DataGridPathGroupDescription(nameof(Country.Region)));

        SelectionModel = new SelectionModel<Country>
        {
            SingleSelect = false,
            Source = GroupedView
        };
    }

    public ObservableCollection<Country> Items { get; }

    public DataGridCollectionView GroupedView { get; }

    public SelectionModel<Country> SelectionModel { get; }
}
