using System;
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
            AttachedToVisualTree += (_, _) => DataContext ??= new DataGridSample.ViewModels.StateSampleViewModel();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
            PayloadBox = this.FindControl<TextBox>("PayloadBox");
            StatusText = this.FindControl<TextBlock>("StatusText");
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
            SetStatus("Captured full runtime state.");
        }

        private void OnRestore(object? sender, RoutedEventArgs e)
        {
            if (_state != null)
            {
                Grid.RestoreState(_state, DataGridStateSections.All, CreateOptions());
                SetStatus("Restored full runtime state.");
            }
            else
            {
                SetStatus("No captured runtime state.");
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

            SetStatus("Applied sample state.");
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

            SetStatus("Cleared grid state.");
        }

        private void OnSerializeJson(object? sender, RoutedEventArgs e)
        {
            try
            {
                var payload = DataGridStatePersistence.SerializeStateToString(
                    Grid,
                    DataGridStateSections.All,
                    CreateOptions());

                PayloadBox.Text = payload;
                SetStatus($"Serialized JSON payload ({payload.Length} chars).");
            }
            catch (Exception ex)
            {
                SetStatus($"JSON serialization failed: {ex.Message}");
            }
        }

        private void OnRestoreJson(object? sender, RoutedEventArgs e)
        {
            var payload = PayloadBox.Text;
            if (string.IsNullOrWhiteSpace(payload))
            {
                SetStatus("JSON payload is empty.");
                return;
            }

            try
            {
                DataGridStatePersistence.RestoreStateFromString(
                    Grid,
                    payload,
                    DataGridStateSections.All,
                    CreateOptions());

                SetStatus("Restored state from JSON payload.");
            }
            catch (Exception ex)
            {
                SetStatus($"JSON restore failed: {ex.Message}");
            }
        }

        private void OnSerializeBase64(object? sender, RoutedEventArgs e)
        {
            try
            {
                var payload = DataGridStatePersistence.SerializeState(
                    Grid,
                    DataGridStateSections.All,
                    CreateOptions());

                var base64 = DataGridStatePersistence.EncodeBase64(payload);
                PayloadBox.Text = base64;
                SetStatus($"Serialized binary payload ({payload.Length} bytes) and encoded to Base64.");
            }
            catch (Exception ex)
            {
                SetStatus($"Base64 serialization failed: {ex.Message}");
            }
        }

        private void OnRestoreBase64(object? sender, RoutedEventArgs e)
        {
            var payload = PayloadBox.Text;
            if (string.IsNullOrWhiteSpace(payload))
            {
                SetStatus("Base64 payload is empty.");
                return;
            }

            try
            {
                var bytes = DataGridStatePersistence.DecodeBase64(payload);
                DataGridStatePersistence.RestoreState(
                    Grid,
                    bytes,
                    DataGridStateSections.All,
                    CreateOptions());

                SetStatus("Restored state from Base64 payload.");
            }
            catch (Exception ex)
            {
                SetStatus($"Base64 restore failed: {ex.Message}");
            }
        }

        private void OnClearPayload(object? sender, RoutedEventArgs e)
        {
            PayloadBox.Text = string.Empty;
            SetStatus("Cleared payload text.");
        }

        private void SetStatus(string message)
        {
            StatusText.Text = message;
        }
    }
}
