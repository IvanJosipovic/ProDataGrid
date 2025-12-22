// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Headless.XUnit;
using Avalonia.Layout;
using Avalonia.Styling;
using Xunit;

namespace Avalonia.Controls.DataGridTests.Columns;

public class DataGridComboBoxColumnThemeTests
{
    [AvaloniaFact]
    public void ComboBoxColumn_Reuses_Display_And_Edit_Themes()
    {
        var editTheme = new ControlTheme(typeof(ComboBox));
        var displayTheme = new ControlTheme(typeof(ComboBox));

        var grid = new DataGrid();
        grid.Resources.Add("DataGridCellComboBoxTheme", editTheme);
        grid.Resources.Add("DataGridCellComboBoxDisplayTheme", displayTheme);

        var column = new DerivedComboBoxColumn
        {
            ItemsSource = new List<string> { "A", "B" },
            SelectedItemBinding = new Binding(nameof(Choice.Value))
        };

        grid.ColumnsInternal.Add(column);

        var displayElement = column.CreateDisplayElement(new DataGridCell(), new Choice());
        var editingElement = column.CreateEditingElement(new DataGridCell(), new Choice());
        var (edit, display) = column.GetThemes();

        Assert.Same(displayTheme, displayElement.Theme);
        Assert.Same(editTheme, editingElement.Theme);
        Assert.Same(editTheme, edit);
        Assert.Same(displayTheme, display);
    }

    [AvaloniaFact]
    public void ComboBox_Themes_Not_Locked_When_Accessed_Before_Attach()
    {
        var editTheme = new ControlTheme(typeof(ComboBox));
        var displayTheme = new ControlTheme(typeof(ComboBox));

        var grid = new DataGrid();
        grid.Resources.Add("DataGridCellComboBoxTheme", editTheme);
        grid.Resources.Add("DataGridCellComboBoxDisplayTheme", displayTheme);

        var column = new DerivedComboBoxColumn
        {
            ItemsSource = new List<string> { "A", "B" },
            SelectedItemBinding = new Binding(nameof(Choice.Value))
        };

        // Touch themes before adding to grid.
        var pre = column.GetThemes();
        Assert.Null(pre.editTheme);
        Assert.Null(pre.displayTheme);

        grid.ColumnsInternal.Add(column);

        var post = column.GetThemes();
        Assert.Same(editTheme, post.editTheme);
        Assert.Same(displayTheme, post.displayTheme);
    }

    [AvaloniaFact]
    public void ComboBoxColumn_Applies_ItemTemplate_And_ContentAlignment()
    {
        var template = new FuncDataTemplate<string>((_, _) => new TextBlock());
        var column = new DerivedComboBoxColumn
        {
            ItemTemplate = template,
            HorizontalContentAlignment = HorizontalAlignment.Center,
            VerticalContentAlignment = VerticalAlignment.Bottom
        };

        var displayElement = column.CreateDisplayElement(new DataGridCell(), "A");
        var editingElement = column.CreateEditingElement(new DataGridCell(), "A");

        Assert.Same(template, displayElement.ItemTemplate);
        Assert.Same(template, editingElement.ItemTemplate);
        Assert.Equal(HorizontalAlignment.Center, displayElement.HorizontalContentAlignment);
        Assert.Equal(VerticalAlignment.Bottom, displayElement.VerticalContentAlignment);
        Assert.Equal(HorizontalAlignment.Center, editingElement.HorizontalContentAlignment);
        Assert.Equal(VerticalAlignment.Bottom, editingElement.VerticalContentAlignment);
    }

    private class DerivedComboBoxColumn : DataGridComboBoxColumn
    {
        public ComboBox CreateDisplayElement(DataGridCell cell, object dataItem) =>
            (ComboBox)base.GenerateElement(cell, dataItem);

        public ComboBox CreateEditingElement(DataGridCell cell, object dataItem)
        {
            var element = (ComboBox)GenerateEditingElement(cell, dataItem, out _);
            return element;
        }

        public (ControlTheme editTheme, ControlTheme displayTheme) GetThemes() =>
            (CellComboBoxTheme, CellComboBoxDisplayTheme);
    }

    private class Choice
    {
        public string Value { get; set; } = string.Empty;
    }
}
