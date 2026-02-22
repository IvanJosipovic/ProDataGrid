using Avalonia.Controls;

namespace DataGridSample.Pages;

public partial class TreeViewMimicPage : UserControl
{
    public TreeViewMimicPage()
    {
        InitializeComponent();
        AttachedToVisualTree += (_, _) => DataContext ??= new DataGridSample.ViewModels.TreeViewMimicViewModel();
    }
}
