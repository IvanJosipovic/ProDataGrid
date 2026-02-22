using Avalonia.Controls;

namespace DataGridSample.Pages
{
    public partial class FormulaEngineSamplesPage : UserControl
    {
        public FormulaEngineSamplesPage()
        {
            InitializeComponent();
            AttachedToVisualTree += (_, _) => DataContext ??= new DataGridSample.ViewModels.FormulaEngineSamplesViewModel();
        }
    }
}
