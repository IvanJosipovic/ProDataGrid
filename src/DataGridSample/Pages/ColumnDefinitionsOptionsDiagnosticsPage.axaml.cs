using System;
using Avalonia.Controls;
using DataGridSample.ViewModels;

namespace DataGridSample.Pages
{
    public partial class ColumnDefinitionsOptionsDiagnosticsPage : UserControl
    {
        public ColumnDefinitionsOptionsDiagnosticsPage()
        {
            InitializeComponent();
            AttachedToVisualTree += (_, _) => DataContext ??= new DataGridSample.ViewModels.ColumnDefinitionsOptionsDiagnosticsViewModel();
            DataContextChanged += OnDataContextChanged;
            OnDataContextChanged(this, EventArgs.Empty);
        }

        private void OnDataContextChanged(object? sender, EventArgs e)
        {
            if (DataContext is ColumnDefinitionsOptionsDiagnosticsViewModel viewModel)
            {
                OptionsGrid.FastPathOptions = viewModel.FastPathOptions;
            }
        }

    }
}
