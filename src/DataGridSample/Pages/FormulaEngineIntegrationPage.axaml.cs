using Avalonia.Controls;

namespace DataGridSample.Pages
{
    public partial class FormulaEngineIntegrationPage : UserControl
    {
        public FormulaEngineIntegrationPage()
        {
            InitializeComponent();
            AttachedToVisualTree += (_, _) => DataContext ??= new DataGridSample.ViewModels.FormulaEngineIntegrationViewModel();
        }
    }
}
