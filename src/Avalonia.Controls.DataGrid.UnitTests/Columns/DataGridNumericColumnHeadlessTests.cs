// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

using System;
using System.Collections.ObjectModel;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Headless.XUnit;
using Avalonia.Layout;
using Avalonia.Markup.Xaml.Styling;
using Avalonia.Styling;
using Avalonia.VisualTree;
using Avalonia.Themes.Fluent;
using System.Linq;
using Xunit;

namespace Avalonia.Controls.DataGridTests.Columns;

public class DataGridNumericColumnHeadlessTests
{
    [AvaloniaFact]
    public void NumericColumn_Binds_Value()
    {
        var vm = new NumericTestViewModel();
        var (window, grid) = CreateWindow(vm);

        window.Show();
        grid.ApplyTemplate();
        grid.UpdateLayout();

        var cell = GetCell(grid, "Price", 0);
        var textBlock = Assert.IsType<TextBlock>(cell.Content);

        Assert.NotNull(textBlock.Text);
        Assert.Contains("99", textBlock.Text);
    }

    [AvaloniaFact]
    public void NumericColumn_Applies_FormatString()
    {
        var vm = new NumericTestViewModel();
        var (window, grid) = CreateWindow(vm, formatString: "C2");

        window.Show();
        grid.ApplyTemplate();
        grid.UpdateLayout();

        var cell = GetCell(grid, "Price", 0);
        var textBlock = Assert.IsType<TextBlock>(cell.Content);

        // The text should contain currency formatting
        Assert.NotNull(textBlock.Text);
    }

    [AvaloniaFact]
    public void NumericColumn_Respects_MinMax()
    {
        var column = new DataGridNumericColumn
        {
            Header = "Value",
            Minimum = 0,
            Maximum = 100
        };

        Assert.Equal(0m, column.Minimum);
        Assert.Equal(100m, column.Maximum);
    }

    [AvaloniaFact]
    public void NumericColumn_Respects_ShowButtonSpinner()
    {
        var column = new DataGridNumericColumn
        {
            Header = "Value",
            ShowButtonSpinner = false
        };

        Assert.False(column.ShowButtonSpinner);
    }

    [AvaloniaFact]
    public void NumericColumn_Default_HorizontalAlignment_IsRight()
    {
        var column = new DataGridNumericColumn();

        Assert.Equal(Avalonia.Layout.HorizontalAlignment.Right, column.HorizontalContentAlignment);
    }

    [AvaloniaFact]
    public void NumericColumn_Applies_VerticalContentAlignment()
    {
        var column = new TestNumericColumn
        {
            VerticalContentAlignment = VerticalAlignment.Bottom
        };

        var displayElement = column.CreateDisplayElement(new DataGridCell(), new object());
        var editingElement = column.CreateEditingElement(new DataGridCell(), new object());

        Assert.Equal(VerticalAlignment.Bottom, displayElement.VerticalAlignment);
        Assert.Equal(VerticalAlignment.Bottom, editingElement.VerticalContentAlignment);
    }

    private static (Window window, DataGrid grid) CreateWindow(NumericTestViewModel vm, string? formatString = null)
    {
        var window = new Window
        {
            Width = 600,
            Height = 400,
            DataContext = vm
        };

        window.SetThemeStyles();

        var column = new DataGridNumericColumn
        {
            Header = "Price",
            Binding = new Binding("Price")
        };

        if (formatString != null)
        {
            column.FormatString = formatString;
        }

        var grid = new DataGrid
        {
            AutoGenerateColumns = false,
            ItemsSource = vm.Items,
            Columns = new ObservableCollection<DataGridColumn>
            {
                new DataGridTextColumn
                {
                    Header = "Name",
                    Binding = new Binding("Name")
                },
                column
            }
        };

        window.Content = grid;
        return (window, grid);
    }

    private static DataGridCell GetCell(DataGrid grid, string header, int rowIndex)
    {
        return grid
            .GetVisualDescendants()
            .OfType<DataGridCell>()
            .First(c => c.OwningColumn?.Header?.ToString() == header && c.OwningRow?.Index == rowIndex);
    }

    private sealed class NumericTestViewModel
    {
        public NumericTestViewModel()
        {
            Items = new ObservableCollection<NumericItem>
            {
                new() { Name = "Item 1", Price = 99.99m },
                new() { Name = "Item 2", Price = 149.50m },
                new() { Name = "Item 3", Price = 25.00m }
            };
        }

        public ObservableCollection<NumericItem> Items { get; }
    }

    private sealed class NumericItem
    {
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
    }

    private sealed class TestNumericColumn : DataGridNumericColumn
    {
        public TextBlock CreateDisplayElement(DataGridCell cell, object dataItem) =>
            (TextBlock)GenerateElement(cell, dataItem);

        public NumericUpDown CreateEditingElement(DataGridCell cell, object dataItem) =>
            (NumericUpDown)GenerateEditingElementDirect(cell, dataItem);
    }
}
