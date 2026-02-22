using Avalonia.Controls;

namespace DataGridSample.Pages;

public partial class GroupedSelectionPage : UserControl
{
    public GroupedSelectionPage()
    {
        InitializeComponent();
        AttachedToVisualTree += (_, _) => DataContext ??= new DataGridSample.ViewModels.GroupedSelectionViewModel();
    }
}
