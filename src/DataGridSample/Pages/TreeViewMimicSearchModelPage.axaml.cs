using Avalonia.Controls;

namespace DataGridSample.Pages;

public partial class TreeViewMimicSearchModelPage : UserControl
{
    public TreeViewMimicSearchModelPage()
    {
        InitializeComponent();
        AttachedToVisualTree += (_, _) => DataContext ??= new DataGridSample.ViewModels.TreeViewMimicSearchModelViewModel();
    }
}
