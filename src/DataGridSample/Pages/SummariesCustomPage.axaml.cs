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
        public SummariesCustomPage()
        {
            InitializeComponent();
            AttachedToVisualTree += (_, _) => DataContext ??= new DataGridSample.ViewModels.SummariesCustomViewModel();
            AddCustomSummaries();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private void AddCustomSummaries()
        {
            var vm = DataContext as SummariesCustomViewModel;
            if (vm == null)
                return;

            var dataGrid = this.FindControl<DataGrid>("CustomDataGrid");
            if (dataGrid == null)
                return;

            // Add custom calculator to Quantity column (index 3)
            var quantityColumn = dataGrid.Columns.FirstOrDefault(c => c.Header?.ToString() == "Quantity");
            if (quantityColumn != null)
            {
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
                totalColumn.Summaries.Add(new DataGridCustomSummaryDescription
                {
                    Calculator = vm.PercentageOfTotalCalculator,
                    Title = "% Visible: "
                });
            }
        }
    }
}
