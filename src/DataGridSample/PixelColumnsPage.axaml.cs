using System.Collections.ObjectModel;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using DataGridSample.Models;

namespace DataGridSample
{
    public partial class PixelColumnsPage : UserControl
    {
        public ObservableCollection<PixelItem> Items { get; } = new();

        public PixelColumnsPage()
        {
            InitializeComponent();
            DataContext = this;
            Populate();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private void Populate()
        {
            Items.Clear();
            var random = new System.Random(7);
            for (int i = 1; i <= 150; i++)
            {
                Items.Add(PixelItem.Create(i, random));
            }
        }
    }
}
