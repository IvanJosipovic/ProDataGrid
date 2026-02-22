using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace DataGridSample.Pages;

public partial class FocusLossOnScrollPage : UserControl
{
    public FocusLossOnScrollPage()
    {
        InitializeComponent();
        AttachedToVisualTree += (_, _) => DataContext ??= new DataGridSample.ViewModels.FocusLossOnScrollViewModel();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
