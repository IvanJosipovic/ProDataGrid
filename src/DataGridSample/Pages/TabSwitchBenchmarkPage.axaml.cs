using Avalonia.Controls;

namespace DataGridSample.Pages;

public partial class TabSwitchBenchmarkPage : UserControl
{
    public TabSwitchBenchmarkPage()
    {
        InitializeComponent();
        AttachedToVisualTree += (_, _) => DataContext ??= new DataGridSample.ViewModels.TabSwitchBenchmarkViewModel();
    }
}
