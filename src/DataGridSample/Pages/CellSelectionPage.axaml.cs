using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using DataGridSample.Models;

namespace DataGridSample.Pages
{
    public partial class CellSelectionPage : UserControl
    {
        public CellSelectionPage()
        {
            Items = new ObservableCollection<Country>(Countries.All.Take(20).ToList());
            SelectedCells = new ObservableCollection<DataGridCellInfo>();
            SelectionLog = new ObservableCollection<string>();
            SelectionUnits = Enum.GetValues<DataGridSelectionUnit>();

            InitializeComponent();
            AttachedToVisualTree += (_, _) => DataContext ??= this;

            SelectedCells.CollectionChanged += OnSelectedCellsChanged;
        }

        public ObservableCollection<Country> Items { get; }

        public ObservableCollection<DataGridCellInfo> SelectedCells { get; }

        public ObservableCollection<string> SelectionLog { get; }

        public Array SelectionUnits { get; }

        private void OnSelectedCellsChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            SelectionLog.Insert(0, $"Add {e.NewItems?.Count ?? 0}, Remove {e.OldItems?.Count ?? 0}, Total {SelectedCells.Count}");
            if (SelectionLog.Count > 60)
            {
                SelectionLog.RemoveAt(SelectionLog.Count - 1);
            }
        }

        private void SelectFirstRow(object? sender, RoutedEventArgs e)
        {
            var columns = CellGrid.Columns.ToList();
            if (columns.Count == 0 || Items.Count == 0)
            {
                return;
            }

            SelectedCells.Clear();

            var item = Items[0];
            int rowIndex = 0;
            for (int colIndex = 0; colIndex < columns.Count; colIndex++)
            {
                var column = columns[colIndex];
                if (column == null || !column.IsVisible)
                {
                    continue;
                }

                SelectedCells.Add(new DataGridCellInfo(item, column, rowIndex, colIndex, isValid: true));
            }
        }

        private void SelectDiagonal(object? sender, RoutedEventArgs e)
        {
            SelectedCells.Clear();

            var columns = CellGrid.Columns.ToList();
            if (columns.Count == 0 || Items.Count == 0)
            {
                return;
            }

            int count = Math.Min(Items.Count, columns.Count);
            for (int i = 0; i < count; i++)
            {
                var column = columns[i];
                if (column == null || !column.IsVisible)
                {
                    continue;
                }

                SelectedCells.Add(new DataGridCellInfo(Items[i], column, i, i, isValid: true));
            }
        }

        private void ClearSelection(object? sender, RoutedEventArgs e)
        {
            SelectedCells.Clear();
        }
    }
}
