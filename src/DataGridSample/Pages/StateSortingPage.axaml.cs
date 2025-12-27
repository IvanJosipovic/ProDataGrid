using System.ComponentModel;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Controls.DataGridSorting;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using DataGridSample;
using DataGridSample.ViewModels;

namespace DataGridSample.Pages
{
    public partial class StateSortingPage : UserControl
    {
        private DataGridSortingState? _state;

        public StateSortingPage()
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
            _state = Grid.CaptureSortingState(CreateOptions());
        }

        private void OnRestore(object? sender, RoutedEventArgs e)
        {
            if (_state != null)
            {
                Grid.RestoreSortingState(_state, CreateOptions());
            }
        }

        private void OnApplySort(object? sender, RoutedEventArgs e)
        {
            var nameColumn = Grid.Columns.ElementAtOrDefault(1);
            if (nameColumn != null)
            {
                Grid.SortingModel.Apply(new[]
                {
                    new SortingDescriptor(nameColumn, ListSortDirection.Descending, "Name"),
                });
            }
        }

        private void OnClearSort(object? sender, RoutedEventArgs e)
        {
            Grid.SortingModel.Clear();
        }
    }
}
