using Avalonia.Controls;

namespace DataGridSample.Pages;

public partial class SelectionUnitsPage : UserControl
{
    public SelectionUnitsPage()
    {
        InitializeComponent();
        AttachedToVisualTree += (_, _) => DataContext ??= new DataGridSample.ViewModels.SelectionUnitsViewModel();
    }
}
