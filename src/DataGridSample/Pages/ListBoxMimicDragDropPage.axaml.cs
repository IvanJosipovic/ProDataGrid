using Avalonia.Controls;

namespace DataGridSample.Pages
{
    public partial class ListBoxMimicDragDropPage : UserControl
    {
        public ListBoxMimicDragDropPage()
        {
            InitializeComponent();
            AttachedToVisualTree += (_, _) => DataContext ??= new DataGridSample.ViewModels.ListBoxMimicDragDropViewModel();
        }
    }
}
