using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using DataGridSample.ViewModels;

namespace DataGridSample
{
    public partial class FrozenColumnsPage : UserControl
    {
        public FrozenColumnsPage()
        {
            InitializeComponent();
            AttachedToVisualTree += (_, _) => DataContext ??= new FrozenColumnsViewModel();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
