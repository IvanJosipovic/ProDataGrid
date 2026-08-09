using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace DataGridSample.Pages
{
    public partial class PagingSelectionPage : UserControl
    {
        public PagingSelectionPage()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
