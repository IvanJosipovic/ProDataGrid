using Avalonia.Controls;

namespace DataGridSample.Pages
{
    public partial class ValidationStylingPage : UserControl
    {
        public ValidationStylingPage()
        {
            InitializeComponent();
            AttachedToVisualTree += (_, _) => DataContext ??= new DataGridSample.ViewModels.ValidationStylingViewModel();
        }
    }
}
