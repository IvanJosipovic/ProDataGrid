using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.VisualTree;
using DataGridSample.ViewModels;

namespace DataGridSample.Pages
{
    public partial class ColumnDefinitionsStreamingModelsPage : UserControl
    {
        private DataGrid? _streamingGrid;

        public ColumnDefinitionsStreamingModelsPage()
        {
            InitializeComponent();
            AttachedToVisualTree += OnAttachedToVisualTree;
            DataContextChanged += OnDataContextChanged;
            ApplyFastPathOptions();
        }

        private void OnAttachedToVisualTree(object? sender, VisualTreeAttachmentEventArgs e)
        {
            DataContext ??= new ColumnDefinitionsStreamingModelsViewModel();
            ApplyFastPathOptions();
        }

        private void OnDataContextChanged(object? sender, EventArgs e)
        {
            ApplyFastPathOptions();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
            _streamingGrid = this.FindControl<DataGrid>("StreamingGrid");
        }

        private void ApplyFastPathOptions()
        {
            var grid = _streamingGrid ??= this.FindControl<DataGrid>("StreamingGrid");
            if (grid == null)
            {
                return;
            }

            grid.FastPathOptions = (DataContext as ColumnDefinitionsStreamingModelsViewModel)?.FastPathOptions;
        }
    }
}
