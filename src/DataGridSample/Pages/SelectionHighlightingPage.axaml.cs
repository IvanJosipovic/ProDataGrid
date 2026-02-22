using Avalonia.Controls;

namespace DataGridSample.Pages;

public partial class SelectionHighlightingPage : UserControl
{
    public SelectionHighlightingPage()
    {
        InitializeComponent();
        AttachedToVisualTree += (_, _) => DataContext ??= new DataGridSample.ViewModels.SelectionHighlightingViewModel();
    }
}
