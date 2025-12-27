using System.ComponentModel;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Controls.DataGridFiltering;
using Avalonia.Controls.DataGridSearching;
using Avalonia.Controls.DataGridSorting;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using DataGridSample;
using DataGridSample.ViewModels;

namespace DataGridSample.Pages
{
    public partial class StateFullPage : UserControl
    {
        private DataGridState? _state;

        public StateFullPage()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
            Grid = this.FindControl<DataGrid>("Grid");
        }

        private StateSampleViewModel? ViewModel => DataContext as StateSampleViewModel;

        private DataGridStateOptions? CreateOptions()
        {
            return ViewModel == null ? null : StateSampleOptionsFactory.Create(Grid, ViewModel.Items);
        }

        private void OnCapture(object? sender, RoutedEventArgs e)
        {
            _state = Grid.CaptureState(DataGridStateSections.All, CreateOptions());
        }

        private void OnRestore(object? sender, RoutedEventArgs e)
        {
            if (_state != null)
            {
                Grid.RestoreState(_state, DataGridStateSections.All, CreateOptions());
            }
        }

        private void OnApplySample(object? sender, RoutedEventArgs e)
        {
            if (ViewModel == null)
            {
                return;
            }

            var nameColumn = Grid.Columns.ElementAtOrDefault(1);
            var categoryColumn = Grid.Columns.ElementAtOrDefault(2);

            if (nameColumn != null)
            {
                nameColumn.DisplayIndex = 0;
            }

            if (Grid.Columns.Count > 0)
            {
                Grid.Columns[0].DisplayIndex = 1;
            }

            if (categoryColumn != null)
            {
                categoryColumn.IsVisible = false;
            }

            Grid.FrozenColumnCount = 1;

            if (nameColumn != null)
            {
                Grid.SortingModel.Apply(new[]
                {
                    new SortingDescriptor(nameColumn, ListSortDirection.Descending, "Name"),
                });
            }

            if (categoryColumn != null)
            {
                Grid.FilteringModel.Apply(new[]
                {
                    new FilteringDescriptor(
                        categoryColumn,
                        FilteringOperator.Equals,
                        "Category",
                        "Alpha"),
                });
            }

            Grid.SearchModel.Apply(new[]
            {
                new SearchDescriptor("Item 1", SearchMatchMode.Contains, SearchTermCombineMode.Any, SearchScope.AllColumns),
            });

            if (ViewModel.Items.Count > 20)
            {
                Grid.SelectedItems.Clear();
                Grid.Selection.Select(2);
                Grid.Selection.Select(4);
                Grid.ScrollIntoView(ViewModel.Items[20], nameColumn ?? Grid.Columns[0]);
            }
        }

        private void OnClearState(object? sender, RoutedEventArgs e)
        {
            if (ViewModel == null)
            {
                return;
            }

            Grid.SortingModel.Clear();
            Grid.FilteringModel.Clear();
            Grid.SearchModel.Clear();
            Grid.SelectedItems.Clear();
            Grid.SelectedCells.Clear();
            Grid.FrozenColumnCount = 0;

            for (int i = 0; i < Grid.Columns.Count; i++)
            {
                Grid.Columns[i].DisplayIndex = i;
                Grid.Columns[i].IsVisible = true;
            }

            if (ViewModel.Items.Count > 0)
            {
                Grid.ScrollIntoView(ViewModel.Items[0], Grid.Columns[0]);
            }
        }
    }
}
