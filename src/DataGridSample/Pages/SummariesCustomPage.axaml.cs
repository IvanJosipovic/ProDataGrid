// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

using System.Linq;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using DataGridSample.ViewModels;

namespace DataGridSample.Pages
{
    public partial class SummariesCustomPage : UserControl
    {
        private SummariesCustomViewModel? _configuredViewModel;

        public SummariesCustomPage()
        {
            InitializeComponent();
            AttachedToVisualTree += (_, _) => DataContext ??= new DataGridSample.ViewModels.SummariesCustomViewModel();
            AttachedToVisualTree += (_, _) => EnsureCustomSummariesConfigured();
            DataContextChanged += (_, _) => EnsureCustomSummariesConfigured();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private void EnsureCustomSummariesConfigured()
        {
            var vm = DataContext as SummariesCustomViewModel;
            if (vm == null)
                return;

            if (ReferenceEquals(_configuredViewModel, vm))
                return;

            var dataGrid = this.FindControl<DataGrid>("CustomDataGrid");
            if (dataGrid == null)
                return;

            _configuredViewModel = vm;

            // Add custom calculator to Quantity column (index 3)
            var quantityColumn = dataGrid.Columns.FirstOrDefault(c => c.Header?.ToString() == "Quantity");
            if (quantityColumn != null)
            {
                RemoveCustomSummaryByTitle(quantityColumn, "StdDev: ");
                quantityColumn.Summaries.Add(new DataGridCustomSummaryDescription
                {
                    Calculator = vm.StandardDeviationCalculator,
                    Title = "StdDev: ",
                    StringFormat = "N2"
                });
            }

            // Add weighted average calculator to Unit Price column
            var unitPriceColumn = dataGrid.Columns.FirstOrDefault(c => c.Header?.ToString() == "Unit Price");
            if (unitPriceColumn != null)
            {
                RemoveCustomSummaryByTitle(unitPriceColumn, "Wtd Avg: ");
                unitPriceColumn.Summaries.Add(new DataGridCustomSummaryDescription
                {
                    Calculator = vm.WeightedAverageCalculator,
                    Title = "Wtd Avg: ",
                    StringFormat = "C2"
                });
            }

            // Add percentage calculator to Total column
            var totalColumn = dataGrid.Columns.FirstOrDefault(c => c.Header?.ToString() == "Total");
            if (totalColumn != null)
            {
                RemoveCustomSummaryByTitle(totalColumn, "% Visible: ");
                totalColumn.Summaries.Add(new DataGridCustomSummaryDescription
                {
                    Calculator = vm.PercentageOfTotalCalculator,
                    Title = "% Visible: "
                });
            }
        }

        private static void RemoveCustomSummaryByTitle(DataGridColumn column, string title)
        {
            for (var i = column.Summaries.Count - 1; i >= 0; i--)
            {
                if (column.Summaries[i] is DataGridCustomSummaryDescription summary && summary.Title == title)
                {
                    column.Summaries.RemoveAt(i);
                }
            }
        }
    }
}
