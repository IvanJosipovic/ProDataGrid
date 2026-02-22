using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace DataGridSample.Pages
{
    public partial class ColumnDragIndicatorHeaderBindingPage : UserControl
    {
        public ColumnDragIndicatorHeaderBindingPage()
        {
            InitializeComponent();
            AttachedToVisualTree += (_, _) => DataContext ??= new DataGridSample.ViewModels.ColumnDragIndicatorHeaderBindingViewModel();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
