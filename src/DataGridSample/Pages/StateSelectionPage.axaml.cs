using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using DataGridSample;
using DataGridSample.Models;
using DataGridSample.ViewModels;

namespace DataGridSample.Pages
{
    public partial class StateSelectionPage : UserControl
    {
        private DataGridSelectionState? _state;

        public StateSelectionPage()
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
            _state = Grid.CaptureSelectionState(CreateOptions());
        }

        private void OnRestore(object? sender, RoutedEventArgs e)
        {
            if (_state != null)
            {
                Grid.RestoreSelectionState(_state, CreateOptions());
            }
        }

        private void OnSelectSample(object? sender, RoutedEventArgs e)
        {
            if (ViewModel == null || ViewModel.Items.Count < 4)
            {
                return;
            }

            Grid.SelectedItems.Clear();
            Grid.SelectedCells.Clear();

            Grid.Selection.Select(1);
            Grid.Selection.Select(3);

            var column = Grid.Columns.ElementAtOrDefault(1);
            if (column != null)
            {
                var columnIndex = Grid.Columns.IndexOf(column);
                var rowIndex = 2;
                Grid.SelectedCells.Add(new DataGridCellInfo(ViewModel.Items[rowIndex], column, rowIndex, columnIndex, true));
                Grid.CurrentCell = new DataGridCellInfo(ViewModel.Items[1], column, 1, columnIndex, true);
            }
        }

        private void OnClearSelection(object? sender, RoutedEventArgs e)
        {
            Grid.SelectedItems.Clear();
            Grid.SelectedCells.Clear();
            Grid.CurrentCell = DataGridCellInfo.Unset;
        }
    }
}
