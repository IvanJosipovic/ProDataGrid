using Avalonia.Controls;

namespace DataGridSample.Pages;

public partial class TreeViewMimicSortModelPage : UserControl
{
    public TreeViewMimicSortModelPage()
    {
        InitializeComponent();
        AttachedToVisualTree += (_, _) => DataContext ??= new DataGridSample.ViewModels.TreeViewMimicSortModelViewModel();
    }
}
