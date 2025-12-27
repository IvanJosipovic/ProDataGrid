using Avalonia.Controls;
using Avalonia.Controls.DataGridSearching;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using DataGridSample;
using DataGridSample.ViewModels;

namespace DataGridSample.Pages
{
    public partial class StateSearchPage : UserControl
    {
        private DataGridSearchState? _state;

        public StateSearchPage()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
            QueryBox = this.FindControl<TextBox>("QueryBox");
            Grid = this.FindControl<DataGrid>("Grid");
        }

        private StateSampleViewModel? ViewModel => DataContext as StateSampleViewModel;

        private DataGridStateOptions? CreateOptions()
        {
            return ViewModel == null ? null : StateSampleOptionsFactory.Create(Grid, ViewModel.Items);
        }

        private void OnApplySearch(object? sender, RoutedEventArgs e)
        {
            var query = QueryBox.Text ?? string.Empty;
            var descriptor = new SearchDescriptor(query, SearchMatchMode.Contains, SearchTermCombineMode.Any, SearchScope.AllColumns);

            Grid.SearchModel.HighlightMode = SearchHighlightMode.TextAndCell;
            Grid.SearchModel.HighlightCurrent = true;
            Grid.SearchModel.Apply(new[] { descriptor });

            if (Grid.SearchModel.Results.Count > 0)
            {
                Grid.SearchModel.MoveTo(0);
            }
        }

        private void OnClearSearch(object? sender, RoutedEventArgs e)
        {
            Grid.SearchModel.Clear();
        }

        private void OnCapture(object? sender, RoutedEventArgs e)
        {
            _state = Grid.CaptureSearchState(CreateOptions());
        }

        private void OnRestore(object? sender, RoutedEventArgs e)
        {
            if (_state != null)
            {
                Grid.RestoreSearchState(_state, CreateOptions());
            }
        }

        private void OnMoveNext(object? sender, RoutedEventArgs e)
        {
            Grid.SearchModel.MoveNext();
        }
    }
}
