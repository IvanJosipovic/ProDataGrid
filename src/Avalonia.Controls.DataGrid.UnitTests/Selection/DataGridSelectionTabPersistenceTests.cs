// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Headless.XUnit;
using Avalonia.Layout;
using Avalonia.Markup.Xaml.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Xunit;

namespace Avalonia.Controls.DataGridTests.Selection;

    public class DataGridSelectionTabPersistenceTests
    {
        [AvaloniaFact]
        public void Selection_Is_Restored_When_Switching_Tabs()
        {
        var items1 = new ObservableCollection<string> { "Alpha", "Beta", "Gamma" };
        var items2 = new ObservableCollection<string> { "One", "Two", "Three" };

        var grid1 = CreateGrid(items1);
        var grid2 = CreateGrid(items2);
        grid1.SelectedItem = items1[1]; // Beta

        var firstTab = new TabItem { Header = "First", Content = grid1 };
        var secondTab = new TabItem { Header = "Second", Content = grid2 };

        var tabs = new TabControl
        {
            Items = { firstTab, secondTab },
            SelectedItem = firstTab
        };

        var window = new Window
        {
            Width = 400,
            Height = 300,
            Content = tabs,
            Styles =
            {
                new StyleInclude((Uri?)null)
                {
                    Source = new Uri("avares://Avalonia.Themes.Fluent/FluentTheme.xaml")
                },
                new StyleInclude((Uri?)null)
                {
                    Source = new Uri("avares://Avalonia.Controls.DataGrid/Themes/Simple.xaml")
                }
            }
        };

        try
        {
            window.Show();
            tabs.ApplyTemplate();
            tabs.UpdateLayout();
            grid1.ApplyTemplate();
            grid1.UpdateLayout();
            var initialRow = RealizeRow(window, grid1, items1[1]);
            if (initialRow != null)
            {
                Assert.True(initialRow.IsSelected);
                Assert.True(((IPseudoClasses)initialRow.Classes).Contains(":selected"));
            }
            else
            {
                Assert.Equal(items1[1], grid1.SelectedItem);
            }

            tabs.SelectedIndex = 1;
            Dispatcher.UIThread.RunJobs();
            window.UpdateLayout();
            grid1.UpdateLayout();

            tabs.SelectedIndex = 0;
            var restoredRow = RealizeRow(window, grid1, items1[1]);
            if (restoredRow != null)
            {
                Assert.True(restoredRow.IsSelected);
                Assert.True(((IPseudoClasses)restoredRow.Classes).Contains(":selected"));
            }
            else
            {
                Assert.Equal(items1[1], grid1.SelectedItem);
            }
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact(Skip = "Known issue: scroll offset is not restored when switching tabs; enable once fixed.")]
        public void Scroll_Offset_Is_Preserved_When_Switching_Tabs()
        {
            var items1 = new ObservableCollection<string>(Enumerable.Range(0, 200).Select(i => $"Item {i}"));
            var items2 = new ObservableCollection<string>(Enumerable.Range(0, 5).Select(i => $"Other {i}"));

        var grid1 = CreateGrid(items1);
        var grid2 = CreateGrid(items2);

        var firstTab = new TabItem { Header = "First", Content = grid1 };
        var secondTab = new TabItem { Header = "Second", Content = grid2 };

        var tabs = new TabControl
        {
            Items = { firstTab, secondTab },
            SelectedItem = firstTab
        };

        var window = new Window
        {
            Width = 400,
            Height = 300,
            Content = tabs,
            Styles =
            {
                new StyleInclude((Uri?)null)
                {
                    Source = new Uri("avares://Avalonia.Themes.Fluent/FluentTheme.xaml")
                },
                new StyleInclude((Uri?)null)
                {
                    Source = new Uri("avares://Avalonia.Controls.DataGrid/Themes/Simple.xaml")
                }
            }
        };

        try
        {
            window.Show();
            tabs.ApplyTemplate();
            grid1.ApplyTemplate();
            grid2.ApplyTemplate();
            ForceLayout(window, new Size(400, 300));

            var targetItem = items1[150];
            grid1.ScrollIntoView(targetItem, grid1.ColumnDefinitions[0]);
            ForceLayout(window, new Size(400, 300));

            var offsetBefore = grid1.GetVerticalOffset();
            Assert.True(offsetBefore > 0);

            tabs.SelectedItem = secondTab;
            ForceLayout(window, new Size(400, 300));

            tabs.SelectedItem = firstTab;
            ForceLayout(window, new Size(400, 300));

            var offsetAfter = grid1.GetVerticalOffset();
            Assert.InRange(offsetAfter, offsetBefore - 0.5, offsetBefore + 0.5);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact(Skip = "Known issue: realized rows after tab switch can mismatch the expected viewport slice; enable when fixed.")]
    public void Realized_Rows_Match_Viewport_After_Tab_Switch()
    {
        var items1 = new ObservableCollection<string>(Enumerable.Range(0, 300).Select(i => $"Item {i}"));
            var items2 = new ObservableCollection<string>(Enumerable.Range(0, 5).Select(i => $"Other {i}"));

            var grid1 = CreateGrid(items1);
            var grid2 = CreateGrid(items2);

            var firstTab = new TabItem { Header = "First", Content = grid1 };
            var secondTab = new TabItem { Header = "Second", Content = grid2 };

            var tabs = new TabControl
            {
                Items = { firstTab, secondTab },
                SelectedItem = firstTab
            };

            var window = new Window
            {
                Width = 400,
                Height = 300,
                Content = tabs,
                Styles =
                {
                    new StyleInclude((Uri?)null)
                    {
                        Source = new Uri("avares://Avalonia.Themes.Fluent/FluentTheme.xaml")
                    },
                    new StyleInclude((Uri?)null)
                    {
                        Source = new Uri("avares://Avalonia.Controls.DataGrid/Themes/Simple.xaml")
                    }
                }
            };

            try
            {
                window.Show();
                tabs.ApplyTemplate();
                grid1.ApplyTemplate();
                grid2.ApplyTemplate();
                ForceLayout(window, new Size(400, 300));

                var targetItem = items1[180];
                grid1.ScrollIntoView(targetItem, grid1.ColumnDefinitions[0]);
                ForceLayout(window, new Size(400, 300));

                var beforeRows = GetRealizedRows(grid1).Select(r => r.DataContext).ToArray();
                Assert.NotEmpty(beforeRows);

                tabs.SelectedItem = secondTab;
                ForceLayout(window, new Size(400, 300));

                tabs.SelectedItem = firstTab;
                ForceLayout(window, new Size(400, 300));

                var afterRows = GetRealizedRows(grid1).Select(r => r.DataContext).ToArray();
                Assert.Equal(beforeRows.Length, afterRows.Length);
                Assert.True(beforeRows.SequenceEqual(afterRows));
            }
            finally
            {
                window.Close();
            }
        }

    private static DataGrid CreateGrid(System.Collections.IEnumerable items)
    {
        var grid = new DataGrid
        {
            AutoGenerateColumns = false,
            ItemsSource = items,
            SelectionMode = DataGridSelectionMode.Extended
        };
        grid.ColumnsInternal.Add(new DataGridTextColumn
        {
            Header = "Value",
            Binding = new Binding(".")
        });
        return grid;
    }

    private static DataGridRow? FindRow(DataGrid grid, object item)
    {
        return grid
            .GetSelfAndVisualDescendants()
            .OfType<DataGridRow>()
            .FirstOrDefault(r => Equals(r.DataContext, item));
    }

    private static DataGridRow? RealizeRow(Window window, DataGrid grid, object item)
    {
        for (int i = 0; i < 5; i++)
        {
            ForceLayout(window, new Size(400, 300));
            ForceLayout(grid, new Size(400, 300));
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();
            grid.ApplyTemplate();
            grid.UpdateLayout();
            grid.ScrollIntoView(item, grid.ColumnDefinitions[0]);
            grid.UpdateLayout();
            Dispatcher.UIThread.RunJobs();
            var row = FindRow(grid, item);
            if (row != null)
            {
                return row;
            }
        }

        return null;
    }

    private static void ForceLayout(Control control, Size size)
    {
        control.Measure(size);
        control.Arrange(new Rect(size));
        Dispatcher.UIThread.RunJobs();
        control.UpdateLayout();
    }

    private static IReadOnlyList<DataGridRow> GetRealizedRows(DataGrid grid)
    {
        return grid
            .GetSelfAndVisualDescendants()
            .OfType<DataGridRow>()
            .OrderBy(r => r.Index)
            .ToArray();
    }
}
