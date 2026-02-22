using Avalonia.Controls;

namespace DataGridSample
{
    public partial class DataTablePage : UserControl
    {
        public DataTablePage()
        {
            InitializeComponent();
            AttachedToVisualTree += (_, _) => DataContext ??= new DataGridSample.ViewModels.DataTableViewModel();
        }
    }
}
