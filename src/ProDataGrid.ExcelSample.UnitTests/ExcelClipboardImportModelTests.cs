using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Avalonia.Controls;
using Avalonia.Controls.DataGridClipboard;
using Avalonia.Controls.DataGridFormulas;
using Avalonia.Data;
using Avalonia.Headless.XUnit;
using Avalonia.Markup.Xaml.Styling;
using Avalonia.Themes.Fluent;
using ProDataGrid.ExcelSample.Helpers;
using ProDataGrid.ExcelSample.Models;
using ProDataGrid.FormulaEngine;
using Xunit;

namespace ProDataGrid.ExcelSample.Tests;

public sealed class ExcelClipboardImportModelTests
{
    [AvaloniaFact]
    public void StructuredPaste_MapsByHeader()
    {
        var items = new ObservableCollection<TextRow>
        {
            new TextRow(),
            new TextRow()
        };

        var (window, grid) = CreateTextGrid(items);
        try
        {
            var model = new ExcelClipboardImportModel();
            var selectedCells = new List<DataGridCellInfo>
            {
                new DataGridCellInfo(items[0], grid.Columns[0], 0, 0, isValid: true)
            };
            var context = new DataGridClipboardImportContext(grid, "C\tA\nc1\ta1\nc2\ta2", selectedCells);

            var pasted = model.Paste(context);

            Assert.True(pasted);
            Assert.Equal("a1", items[0].A);
            Assert.Equal("c1", items[0].C);
            Assert.Equal("a2", items[1].A);
            Assert.Equal("c2", items[1].C);
            Assert.Null(items[0].B);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void PasteRepeatsIntoSelectionRange()
    {
        var items = new ObservableCollection<TextRow>
        {
            new TextRow(),
            new TextRow(),
            new TextRow(),
            new TextRow()
        };

        var (window, grid) = CreateTextGrid(items);
        try
        {
            var model = new ExcelClipboardImportModel();
            var selectedCells = new List<DataGridCellInfo>();

            for (var row = 0; row < 4; row++)
            {
                for (var column = 0; column < 4; column++)
                {
                    selectedCells.Add(new DataGridCellInfo(items[row], grid.Columns[column], row, column, isValid: true));
                }
            }

            var context = new DataGridClipboardImportContext(grid, "1\t2\n3\t4", selectedCells);

            var pasted = model.Paste(context);

            Assert.True(pasted);
            Assert.Equal("1", items[0].A);
            Assert.Equal("2", items[0].B);
            Assert.Equal("1", items[0].C);
            Assert.Equal("2", items[0].D);
            Assert.Equal("3", items[1].A);
            Assert.Equal("4", items[1].B);
            Assert.Equal("3", items[1].C);
            Assert.Equal("4", items[1].D);
            Assert.Equal("1", items[2].A);
            Assert.Equal("2", items[2].B);
            Assert.Equal("3", items[3].A);
            Assert.Equal("4", items[3].B);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void PasteSetsFormulaWhenTargetColumnSupportsFormulas()
    {
        var rows = new ObservableCollection<SpreadsheetRow>
        {
            new SpreadsheetRow(1, 1)
        };
        var definitions = new ObservableCollection<DataGridColumnDefinition>
        {
            new DataGridFormulaColumnDefinition
            {
                Header = "A",
                Formula = string.Empty,
                FormulaName = "A",
                ColumnKey = "A",
                AllowCellFormulas = true
            }
        };

        var formulaModel = new TestFormulaModel();
        var (window, grid) = CreateFormulaGrid(rows, definitions, formulaModel);
        try
        {
            var model = new ExcelClipboardImportModel();
            var selectedCells = new List<DataGridCellInfo>
            {
                new DataGridCellInfo(rows[0], grid.Columns[0], 0, 0, isValid: true)
            };
            var context = new DataGridClipboardImportContext(grid, "=1+2", selectedCells);

            var pasted = model.Paste(context);

            Assert.True(pasted);
            Assert.Equal("=1+2", formulaModel.LastFormula);
        }
        finally
        {
            window.Close();
        }
    }

    private static (Window Window, DataGrid Grid) CreateTextGrid(ObservableCollection<TextRow> items)
    {
        var window = CreateWindow();
        var grid = new DataGrid
        {
            ItemsSource = items,
            AutoGenerateColumns = false,
            CanUserAddRows = false,
            CanUserDeleteRows = false
        };

        grid.Columns.Add(new DataGridTextColumn { Header = "A", Binding = new Binding(nameof(TextRow.A)) });
        grid.Columns.Add(new DataGridTextColumn { Header = "B", Binding = new Binding(nameof(TextRow.B)) });
        grid.Columns.Add(new DataGridTextColumn { Header = "C", Binding = new Binding(nameof(TextRow.C)) });
        grid.Columns.Add(new DataGridTextColumn { Header = "D", Binding = new Binding(nameof(TextRow.D)) });

        window.Content = grid;
        window.Show();
        grid.UpdateLayout();

        return (window, grid);
    }

    private static (Window Window, DataGrid Grid) CreateFormulaGrid(
        ObservableCollection<SpreadsheetRow> rows,
        ObservableCollection<DataGridColumnDefinition> definitions,
        TestFormulaModel formulaModel)
    {
        var window = CreateWindow();
        var grid = new DataGrid
        {
            ItemsSource = rows,
            ColumnDefinitionsSource = definitions,
            AutoGenerateColumns = false,
            CanUserAddRows = false,
            CanUserDeleteRows = false,
            FormulaModel = formulaModel
        };

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

        public string? C { get; set; }

        public string? D { get; set; }
    }

    private sealed class TestFormulaModel : IDataGridFormulaModel
    {
        public event EventHandler<DataGridFormulaInvalidatedEventArgs>? Invalidated
        {
            add { }
            remove { }
        }

        public int FormulaVersion => 0;

        public FormulaCalculationMode CalculationMode { get; set; }

        public string? LastFormula { get; private set; }

        public void Attach(DataGrid grid)
        {
        }

        public void Detach()
        {
        }

        public object? Evaluate(object item, DataGridFormulaColumnDefinition column)
        {
            return null;
        }

        public void Invalidate()
        {
        }

        public void Recalculate()
        {
        }

        public string? GetCellFormula(object item, DataGridFormulaColumnDefinition column)
        {
            return null;
        }

        public bool TrySetCellFormula(object item, DataGridFormulaColumnDefinition column, string? formulaText, out string? error)
        {
            error = null;
            LastFormula = formulaText?.Trim();
            return true;
        }
    }
}
