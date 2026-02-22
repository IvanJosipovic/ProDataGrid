using Avalonia.Controls;

namespace DataGridSample.Pages;

public partial class TreeViewMimicFilterModelPage : UserControl
{
    public TreeViewMimicFilterModelPage()
    {
        InitializeComponent();
        AttachedToVisualTree += (_, _) => DataContext ??= new DataGridSample.ViewModels.TreeViewMimicFilterModelViewModel();
    }
}
