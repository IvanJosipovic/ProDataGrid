using Avalonia.Controls;

namespace DataGridSample.Pages
{
    public partial class CurrentRowPseudoClassPage : UserControl
    {
        public CurrentRowPseudoClassPage()
        {
            InitializeComponent();
            AttachedToVisualTree += (_, _) => DataContext ??= new DataGridSample.ViewModels.CurrentRowPseudoClassViewModel();
        }
    }
}
