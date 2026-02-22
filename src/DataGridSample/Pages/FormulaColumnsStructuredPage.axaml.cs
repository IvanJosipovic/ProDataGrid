using Avalonia.Controls;

namespace DataGridSample.Pages
{
    public partial class FormulaColumnsStructuredPage : UserControl
    {
        public FormulaColumnsStructuredPage()
        {
            InitializeComponent();
            AttachedToVisualTree += (_, _) => DataContext ??= new DataGridSample.ViewModels.FormulaColumnsStructuredViewModel();
        }
    }
}
