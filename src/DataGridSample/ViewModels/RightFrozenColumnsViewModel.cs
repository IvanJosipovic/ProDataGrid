using System;
using System.Collections.ObjectModel;
using DataGridSample.Models;
using DataGridSample.Mvvm;

namespace DataGridSample.ViewModels
{
    public class RightFrozenColumnsViewModel : ObservableObject
    {
        private int _frozenLeftColumnCount = 1;
        private int _frozenRightColumnCount = 1;

        public RightFrozenColumnsViewModel()
        {
            Populate();
        }

        public ObservableCollection<PixelItem> Items { get; } = new();

        public int FrozenLeftColumnCount
        {
            get => _frozenLeftColumnCount;
            set => SetProperty(ref _frozenLeftColumnCount, value);
        }

        public int FrozenRightColumnCount
        {
            get => _frozenRightColumnCount;
            set => SetProperty(ref _frozenRightColumnCount, value);
        }

        private void Populate()
        {
            Items.Clear();
            var random = new Random(42);
            for (int i = 1; i <= 200; i++)
            {
                Items.Add(PixelItem.Create(i, random));
            }
        }
    }
}
