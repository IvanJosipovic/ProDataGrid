using Avalonia.Controls;

namespace DataGridSample.Pages
{
    public partial class SelectionFastIndexInterfacePage : UserControl
    {
        public SelectionFastIndexInterfacePage()
        {
            InitializeComponent();
            AttachedToVisualTree += (_, _) => DataContext ??= new DataGridSample.ViewModels.SelectionFastIndexInterfaceViewModel();
        }
    }
}

