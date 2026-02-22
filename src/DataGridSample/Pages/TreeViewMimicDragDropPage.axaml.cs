using Avalonia.Controls;

namespace DataGridSample.Pages
{
    public partial class TreeViewMimicDragDropPage : UserControl
    {
        public TreeViewMimicDragDropPage()
        {
            InitializeComponent();
            AttachedToVisualTree += (_, _) => DataContext ??= new DataGridSample.ViewModels.TreeViewMimicDragDropViewModel();
        }
    }
}
