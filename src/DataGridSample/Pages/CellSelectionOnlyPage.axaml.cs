using Avalonia.Controls;

namespace DataGridSample.Pages;

public partial class CellSelectionOnlyPage : UserControl
{
    public CellSelectionOnlyPage()
    {
        InitializeComponent();
        AttachedToVisualTree += (_, _) => DataContext ??= new DataGridSample.ViewModels.CellSelectionOnlyViewModel();
    }
}
