using Avalonia.Controls;

namespace DataGridSample.Pages;

public partial class ColumnThemesPage : UserControl
{
    public ColumnThemesPage()
    {
        InitializeComponent();
        AttachedToVisualTree += (_, _) => DataContext ??= new DataGridSample.ViewModels.ColumnThemesViewModel();
    }
}
