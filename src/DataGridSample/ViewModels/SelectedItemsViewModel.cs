using System.Collections;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using DataGridSample.Models;
using DataGridSample.Mvvm;

namespace DataGridSample.ViewModels
{
    public class SelectedItemsViewModel : ObservableObject
    {
        private string _lastChange = "Make a selection in the grid or via the ViewModel buttons.";

        public SelectedItemsViewModel()
        {
            Items = new ObservableCollection<Country>(Countries.All.Take(30).ToList());
            SelectedItems = new ObservableCollection<object>();
            SelectionLog = new ObservableCollection<string>();

            SelectedItems.CollectionChanged += OnSelectedItemsChanged;

            SelectTopCountriesCommand = new RelayCommand(_ => SelectTopCountries());
            SelectEuropeanRingCommand = new RelayCommand(_ => SelectRegion("WESTERN EUROPE"));
            ClearSelectionCommand = new RelayCommand(_ => SelectedItems.Clear());
        }

        public ObservableCollection<Country> Items { get; }

        public ObservableCollection<object> SelectedItems { get; }

        public ObservableCollection<string> SelectionLog { get; }

        public string LastChange
        {
            get => _lastChange;
            private set => SetProperty(ref _lastChange, value);
        }

        public int SelectedCount => SelectedItems.Count;

        public RelayCommand SelectTopCountriesCommand { get; }

        public RelayCommand SelectEuropeanRingCommand { get; }

        public RelayCommand ClearSelectionCommand { get; }

        private void OnSelectedItemsChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            var description = e.Action switch
            {
                NotifyCollectionChangedAction.Add => $"Add: {DescribeItems(e.NewItems)}",
                NotifyCollectionChangedAction.Remove => $"Remove: {DescribeItems(e.OldItems)}",
                NotifyCollectionChangedAction.Replace => $"Replace: {DescribeItems(e.OldItems)} -> {DescribeItems(e.NewItems)}",
                NotifyCollectionChangedAction.Reset => "Reset selection",
                NotifyCollectionChangedAction.Move => $"Move: {DescribeItems(e.NewItems)}",
                _ => e.Action.ToString()
            };

            LastChange = description;
            SelectionLog.Insert(0, $"{description} (count: {SelectedCount})");
            if (SelectionLog.Count > 40)
            {
                SelectionLog.RemoveAt(SelectionLog.Count - 1);
            }

            OnPropertyChanged(nameof(SelectedCount));
        }

        private void SelectTopCountries()
        {
            SelectedItems.Clear();
            foreach (var country in Items.Take(3))
            {
                SelectedItems.Add(country);
            }
        }

        private void SelectRegion(string region)
        {
            SelectedItems.Clear();
            foreach (var country in Items.Where(x => x.Region == region).Take(4))
            {
                SelectedItems.Add(country);
            }
        }

        private static string DescribeItems(IList? items)
        {
            if (items == null || items.Count == 0)
            {
                return "(none)";
            }

            return string.Join(", ",
                items.Cast<object>()
                     .Select(item => item is Country c ? c.Name : item?.ToString() ?? "(null)"));
        }
    }
}
