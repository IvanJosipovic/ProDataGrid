using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Xunit;

namespace DataGridSample.Tests;

public sealed class LargeVariableHeightScrollingTests
{
    [AvaloniaFact]
    public void LargeVariableHeight_SmallScrollsRemainStableAfterDistantJump()
    {
        var page = new global::DataGridSample.LargeVariableHeightPage();
        var window = new Window
        {
            Width = 1280,
            Height = 720,
            Content = page
        };
        window.ApplySampleTheme();

        try
        {
            window.Show();
            PumpLayout(window);

            var grid = page.GetVisualDescendants().OfType<DataGrid>().Single();
            grid.CanUserAddRows = false;
            var scrollViewer = grid.GetVisualDescendants().OfType<ScrollViewer>().Single();
            var presenter = grid.GetVisualDescendants().OfType<DataGridRowsPresenter>().Single();
            var maximumOffset = Math.Max(0, scrollViewer.Extent.Height - scrollViewer.Viewport.Height);
            Assert.True(maximumOffset > 0, $"Expected a scrollable large sample, got maximum offset {maximumOffset}.");

            scrollViewer.Offset = new Vector(0, maximumOffset * 0.5);
            PumpLayout(window);

            var offsets = new List<double> { presenter.Offset.Y };
            var firstVisibleRows = new List<int> { GetFirstVisibleRowIndex(grid) };
            var lastVisibleRows = new List<int> { GetLastVisibleRowIndex(grid) };
            var firstVisibleTops = new List<double> { GetFirstVisibleRow(grid).Bounds.Top };
            var previousRowTops = GetViewportRowTops(grid);
            var commonRowTopDeltas = new List<double>();
            for (var i = 0; i < 64; i++)
            {
                scrollViewer.Offset = new Vector(0, scrollViewer.Offset.Y + 30);
                PumpLayout(window);
                offsets.Add(presenter.Offset.Y);
                firstVisibleRows.Add(GetFirstVisibleRowIndex(grid));
                lastVisibleRows.Add(GetLastVisibleRowIndex(grid));
                firstVisibleTops.Add(GetFirstVisibleRow(grid).Bounds.Top);

                var currentRowTops = GetViewportRowTops(grid);
                foreach (var previousRow in previousRowTops)
                {
                    if (currentRowTops.TryGetValue(previousRow.Key, out var currentTop))
                    {
                        commonRowTopDeltas.Add(currentTop - previousRow.Value);
                    }
                }
                previousRowTops = currentRowTops;
            }

            var deltas = offsets.Zip(offsets.Skip(1), (previous, current) => current - previous).ToList();
            var firstVisibleRegressions = firstVisibleRows.Zip(firstVisibleRows.Skip(1), (previous, current) => current - previous)
                .Where(delta => delta < 0)
                .ToList();
            var backwardJumps = deltas.Where(delta => delta < -1).ToList();
            var visibleRegressions = lastVisibleRows.Zip(lastVisibleRows.Skip(1), (previous, current) => current - previous)
                .Where(delta => delta < 0)
                .ToList();
            var rowTopDeltaRegressions = commonRowTopDeltas
                .Where(delta => Math.Abs(delta + 30) > 1)
                .ToList();

            Assert.True(
                backwardJumps.Count == 0,
                $"Offset regressed after a small jump. Deltas: {string.Join(", ", deltas.Select(delta => delta.ToString("F1")))}; Offsets: {string.Join(", ", offsets.Select(offset => offset.ToString("F1")))}");
            Assert.NotEmpty(commonRowTopDeltas);
            Assert.True(
                rowTopDeltaRegressions.Count == 0,
                $"A realized row moved by more than the requested small jump. Common row top deltas: {string.Join(", ", commonRowTopDeltas.Select(delta => delta.ToString("F1")))}; Offsets: {string.Join(", ", offsets.Select(offset => offset.ToString("F1")))}");
            Assert.True(
                firstVisibleRegressions.Count == 0,
                $"First visible row regressed after a small jump. First visible rows: {string.Join(", ", firstVisibleRows)}; First visible tops: {string.Join(", ", firstVisibleTops.Select(top => top.ToString("F1")))}; Last visible rows: {string.Join(", ", lastVisibleRows)}; Offsets: {string.Join(", ", offsets.Select(offset => offset.ToString("F1")))}");
            Assert.True(
                visibleRegressions.Count == 0,
                $"Visible range regressed after a small jump. First visible rows: {string.Join(", ", firstVisibleRows)}; First visible tops: {string.Join(", ", firstVisibleTops.Select(top => top.ToString("F1")))}; Last visible rows: {string.Join(", ", lastVisibleRows)}; Offsets: {string.Join(", ", offsets.Select(offset => offset.ToString("F1")))}");
            Assert.Contains(deltas, delta => delta > 1);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void LargeVariableHeight_SmallScrollsUpRemainStableAfterDistantJump()
    {
        var page = new global::DataGridSample.LargeVariableHeightPage();
        var window = new Window
        {
            Width = 1280,
            Height = 720,
            Content = page
        };
        window.ApplySampleTheme();

        try
        {
            window.Show();
            PumpLayout(window);

            var grid = page.GetVisualDescendants().OfType<DataGrid>().Single();
            grid.CanUserAddRows = false;
            var scrollViewer = grid.GetVisualDescendants().OfType<ScrollViewer>().Single();
            var presenter = grid.GetVisualDescendants().OfType<DataGridRowsPresenter>().Single();
            var maximumOffset = Math.Max(0, scrollViewer.Extent.Height - scrollViewer.Viewport.Height);
            Assert.True(maximumOffset > 0, $"Expected a scrollable large sample, got maximum offset {maximumOffset}.");

            scrollViewer.Offset = new Vector(0, maximumOffset * 0.5);
            PumpLayout(window);

            var offsets = new List<double> { presenter.Offset.Y };
            var firstVisibleRows = new List<int> { GetFirstVisibleRowIndex(grid) };
            var lastVisibleRows = new List<int> { GetLastVisibleRowIndex(grid) };
            var firstVisibleTops = new List<double> { GetFirstVisibleRow(grid).Bounds.Top };
            var previousRowTops = GetViewportRowTops(grid);
            var commonRowTopDeltas = new List<double>();
            for (var i = 0; i < 64; i++)
            {
                scrollViewer.Offset = new Vector(0, scrollViewer.Offset.Y - 30);
                PumpLayout(window);
                offsets.Add(presenter.Offset.Y);
                firstVisibleRows.Add(GetFirstVisibleRowIndex(grid));
                lastVisibleRows.Add(GetLastVisibleRowIndex(grid));
                firstVisibleTops.Add(GetFirstVisibleRow(grid).Bounds.Top);

                var currentRowTops = GetViewportRowTops(grid);
                foreach (var previousRow in previousRowTops)
                {
                    if (currentRowTops.TryGetValue(previousRow.Key, out var currentTop))
                    {
                        commonRowTopDeltas.Add(currentTop - previousRow.Value);
                    }
                }
                previousRowTops = currentRowTops;
            }

            var offsetDeltas = offsets.Zip(offsets.Skip(1), (previous, current) => current - previous).ToList();
            var offsetDeltaRegressions = offsetDeltas
                .Where(delta => Math.Abs(delta + 30) > 1)
                .ToList();
            var firstVisibleRegressions = firstVisibleRows.Zip(firstVisibleRows.Skip(1), (previous, current) => current - previous)
                .Where(delta => delta > 0)
                .ToList();
            var lastVisibleRegressions = lastVisibleRows.Zip(lastVisibleRows.Skip(1), (previous, current) => current - previous)
                .Where(delta => delta > 0)
                .ToList();
            var rowTopDeltaRegressions = commonRowTopDeltas
                .Where(delta => Math.Abs(delta - 30) > 1)
                .ToList();

            Assert.NotEmpty(commonRowTopDeltas);
            Assert.True(
                offsetDeltaRegressions.Count == 0,
                $"Offset did not move up by 30 pixels. Deltas: {string.Join(", ", offsetDeltas.Select(delta => delta.ToString("F1")))}");
            Assert.True(
                firstVisibleRegressions.Count == 0,
                $"First visible row moved down during upward scrolling. Rows: {string.Join(", ", firstVisibleRows)}; Tops: {string.Join(", ", firstVisibleTops.Select(top => top.ToString("F1")))}; Common deltas: {string.Join(", ", commonRowTopDeltas.Take(20).Select(delta => delta.ToString("F1")))}; Offsets: {string.Join(", ", offsets.Take(10).Select(offset => offset.ToString("F1")))}");
            Assert.True(
                lastVisibleRegressions.Count == 0,
                $"Last visible row moved down during upward scrolling. Rows: {string.Join(", ", lastVisibleRows)}; Offsets: {string.Join(", ", offsets.Select(offset => offset.ToString("F1")))}");
            Assert.True(
                rowTopDeltaRegressions.Count == 0,
                $"A realized row moved by more than the requested upward jump. Common row top deltas: {string.Join(", ", commonRowTopDeltas.Select(delta => delta.ToString("F1")))}; Offsets: {string.Join(", ", offsets.Select(offset => offset.ToString("F1")))}");
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void LargeVariableHeight_AlternatingSmallWheelsReturnToSameOffset()
    {
        var page = new global::DataGridSample.LargeVariableHeightPage();
        var window = new Window
        {
            Width = 1280,
            Height = 720,
            Content = page
        };
        window.ApplySampleTheme();

        try
        {
            window.Show();
            PumpLayout(window);

            var grid = page.GetVisualDescendants().OfType<DataGrid>().Single();
            grid.CanUserAddRows = false;
            var scrollViewer = grid.GetVisualDescendants().OfType<ScrollViewer>().Single();
            var presenter = grid.GetVisualDescendants().OfType<DataGridRowsPresenter>().Single();
            var maximumOffset = Math.Max(0, scrollViewer.Extent.Height - scrollViewer.Viewport.Height);
            scrollViewer.Offset = new Vector(0, maximumOffset * 0.5);
            PumpLayout(window);

            var wheelPoint = page.TranslatePoint(
                new Point(page.Bounds.Width / 2, page.Bounds.Height / 2),
                window)!.Value;
            var topLevel = (TopLevel)window;
            var baselineOffset = presenter.Offset.Y;
            for (var i = 0; i < 32; i++)
            {
                topLevel.MouseWheel(wheelPoint, new Vector(0, -3));
                PumpLayout(window);
                topLevel.MouseWheel(wheelPoint, new Vector(0, 3));
                PumpLayout(window);
            }

            Assert.InRange(Math.Abs(presenter.Offset.Y - baselineOffset), 0, 1);
        }
        finally
        {
            window.Close();
        }
    }

    private static int GetLastVisibleRowIndex(DataGrid grid)
    {
        return GetViewportRows(grid)
            .Max(row => row.Index);
    }

    private static int GetFirstVisibleRowIndex(DataGrid grid)
    {
        return GetViewportRows(grid)
            .Min(row => row.Index);
    }

    private static DataGridRow GetFirstVisibleRow(DataGrid grid)
    {
        return GetViewportRows(grid)
            .OrderBy(row => row.Index)
            .First();
    }

    private static IReadOnlyList<DataGridRow> GetViewportRows(DataGrid grid)
    {
        var presenter = grid.GetVisualDescendants().OfType<DataGridRowsPresenter>().Single();
        var viewportHeight = presenter.Bounds.Height;
        return grid.GetVisualDescendants()
            .OfType<DataGridRow>()
            .Where(row =>
            {
                var position = row.TranslatePoint(new Point(0, 0), presenter);
                return row.IsVisible &&
                       position.HasValue &&
                       position.Value.Y + row.Bounds.Height > 0 &&
                       position.Value.Y < viewportHeight;
            })
            .ToList();
    }

    private static Dictionary<int, double> GetViewportRowTops(DataGrid grid)
    {
        var presenter = grid.GetVisualDescendants().OfType<DataGridRowsPresenter>().Single();
        return GetViewportRows(grid)
            .ToDictionary(
                row => row.Index,
                row => row.TranslatePoint(new Point(0, 0), presenter)!.Value.Y);
    }

    private static void PumpLayout(Control control)
    {
        Dispatcher.UIThread.RunJobs();
        control.UpdateLayout();
        Dispatcher.UIThread.RunJobs();
    }
}
