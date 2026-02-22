using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace DataGridSample.Pages
{
    public partial class SelectionItemsSourceResetPage : UserControl
    {
        public SelectionItemsSourceResetPage()
        {
            InitializeComponent();
            AttachedToVisualTree += (_, _) => DataContext ??= new DataGridSample.ViewModels.SelectionItemsSourceResetViewModel();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
