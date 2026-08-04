using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Controls.DataGridBanding;
using Avalonia.Controls.Primitives;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Xunit;

namespace Avalonia.Controls.DataGridTests.Banding;

public sealed class ColumnBandGroupedHeaderTests
{
    [Fact]
    public void HeaderLayout_Refreshes_Materialized_Definition_Headers()
    {
        using var model = new ColumnBandModel();
        model.Bands.Add(new ColumnBand
        {
            Header = "Order",
            Children =
            {
                CreateLeaf("Date")
            }
        });

        var stacked = Assert.IsType<ColumnBandHeader>(Assert.Single(model.ColumnDefinitions).Header);
        Assert.Equal(ColumnBandHeaderLayout.Stacked, stacked.Layout);
        Assert.Equal(2, stacked.DisplaySegments.Count);

        model.HeaderLayout = ColumnBandHeaderLayout.Grouped;

        var grouped = Assert.IsType<ColumnBandHeader>(Assert.Single(model.ColumnDefinitions).Header);
        Assert.Equal(ColumnBandHeaderLayout.Grouped, grouped.Layout);
        Assert.Single(grouped.DisplaySegments);
        Assert.Equal("Date", grouped.DisplaySegments[0]);
    }

    [AvaloniaFact]
    public void Grouped_Headers_Span_Contiguous_Leaves_And_Remaining_Rows()
    {
        using var model = CreateGroupedModel();
        var grid = new DataGrid
        {
            AutoGenerateColumns = false,
            CanUserAddRows = false,
            ColumnHeaderHeight = 120,
            ItemsSource = new ObservableCollection<RowItem> { new() },
            HeadersVisibility = DataGridHeadersVisibility.Column
        };
        var window = new Window
        {
            Width = 540,
            Height = 260,
            Background = Brushes.White
        };
        window.SetThemeStyles();
        window.Content = grid;
        grid.ColumnDefinitionsSource = model.ColumnDefinitions;

        try
        {
            window.Show();
            Dispatcher.UIThread.RunJobs();
            window.UpdateLayout();
            grid.ApplyTemplate();
            grid.UpdateLayout();

            var presenter = Assert.Single(grid.GetVisualDescendants().OfType<DataGridColumnHeadersPresenter>());
            var bandCells = presenter.Children.OfType<DataGridColumnBandHeaderCell>().ToArray();
            Assert.Equal(4, bandCells.Length);

            var order = Assert.Single(bandCells, cell => Equals(cell.Content, "Order"));
            var financials = Assert.Single(bandCells, cell => Equals(cell.Content, "Financials"));
            var revenue = Assert.Single(bandCells, cell => Equals(cell.Content, "Revenue"));
            var volume = Assert.Single(bandCells, cell => Equals(cell.Content, "Volume"));

            var date = GetLeafHeader(grid, "Date");
            var region = GetLeafHeader(grid, "Region");
            var sales = GetLeafHeader(grid, "Sales");
            var profit = GetLeafHeader(grid, "Profit");
            var units = GetLeafHeader(grid, "Units");

            AssertClose(date.Bounds.Width + region.Bounds.Width, order.Bounds.Width);
            AssertClose(sales.Bounds.Width + profit.Bounds.Width + units.Bounds.Width, financials.Bounds.Width);
            AssertClose(sales.Bounds.Width + profit.Bounds.Width, revenue.Bounds.Width);
            AssertClose(units.Bounds.Width, volume.Bounds.Width);

            AssertClose(0, order.Bounds.Y);
            AssertClose(0, financials.Bounds.Y);
            AssertClose(40, revenue.Bounds.Y);
            AssertClose(40, volume.Bounds.Y);
            AssertClose(40, date.Bounds.Y);
            AssertClose(80, date.Bounds.Height);
            AssertClose(80, sales.Bounds.Y);
            AssertClose(40, sales.Bounds.Height);

            SaveScreenshotWhenRequested(window, "issue-294-grouped-column-bands.png");
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void Grouped_Header_Splits_At_Frozen_Boundary()
    {
        using var model = new ColumnBandModel
        {
            HeaderLayout = ColumnBandHeaderLayout.Grouped
        };
        using (model.DeferRefresh())
        {
            model.Bands.Add(new ColumnBand
            {
                Header = "Order",
                Children =
                {
                    CreateLeaf("Date"),
                    CreateLeaf("Region")
                }
            });
        }

        var grid = new DataGrid
        {
            AutoGenerateColumns = false,
            FrozenColumnCount = 1,
            HeadersVisibility = DataGridHeadersVisibility.Column
        };
        var window = new Window
        {
            Width = 240,
            Height = 160,
            Background = Brushes.White
        };
        window.SetThemeStyles();
        window.Content = grid;
        grid.ColumnDefinitionsSource = model.ColumnDefinitions;

        try
        {
            window.Show();
            Dispatcher.UIThread.RunJobs();
            window.UpdateLayout();
            grid.ApplyTemplate();
            grid.UpdateLayout();

            var presenter = Assert.Single(grid.GetVisualDescendants().OfType<DataGridColumnHeadersPresenter>());
            var orderCells = presenter.Children
                .OfType<DataGridColumnBandHeaderCell>()
                .Where(cell => Equals(cell.Content, "Order"))
                .ToArray();

            Assert.Equal(2, orderCells.Length);
            Assert.Contains(orderCells, cell => cell.IsFrozenLeft);
            Assert.Contains(orderCells, cell => !cell.IsFrozenLeft && !cell.IsFrozenRight);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void Grouped_Header_Does_Not_Merge_Distinct_Bands_With_The_Same_Caption()
    {
        using var model = new ColumnBandModel
        {
            HeaderLayout = ColumnBandHeaderLayout.Grouped
        };
        using (model.DeferRefresh())
        {
            model.Bands.Add(new ColumnBand
            {
                Header = "Repeated",
                Children = { CreateLeaf("First") }
            });
            model.Bands.Add(new ColumnBand
            {
                Header = "Repeated",
                Children = { CreateLeaf("Second") }
            });
        }

        var grid = new DataGrid
        {
            AutoGenerateColumns = false,
            ColumnHeaderHeight = 80,
            HeadersVisibility = DataGridHeadersVisibility.Column,
            ColumnDefinitionsSource = model.ColumnDefinitions
        };
        var window = new Window
        {
            Width = 240,
            Height = 160,
            Background = Brushes.White
        };
        window.SetThemeStyles();
        window.Content = grid;

        try
        {
            window.Show();
            Dispatcher.UIThread.RunJobs();
            window.UpdateLayout();
            grid.ApplyTemplate();
            grid.UpdateLayout();

            var presenter = Assert.Single(grid.GetVisualDescendants().OfType<DataGridColumnHeadersPresenter>());
            var repeatedHeaders = presenter.Children
                .OfType<DataGridColumnBandHeaderCell>()
                .Where(cell => Equals(cell.Content, "Repeated"))
                .ToArray();

            Assert.Equal(2, repeatedHeaders.Length);
            Assert.All(repeatedHeaders, cell => AssertClose(100, cell.Bounds.Width));
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void Grouped_Header_Grows_Auto_Sized_Leaf_Columns_To_Fit_Its_Caption()
    {
        const string Caption = "A parent caption that is substantially wider than its leaf headers";
        using var model = new ColumnBandModel
        {
            HeaderLayout = ColumnBandHeaderLayout.Grouped
        };
        using (model.DeferRefresh())
        {
            model.Bands.Add(new ColumnBand
            {
                Header = Caption,
                Children =
                {
                    CreateAutoSizedLeaf("A"),
                    CreateAutoSizedLeaf("B")
                }
            });
        }

        var grid = new DataGrid
        {
            AutoGenerateColumns = false,
            ColumnHeaderHeight = 80,
            HeadersVisibility = DataGridHeadersVisibility.Column,
            ItemsSource = new ObservableCollection<RowItem> { new() },
            ColumnDefinitionsSource = model.ColumnDefinitions
        };
        var window = new Window
        {
            Width = 700,
            Height = 160,
            Background = Brushes.White
        };
        window.SetThemeStyles();
        window.Content = grid;

        try
        {
            window.Show();
            Dispatcher.UIThread.RunJobs();
            window.UpdateLayout();
            grid.ApplyTemplate();
            grid.UpdateLayout();

            Assert.Equal(2, grid.Columns.Count);
            Assert.All(grid.Columns, column => Assert.IsType<ColumnBandHeader>(column.Header));
            var presenter = Assert.Single(grid.GetVisualDescendants().OfType<DataGridColumnHeadersPresenter>());
            var bandHeader = Assert.Single(
                presenter.Children.OfType<DataGridColumnBandHeaderCell>(),
                cell => Equals(cell.Content, Caption));

            Assert.True(bandHeader.Bounds.Width > 300);
            AssertClose(
                grid.ColumnsInternal.ItemsInternal
                    .Where(column => column is not DataGridFillerColumn)
                    .Sum(column => column.ActualWidth),
                bandHeader.Bounds.Width);
        }
        finally
        {
            window.Close();
        }
    }

    private static ColumnBandModel CreateGroupedModel()
    {
        var model = new ColumnBandModel
        {
            HeaderLayout = ColumnBandHeaderLayout.Grouped
        };

        using (model.DeferRefresh())
        {
            model.Bands.Add(new ColumnBand
            {
                Header = "Order",
                Children =
                {
                    CreateLeaf("Date"),
                    CreateLeaf("Region")
                }
            });
            model.Bands.Add(new ColumnBand
            {
                Header = "Financials",
                Children =
                {
                    new ColumnBand
                    {
                        Header = "Revenue",
                        Children =
                        {
                            CreateLeaf("Sales"),
                            CreateLeaf("Profit")
                        }
                    },
                    new ColumnBand
                    {
                        Header = "Volume",
                        Children =
                        {
                            CreateLeaf("Units")
                        }
                    }
                }
            });
        }

        return model;
    }

    private static ColumnBand CreateLeaf(string header)
    {
        return new ColumnBand
        {
            Header = header,
            ColumnDefinition = new DataGridTextColumnDefinition
            {
                Header = header,
                Width = new DataGridLength(100)
            }
        };
    }

    private static ColumnBand CreateAutoSizedLeaf(string header)
    {
        return new ColumnBand
        {
            Header = header,
            ColumnDefinition = new DataGridTextColumnDefinition
            {
                Header = header,
                Width = DataGridLength.Auto
            }
        };
    }

    private static DataGridColumnHeader GetLeafHeader(DataGrid grid, string text)
    {
        return grid.ColumnsInternal.ItemsInternal
            .Where(column => column is not DataGridFillerColumn)
            .Single(column =>
                column.Header is ColumnBandHeader header &&
                Equals(header.Segments[header.Segments.Count - 1], text))
            .HeaderCell;
    }

    private static void AssertClose(double expected, double actual)
    {
        Assert.InRange(actual, expected - 0.5, expected + 0.5);
    }

    private static void SaveScreenshotWhenRequested(Window window, string fileName)
    {
        string? directory = Environment.GetEnvironmentVariable("AVALONIA_SCREENSHOT_DIR");
        if (string.IsNullOrWhiteSpace(directory))
        {
            return;
        }

        var frame = window.CaptureRenderedFrame();
        Assert.NotNull(frame);
        Directory.CreateDirectory(directory);
        frame!.Save(Path.Combine(directory, fileName));
    }

    private sealed class RowItem
    {
    }
}
