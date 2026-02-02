using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Headless.XUnit;
using Avalonia.Markup.Xaml.Styling;
using Avalonia.Themes.Fluent;
using ProDataGrid.ExcelSample.Helpers;
using ProDataGrid.ExcelSample.Models;
using Xunit;

namespace ProDataGrid.ExcelSample.Tests;

public sealed class DataGridSelectionBinderTests
{
    [AvaloniaFact]
    public void SelectionBinder_UpdatesStateFromGridSelection()
    {
        var items = new ObservableCollection<TextRow>
        {
            new TextRow { A = "A1", B = "B1" },
            new TextRow { A = "A2", B = "B2" }
        };

        var (window, grid) = CreateGrid(items);
        try
        {
            var state = new SpreadsheetSelectionState();
            DataGridSelectionBinder.SetSelectionState(grid, state);

            grid.CurrentCell = new DataGridCellInfo(items[0], grid.Columns[0], 0, 0, isValid: true);
            grid.SelectedCells = new List<DataGridCellInfo>
            {
                new DataGridCellInfo(items[0], grid.Columns[0], 0, 0, isValid: true),
                new DataGridCellInfo(items[0], grid.Columns[1], 0, 1, isValid: true),
                new DataGridCellInfo(items[1], grid.Columns[0], 1, 0, isValid: true),
                new DataGridCellInfo(items[1], grid.Columns[1], 1, 1, isValid: true)
            };

            Assert.Equal(new SpreadsheetCellReference(1, 1), state.CurrentCell);
            Assert.NotNull(state.SelectedRange);
            Assert.Equal(new SpreadsheetCellReference(0, 0), state.SelectedRange!.Value.Start);
            Assert.Equal(new SpreadsheetCellReference(1, 1), state.SelectedRange!.Value.End);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void SelectionBinder_UpdatesGridFromState()
    {
        var items = new ObservableCollection<TextRow>
        {
            new TextRow { A = "A1", B = "B1" },
            new TextRow { A = "A2", B = "B2" }
        };

        var (window, grid) = CreateGrid(items);
        try
        {
            var state = new SpreadsheetSelectionState();
            DataGridSelectionBinder.SetSelectionState(grid, state);

            state.SelectedRange = new SpreadsheetCellRange(
                new SpreadsheetCellReference(0, 0),
                new SpreadsheetCellReference(1, 1));
            state.CurrentCell = new SpreadsheetCellReference(0, 0);

            Assert.Equal(4, grid.SelectedCells.Count);
            Assert.True(grid.CurrentCell.IsValid);
            Assert.Equal(0, grid.CurrentCell.RowIndex);
            Assert.Equal(0, grid.CurrentCell.ColumnIndex);
        }
        finally
        {
            window.Close();
        }
    }

    private static (Window Window, DataGrid Grid) CreateGrid(ObservableCollection<TextRow> items)
    {
        var window = CreateWindow();

        var grid = new DataGrid
        {
            ItemsSource = items,
            SelectionMode = DataGridSelectionMode.Extended,
            SelectionUnit = DataGridSelectionUnit.CellOrRowHeader,
            AutoGenerateColumns = false,
            CanUserAddRows = false,
            CanUserDeleteRows = false
        };

        grid.Columns.Add(new DataGridTextColumn
        {
            Header = "A",
            Binding = new Binding(nameof(TextRow.A))
        });
        grid.Columns.Add(new DataGridTextColumn
        {
            Header = "B",
            Binding = new Binding(nameof(TextRow.B))
        });

        window.Content = grid;
        window.Show();
        grid.UpdateLayout();

        return (window, grid);
    }

    private static Window CreateWindow()
    {
        var window = new Window
        {
            Width = 640,
            Height = 480
        };

        window.Styles.Add(new FluentTheme());
        window.Styles.Add(new StyleInclude(new Uri("avares://Avalonia.Controls.DataGrid/Themes/"))
        {
            Source = new Uri("avares://Avalonia.Controls.DataGrid/Themes/Fluent.v2.xaml")
        });

        return window;
    }

    private sealed class TextRow
    {
        public string? A { get; set; }

        public string? B { get; set; }
    }
}
