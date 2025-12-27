using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using DataGridSample;
using DataGridSample.ViewModels;

namespace DataGridSample.Pages
{
    public partial class StateScrollPage : UserControl
    {
        private DataGridScrollState? _state;

        public StateScrollPage()
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
            _state = Grid.CaptureScrollState(CreateOptions());
        }

        private void OnRestore(object? sender, RoutedEventArgs e)
        {
            if (_state != null)
            {
                Grid.TryRestoreScrollState(_state, CreateOptions());
            }
        }

        private void OnScrollTop(object? sender, RoutedEventArgs e)
        {
            if (ViewModel?.Items.Count > 0)
            {
                Grid.ScrollIntoView(ViewModel.Items[0], Grid.Columns.ElementAtOrDefault(0));
            }
        }

        private void OnScrollMiddle(object? sender, RoutedEventArgs e)
        {
            if (ViewModel?.Items.Count > 0)
            {
                var index = ViewModel.Items.Count / 2;
                Grid.ScrollIntoView(ViewModel.Items[index], Grid.Columns.ElementAtOrDefault(0));
            }
        }

        private void OnScrollLastColumn(object? sender, RoutedEventArgs e)
        {
            if (ViewModel?.Items.Count > 0 && Grid.Columns.Count > 0)
            {
                var item = ViewModel.Items[0];
                var column = Grid.Columns[Grid.Columns.Count - 1];
                Grid.ScrollIntoView(item, column);
            }
        }
    }
}
