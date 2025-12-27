using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using DataGridSample;
using DataGridSample.ViewModels;

namespace DataGridSample.Pages
{
    public partial class StateColumnsPage : UserControl
    {
        private DataGridColumnLayoutState? _state;

        public StateColumnsPage()
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
            _state = Grid.CaptureColumnLayoutState(CreateOptions());
        }

        private void OnRestore(object? sender, RoutedEventArgs e)
        {
            if (_state != null)
            {
                Grid.RestoreColumnLayoutState(_state, CreateOptions());
            }
        }

        private void OnShuffle(object? sender, RoutedEventArgs e)
        {
            if (Grid.Columns.Count < 2)
            {
                return;
            }

            var first = Grid.Columns[0];
            var last = Grid.Columns[Grid.Columns.Count - 1];
            var firstIndex = first.DisplayIndex;
            first.DisplayIndex = last.DisplayIndex;
            last.DisplayIndex = firstIndex;
        }

        private void OnToggleCategory(object? sender, RoutedEventArgs e)
        {
            var column = Grid.Columns.FirstOrDefault(candidate => Equals(candidate.Header, "Category"));
            if (column != null)
            {
                column.IsVisible = !column.IsVisible;
            }
        }

        private void OnResetWidths(object? sender, RoutedEventArgs e)
        {
            if (Grid.Columns.Count < 5)
            {
                return;
            }

            Grid.Columns[0].Width = new DataGridLength(80);
            Grid.Columns[1].Width = new DataGridLength(2, DataGridLengthUnitType.Star);
            Grid.Columns[2].Width = new DataGridLength(1, DataGridLengthUnitType.Star);
            Grid.Columns[3].Width = new DataGridLength(1, DataGridLengthUnitType.Star);
            Grid.Columns[4].Width = new DataGridLength(1, DataGridLengthUnitType.Star);
        }
    }
}
