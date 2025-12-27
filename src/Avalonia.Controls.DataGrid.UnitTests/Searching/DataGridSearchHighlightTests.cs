// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Controls.DataGridSearching;
using Avalonia.Data;
using Avalonia.Headless.XUnit;
using Avalonia.Styling;
using Avalonia.VisualTree;
using Xunit;

namespace Avalonia.Controls.DataGridTests.Searching;

public class DataGridSearchHighlightTests
{
    [AvaloniaFact]
    public void SearchMatch_PseudoClasses_Applied_To_Row_And_Cell()
    {
        var items = new ObservableCollection<Person>
        {
            new("Alpha", "Engineering"),
            new("Bravo", "Design")
        };

        var (grid, root) = CreateGrid(items);
        try
        {
            grid.SearchModel.HighlightMode = SearchHighlightMode.Cell;
            grid.SearchModel.HighlightCurrent = true;

            grid.SearchModel.SetOrUpdate(new SearchDescriptor("Alpha", comparison: StringComparison.OrdinalIgnoreCase));
            grid.UpdateLayout();

            var row = FindRow(items[0], grid);
            var cell = row.Cells[0];

            Assert.True(((IPseudoClasses)row.Classes).Contains(":searchmatch"));
            Assert.True(((IPseudoClasses)cell.Classes).Contains(":searchmatch"));
            Assert.True(((IPseudoClasses)cell.Classes).Contains(":searchcurrent"));

            var otherRow = FindRow(items[1], grid);
            Assert.False(((IPseudoClasses)otherRow.Classes).Contains(":searchmatch"));
        }
        finally
        {
            root.Close();
        }
    }

    [AvaloniaFact]
    public void SearchHighlightMode_None_Does_Not_Apply_PseudoClasses()
    {
        var items = new ObservableCollection<Person>
        {
            new("Alpha", "Engineering")
        };

        var (grid, root) = CreateGrid(items);
        try
        {
            grid.SearchModel.HighlightMode = SearchHighlightMode.None;
            grid.SearchModel.HighlightCurrent = true;

            grid.SearchModel.SetOrUpdate(new SearchDescriptor("Alpha", comparison: StringComparison.OrdinalIgnoreCase));
            grid.UpdateLayout();

            var row = FindRow(items[0], grid);
            var cell = row.Cells[0];

            Assert.False(((IPseudoClasses)row.Classes).Contains(":searchmatch"));
            Assert.False(((IPseudoClasses)cell.Classes).Contains(":searchmatch"));
            Assert.False(((IPseudoClasses)cell.Classes).Contains(":searchcurrent"));
        }
        finally
        {
            root.Close();
        }
    }

    [AvaloniaFact]
    public void TextHighlight_Creates_Inlines_For_Matches()
    {
        var items = new ObservableCollection<Person>
        {
            new("Alpha", "Engineering")
        };

        var (grid, root) = CreateGrid(items);
        try
        {
            grid.SearchModel.HighlightMode = SearchHighlightMode.TextAndCell;
            grid.SearchModel.HighlightCurrent = true;

            grid.SearchModel.SetOrUpdate(new SearchDescriptor("Al", comparison: StringComparison.OrdinalIgnoreCase));
            grid.UpdateLayout();

            var row = FindRow(items[0], grid);
            var cell = row.Cells[0];
            var textBlock = Assert.IsType<DataGridSearchTextBlock>(cell.Content);

            var runs = textBlock.Inlines?.OfType<Run>().ToList();
            Assert.NotNull(runs);
            Assert.True(runs!.Count > 0);
            Assert.Contains(runs, run => run.Background != null);
        }
        finally
        {
            root.Close();
        }
    }

    [AvaloniaFact]
    public void ClearingSearchResults_Restores_Text_For_TextHighlight()
    {
        var items = new ObservableCollection<Person>
        {
            new("Alpha", "Engineering")
        };

        var (grid, root) = CreateGrid(items);
        try
        {
            grid.SearchModel.HighlightMode = SearchHighlightMode.TextAndCell;

            grid.SearchModel.SetOrUpdate(new SearchDescriptor("Al", comparison: StringComparison.OrdinalIgnoreCase));
            grid.UpdateLayout();

            var row = FindRow(items[0], grid);
            var cell = row.Cells[0];
            var textBlock = Assert.IsType<DataGridSearchTextBlock>(cell.Content);

            Assert.NotNull(textBlock.Inlines);
            Assert.True(textBlock.Inlines!.Count > 0);

            grid.SearchModel.Clear();
            grid.UpdateLayout();

            var runs = textBlock.Inlines?.OfType<Run>().ToList();
            Assert.NotNull(runs);
            Assert.True(runs!.Count > 0);
            Assert.Equal("Alpha", string.Concat(runs.Select(run => run.Text)));
        }
        finally
        {
            root.Close();
        }
    }

    [AvaloniaFact]
    public void MoveNext_Updates_Current_PseudoClass()
    {
        var items = new ObservableCollection<Person>
        {
            new("Alpha", "Engineering"),
            new("Alpha", "Design")
        };

        var (grid, root) = CreateGrid(items);
        try
        {
            grid.SearchModel.HighlightMode = SearchHighlightMode.Cell;
            grid.SearchModel.HighlightCurrent = true;

            grid.SearchModel.SetOrUpdate(new SearchDescriptor("Alpha", comparison: StringComparison.OrdinalIgnoreCase));
            grid.UpdateLayout();

            var firstRow = FindRow(items[0], grid);
            var secondRow = FindRow(items[1], grid);

            Assert.True(((IPseudoClasses)firstRow.Classes).Contains(":searchcurrent"));
            Assert.False(((IPseudoClasses)secondRow.Classes).Contains(":searchcurrent"));

            grid.SearchModel.MoveNext();
            grid.UpdateLayout();

            Assert.False(((IPseudoClasses)firstRow.Classes).Contains(":searchcurrent"));
            Assert.True(((IPseudoClasses)secondRow.Classes).Contains(":searchcurrent"));
        }
        finally
        {
            root.Close();
        }
    }

    private static (DataGrid grid, Window root) CreateGrid(IList<Person> people)
    {
        var root = new Window
        {
            Width = 320,
            Height = 200,
        };

        root.SetThemeStyles();

        var grid = new DataGrid
        {
            HeadersVisibility = DataGridHeadersVisibility.All,
            ItemsSource = people
        };

        grid.ColumnsInternal.Add(new DataGridTextColumn
        {
            Header = "Name",
            Binding = new Binding(nameof(Person.Name))
        });

        grid.ColumnsInternal.Add(new DataGridTextColumn
        {
            Header = "Role",
            Binding = new Binding(nameof(Person.Role))
        });

        root.Content = grid;
        root.Show();
        grid.UpdateLayout();
        return (grid, root);
    }

    private static DataGridRow FindRow(object item, DataGrid grid)
    {
        return grid
            .GetSelfAndVisualDescendants()
            .OfType<DataGridRow>()
            .First(r => ReferenceEquals(r.DataContext, item));
    }

    private class Person
    {
        public Person(string name, string role)
        {
            Name = name;
            Role = role;
        }

        public string Name { get; }
        public string Role { get; }
    }
}
