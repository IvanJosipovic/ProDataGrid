using Avalonia.Controls;

namespace DataGridSample.Pages
{
    public partial class FormulaColumnsA1Page : UserControl
    {
        public FormulaColumnsA1Page()
        {
            InitializeComponent();
            AttachedToVisualTree += (_, _) => DataContext ??= new DataGridSample.ViewModels.FormulaColumnsA1ViewModel();
        }
    }
}
