// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

using System.Collections.ObjectModel;
using System.Linq;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Headless.XUnit;
using Avalonia.Layout;
using Avalonia.Styling;
using Avalonia.VisualTree;
using Xunit;

namespace Avalonia.Controls.DataGridTests;

public class DataGridRowGroupHeaderAlignmentTests
{
    [AvaloniaFact]
    public void ContentVerticalAlignment_Default_Is_Center()
    {
        var header = new DataGridRowGroupHeader();

        Assert.Equal(VerticalAlignment.Center, header.ContentVerticalAlignment);
    }

    [AvaloniaFact]
    public void ContentVerticalAlignment_Is_Propagated_To_Template_Parts()
    {
        var window = new Window
        {
            Width = 400,
            Height = 300
        };
        window.SetThemeStyles();
        window.Styles.Add(new Style(x => x.OfType<DataGridRowGroupHeader>())
        {
            Setters =
            {
                new Setter(DataGridRowGroupHeader.ContentVerticalAlignmentProperty, VerticalAlignment.Top)
            }
        });

        var items = new ObservableCollection<Item>
        {
            new("A", "G1"),
            new("B", "G1"),
            new("C", "G2")
        };

        var view = new DataGridCollectionView(items);
        view.GroupDescriptions.Add(new DataGridPathGroupDescription(nameof(Item.Group)));

        var grid = new DataGrid
        {
            ItemsSource = view,
            HeadersVisibility = DataGridHeadersVisibility.Column
        };

        grid.ColumnsInternal.Add(new DataGridTextColumn
        {
            Header = "Name",
            Binding = new Binding(nameof(Item.Name))
        });

        window.Content = grid;
        window.Show();
        grid.UpdateLayout();

        try
        {
            var header = grid.GetVisualDescendants()
                .OfType<DataGridRowGroupHeader>()
                .First();

            var expander = header.GetVisualDescendants()
                .OfType<ToggleButton>()
                .First(control => control.Name == "PART_ExpanderButton");

            var contentPanel = header.GetVisualDescendants()
                .OfType<StackPanel>()
                .First(control => control.Orientation == Orientation.Horizontal);

            Assert.Equal(VerticalAlignment.Top, header.ContentVerticalAlignment);
            Assert.Equal(VerticalAlignment.Top, expander.VerticalAlignment);
            Assert.Equal(VerticalAlignment.Top, contentPanel.VerticalAlignment);
        }
        finally
        {
            window.Close();
        }
    }

    private sealed record Item(string Name, string Group);
}
