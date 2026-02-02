using System;
using System.Collections.ObjectModel;
using System.Globalization;
using Avalonia.Controls;
using Avalonia.Controls.DataGridFormulas;
using ProDataGrid.ExcelSample.Helpers;
using ProDataGrid.ExcelSample.Models;
using ReactiveUI;

namespace ProDataGrid.ExcelSample.ViewModels;

public sealed class SheetViewModel : ReactiveObject
{
    private const int DefaultRowCount = 200;
    private const int DefaultColumnCount = 15;
    private const int SpillAnchorRow = 0;
    private const int SpillAnchorColumn = 12;
    private bool _spillSeeded;
    private readonly string _spillFormula;

    public SheetViewModel(string name, int rowCount = DefaultRowCount, int columnCount = DefaultColumnCount)
    {
        Name = name;
        Rows = new ObservableCollection<SpreadsheetRow>();
        ColumnDefinitions = BuildColumnDefinitions(columnCount);
        FormulaModel = new DataGridFormulaModel();
        FormulaModel.Invalidated += (_, __) => SeedSpillFormulasIfNeeded();
        _spillFormula = BuildSpillFormula(CultureInfo.CurrentCulture);

        for (var i = 0; i < rowCount; i++)
        {
            Rows.Add(new SpreadsheetRow(columnCount, i + 1));
        }

        SeedRows();
    }

    public string Name { get; }

    public ObservableCollection<SpreadsheetRow> Rows { get; }

    public ObservableCollection<DataGridColumnDefinition> ColumnDefinitions { get; }

    public DataGridFormulaModel FormulaModel { get; }

    public bool TryGetCell(SpreadsheetCellReference cell, out string? displayValue)
    {
        displayValue = string.Empty;
        if (!TryGetRow(cell.RowIndex, out var row) || !TryGetColumn(cell.ColumnIndex, out var definition))
        {
            return false;
        }

        if (definition is DataGridFormulaColumnDefinition formulaDefinition)
        {
            displayValue = FormulaModel.GetCellFormula(row, formulaDefinition) ?? string.Empty;
            return true;
        }

        var value = row.GetCell(cell.ColumnIndex);
        displayValue = value == null ? string.Empty : Convert.ToString(value, CultureInfo.CurrentCulture);
        return true;
    }

    public bool TryApplyFormula(SpreadsheetCellReference cell, string? input, out string? error)
    {
        error = null;
        if (!TryGetRow(cell.RowIndex, out var row) || !TryGetColumn(cell.ColumnIndex, out var definition))
        {
            error = "Selection is out of range.";
            return false;
        }

        var trimmed = input?.Trim() ?? string.Empty;
        if (definition is DataGridFormulaColumnDefinition formulaDefinition)
        {
            return FormulaModel.TrySetCellFormula(row, formulaDefinition, trimmed, out error);
        }

        if (trimmed.Length == 0)
        {
            row.SetCell(cell.ColumnIndex, (object?)null);
            return true;
        }

        if (trimmed.StartsWith("=", StringComparison.Ordinal))
        {
            error = "Formulas are only supported in calculated columns.";
            return false;
        }

        if (definition is DataGridNumericColumnDefinition)
        {
            if (!double.TryParse(trimmed, NumberStyles.Any, CultureInfo.CurrentCulture, out var number))
            {
                error = "Invalid number.";
                return false;
            }

            row.SetCell(cell.ColumnIndex, number);
            return true;
        }

        row.SetCell(cell.ColumnIndex, trimmed);
        return true;
    }

    private static ObservableCollection<DataGridColumnDefinition> BuildColumnDefinitions(int columnCount)
    {
        var builder = DataGridColumnDefinitionBuilder.For<SpreadsheetRow>();
        var columns = new ObservableCollection<DataGridColumnDefinition>();

        for (var columnIndex = 0; columnIndex < columnCount; columnIndex++)
        {
            var header = ExcelColumnName.FromIndex(columnIndex);
            var slot = columnIndex;
            var width = columnIndex switch
            {
                0 => new DataGridLength(160),
                4 or 6 => new DataGridLength(110),
                _ => new DataGridLength(96)
            };

            if (columnIndex >= 12)
            {
                var spillColumn = builder.Formula(
                    header: header,
                    formula: string.Empty,
                    formulaName: header,
                    configure: column =>
                    {
                        column.ColumnKey = header;
                        column.Width = width;
                        column.AllowCellFormulas = true;
                    });

                columns.Add(spillColumn);
                continue;
            }

            if (columnIndex == 4)
            {
                var totalColumn = builder.Formula(
                    header: header,
                    formula: "=([@B]*[@C])*(1-[@D])",
                    formulaName: header,
                    configure: column =>
                    {
                        column.ColumnKey = header;
                        column.Width = width;
                        column.IsReadOnly = true;
                        column.AllowCellFormulas = true;
                    });

                columns.Add(totalColumn);
                continue;
            }

            if (columnIndex == 6)
            {
                var taxColumn = builder.Formula(
                    header: header,
                    formula: "=[@E]/5",
                    formulaName: header,
                    configure: column =>
                    {
                        column.ColumnKey = header;
                        column.Width = width;
                        column.IsReadOnly = true;
                        column.AllowCellFormulas = true;
                    });

                columns.Add(taxColumn);
                continue;
            }

            if (columnIndex is 1 or 2 or 3 or 8 or 10)
            {
                var property = ColumnDefinitionBindingFactory.CreateProperty<SpreadsheetRow, double>(
                    header,
                    row => row.GetCell<double>(slot),
                    (row, value) => row.SetCell(slot, value));

                var numericColumn = builder.Numeric(
                    header: header,
                    property: property,
                    getter: row => row.GetCell<double>(slot),
                    setter: (row, value) => row.SetCell(slot, value),
                    configure: column =>
                    {
                        column.ColumnKey = header;
                        column.Width = width;
                        column.FormatString = columnIndex is 3 ? "P0" : "N2";
                    });

                columns.Add(numericColumn);
                continue;
            }

            var textProperty = ColumnDefinitionBindingFactory.CreateProperty<SpreadsheetRow, string?>(
                header,
                row => row.GetCell<string?>(slot),
                (row, value) => row.SetCell(slot, value));

            var textColumn = builder.Text(
                header: header,
                property: textProperty,
                getter: row => row.GetCell<string?>(slot),
                setter: (row, value) => row.SetCell(slot, value),
                configure: column =>
                {
                    column.ColumnKey = header;
                    column.Width = width;
                });

            columns.Add(textColumn);
        }

        return columns;
    }

    private bool TryGetRow(int rowIndex, out SpreadsheetRow row)
    {
        row = null!;
        if ((uint)rowIndex >= (uint)Rows.Count)
        {
            return false;
        }

        row = Rows[rowIndex];
        return true;
    }

    private bool TryGetColumn(int columnIndex, out DataGridColumnDefinition definition)
    {
        definition = null!;
        if ((uint)columnIndex >= (uint)ColumnDefinitions.Count)
        {
            return false;
        }

        definition = ColumnDefinitions[columnIndex];
        return true;
    }

    private void SeedRows()
    {
        var regions = new[] { "North", "South", "East", "West" };
        var owners = new[] { "Morgan", "Alex", "Taylor", "Jordan" };
        var statuses = new[] { "Open", "In Progress", "Review", "Done" };
        var random = new Random(1337);

        for (var i = 0; i < Rows.Count; i++)
        {
            var row = Rows[i];
            TrySetCell(row, 0, $"Item {i + 1:000}");
            TrySetCell(row, 1, random.Next(1, 50));
            TrySetCell(row, 2, Math.Round(random.NextDouble() * 250 + 10, 2));
            TrySetCell(row, 3, Math.Round(random.NextDouble() * 0.25, 2));
            TrySetCell(row, 5, regions[random.Next(regions.Length)]);
            TrySetCell(row, 7, owners[random.Next(owners.Length)]);
            TrySetCell(row, 8, Math.Round(random.NextDouble() * 100, 2));
            TrySetCell(row, 9, statuses[random.Next(statuses.Length)]);
            TrySetCell(row, 10, Math.Round(random.NextDouble() * 2 - 1, 2));
            TrySetCell(row, 11, i % 6 == 0 ? "Follow up" : string.Empty);
        }
    }

    private static void TrySetCell(SpreadsheetRow row, int columnIndex, object? value)
    {
        if ((uint)columnIndex >= (uint)row.ColumnCount)
        {
            return;
        }

        row.SetCell(columnIndex, value);
    }

    private void SeedSpillFormulasIfNeeded()
    {
        if (_spillSeeded)
        {
            return;
        }

        if (Rows.Count <= SpillAnchorRow || ColumnDefinitions.Count <= SpillAnchorColumn)
        {
            return;
        }

        if (ColumnDefinitions[SpillAnchorColumn] is not DataGridFormulaColumnDefinition formulaDefinition)
        {
            return;
        }

        var row = Rows[SpillAnchorRow];
        var existing = FormulaModel.GetCellFormula(row, formulaDefinition);
        if (!string.IsNullOrWhiteSpace(existing))
        {
            _spillSeeded = true;
            return;
        }

        if (FormulaModel.TrySetCellFormula(row, formulaDefinition, _spillFormula, out _))
        {
            _spillSeeded = true;
        }
    }

    private static string BuildSpillFormula(CultureInfo culture)
    {
        var listSeparator = culture.TextInfo.ListSeparator;
        var separator = string.IsNullOrEmpty(listSeparator) ? ',' : listSeparator[0];
        return $"=SEQUENCE(5{separator}3{separator}1{separator}1)";
    }
}
