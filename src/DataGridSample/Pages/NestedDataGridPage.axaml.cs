using Avalonia.Controls;

namespace DataGridSample.Pages
{
    public partial class NestedDataGridPage : UserControl
    {
        public NestedDataGridPage()
        {
            InitializeComponent();
            AttachedToVisualTree += (_, _) => DataContext ??= new DataGridSample.ViewModels.NestedDataGridViewModel();
        }
    }
}
