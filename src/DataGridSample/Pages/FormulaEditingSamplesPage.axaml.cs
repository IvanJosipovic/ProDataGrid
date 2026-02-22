using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;
using DataGridSample.ViewModels;

namespace DataGridSample.Pages
{
    public partial class FormulaEditingSamplesPage : UserControl
    {
        public FormulaEditingSamplesPage()
        {
            InitializeComponent();
            AttachedToVisualTree += (_, _) => DataContext ??= new DataGridSample.ViewModels.FormulaEditingSamplesViewModel();
            AttachedToVisualTree += OnAttachedToVisualTree;
        }

        private void OnAttachedToVisualTree(object? sender, VisualTreeAttachmentEventArgs e)
        {
            Dispatcher.UIThread.Post(SeedSpillFormula, DispatcherPriority.Background);
        }

        private void SeedSpillFormula()
        {
            var grid = this.FindControl<DataGrid>("SpillGrid");
            if (grid?.FormulaModel == null || DataContext is not FormulaEditingSamplesViewModel viewModel)
            {
                return;
            }

            if (viewModel.SpillItems.Count == 0)
            {
                return;
            }

            var firstFormula = viewModel.SpillColumns
                .OfType<DataGridFormulaColumnDefinition>()
                .FirstOrDefault();

            if (firstFormula == null)
            {
                return;
            }

            var item = viewModel.SpillItems[0];
            grid.FormulaModel.TrySetCellFormula(item, firstFormula, "=SEQUENCE(1,4)", out _);
        }

        private void OnRecalculateClick(object? sender, RoutedEventArgs e)
        {
            var grid = this.FindControl<DataGrid>("IncrementalGrid");
            grid?.FormulaModel?.Recalculate();
        }
    }
}
