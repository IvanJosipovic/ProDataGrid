// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.DataGridConditionalFormatting;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Xunit;

namespace Avalonia.Controls.DataGridTests.Columns;

public sealed class DataGridDrawnVisualParityTests
{
    public static IEnumerable<object[]> ThemeAndScalingCases()
    {
        yield return new object[] { DataGridTheme.FluentV2, 1.0 };
        yield return new object[] { DataGridTheme.FluentV2, 1.5 };
        yield return new object[] { DataGridTheme.SimpleV2, 1.0 };
        yield return new object[] { DataGridTheme.SimpleV2, 1.5 };
    }

    [AvaloniaTheory]
    [MemberData(nameof(ThemeAndScalingCases))]
    public void Retained_And_Drawn_Cells_Preserve_Visual_State(
        DataGridTheme theme,
        double renderScaling)
    {
        var items = new[]
        {
            new VisualItem("Alpha", 12),
            new VisualItem("Selected and invalid", 42),
            new VisualItem("Gamma", -3),
            new VisualItem("Delta", 8)
        };
        var retainedText = CreateTextColumn("Retained text", drawn: false, 180);
        var drawnText = CreateTextColumn("Drawn text", drawn: true, 180);
        var retainedNumber = CreateNumericColumn("Retained number", drawn: false, 120);
        var drawnNumber = CreateNumericColumn("Drawn number", drawn: true, 120);
        var grid = new DataGrid
        {
            Width = 720,
            Height = 210,
            RowHeight = 32,
            ItemsSource = items,
            AutoGenerateColumns = false,
            HeadersVisibility = DataGridHeadersVisibility.Column,
            GridLinesVisibility = DataGridGridLinesVisibility.All,
            SelectionMode = DataGridSelectionMode.Extended,
            SelectionUnit = DataGridSelectionUnit.FullRow,
            FrozenColumnCount = 1,
            FrozenColumnCountRight = 1,
            UseLogicalScrollable = true,
            UseLightweightFiller = true
        };
        grid.ColumnsInternal.Add(retainedText);
        grid.ColumnsInternal.Add(drawnText);
        grid.ColumnsInternal.Add(retainedNumber);
        grid.ColumnsInternal.Add(drawnNumber);

        var window = new Window
        {
            Width = 760,
            Height = 240,
            Background = Brushes.White
        };
        using IDisposable themeScope = UseApplicationTheme(theme);
        window.SetThemeStyles(theme);
        window.SetRenderScaling(renderScaling);
        window.Content = grid;

        try
        {
            ApplyOptimizedThemes(grid);
            ApplyConditionalFormatting(grid, retainedNumber, drawnNumber);
            window.Show();
            window.ApplyTemplate();
            grid.ApplyTemplate();
            Dispatcher.UIThread.RunJobs();
            window.UpdateLayout();
            grid.UpdateLayout();
            Dispatcher.UIThread.RunJobs();
            window.UpdateLayout();
            grid.UpdateLayout();
            int selectedSlot = grid.SlotFromRowIndex(1);
            Assert.True(grid.UpdateSelectionAndCurrency(
                columnIndex: 0,
                slot: selectedSlot,
                action: DataGridSelectionAction.SelectCurrent,
                scrollIntoView: false));

            DataGridRow selectedRow = Assert.IsType<DataGridRow>(
                grid.DisplayData.GetDisplayedRow(rowIndex: 1));
            DataGridCell retainedTextCell = selectedRow.Cells[0];
            DataGridCell drawnTextCell = selectedRow.Cells[1];
            DataGridCell retainedNumberCell = selectedRow.Cells[2];
            DataGridCell drawnNumberCell = selectedRow.Cells[3];

            SetValidationError(retainedTextCell);
            SetValidationError(drawnTextCell);
            Dispatcher.UIThread.RunJobs();
            window.UpdateLayout();
            grid.UpdateLayout();

            Assert.IsNotType<DataGridCustomDrawingCell>(retainedTextCell);
            Assert.IsType<DataGridCustomDrawingCell>(drawnTextCell);
            Assert.IsNotType<DataGridCustomDrawingCell>(retainedNumberCell);
            Assert.IsType<DataGridCustomDrawingCell>(drawnNumberCell);
            Assert.True(retainedTextCell.OwningColumn.IsFrozenLeft);
            Assert.False(drawnTextCell.OwningColumn.IsFrozenLeft);
            Assert.False(retainedNumberCell.OwningColumn.IsFrozenRight);
            Assert.True(drawnNumberCell.OwningColumn.IsFrozenRight);
            for (int columnIndex = 0; columnIndex < 4; columnIndex++)
            {
                Assert.True(grid.GetCellSelectionFromSlot(selectedSlot, columnIndex));
            }

            Assert.Equal(retainedText.ActualWidth, drawnText.ActualWidth);
            Assert.Equal(retainedNumber.ActualWidth, drawnNumber.ActualWidth);
            Assert.Equal(retainedTextCell.Bounds.Height, drawnTextCell.Bounds.Height);
            Assert.Equal(retainedNumberCell.Bounds.Height, drawnNumberCell.Bounds.Height);
            Assert.Equal(retainedTextCell.ValidationSeverity, drawnTextCell.ValidationSeverity);
            Assert.Equal(retainedTextCell.BorderBrush, drawnTextCell.BorderBrush);
            Assert.Equal(retainedTextCell.BorderThickness, drawnTextCell.BorderThickness);
            Assert.False(retainedTextCell.IsValid);
            Assert.False(drawnTextCell.IsValid);
            Assert.True(DataValidationErrors.GetHasErrors(retainedTextCell));
            Assert.True(DataValidationErrors.GetHasErrors(drawnTextCell));
            Assert.Equal(retainedNumberCell.Background, drawnNumberCell.Background);
            Assert.Equal(Brushes.Moccasin, retainedNumberCell.Background);
            Assert.Equal(renderScaling, window.RenderScaling);

            SaveScreenshotWhenRequested(
                window,
                $"drawn-parity-{theme.ToString().ToLowerInvariant()}-{renderScaling.ToString("0.0", CultureInfo.InvariantCulture)}.png");
        }
        finally
        {
            window.Close();
        }
    }

    private static DataGridTextColumn CreateTextColumn(
        object header,
        bool drawn,
        double width)
    {
        var column = new DataGridTextColumn
        {
            Header = header,
            Binding = new Binding(nameof(VisualItem.Text)),
            DisplayMode = drawn
                ? DataGridColumnDisplayMode.Drawn
                : DataGridColumnDisplayMode.Retained,
            Width = new DataGridLength(width)
        };
        DataGridColumnMetadata.SetValueAccessor(
            column,
            new DataGridColumnValueAccessor<VisualItem, string>(item => item.Text));
        return column;
    }

    private static DataGridNumericColumn CreateNumericColumn(
        object header,
        bool drawn,
        double width)
    {
        var column = new DataGridNumericColumn
        {
            Header = header,
            Binding = new Binding(nameof(VisualItem.Number)),
            DisplayMode = drawn
                ? DataGridColumnDisplayMode.Drawn
                : DataGridColumnDisplayMode.Retained,
            Width = new DataGridLength(width)
        };
        DataGridColumnMetadata.SetValueAccessor(
            column,
            new DataGridColumnValueAccessor<VisualItem, decimal>(item => item.Number));
        return column;
    }

    private static void ApplyOptimizedThemes(DataGrid grid)
    {
        grid.RowTheme = FindTheme(grid, "DataGridOptimizedRowTheme");
        grid.CellTheme = FindTheme(grid, "DataGridOptimizedCellTheme");
        grid.ColumnHeaderTheme = FindTheme(grid, "DataGridOptimizedColumnHeaderTheme");
    }

    private static void ApplyConditionalFormatting(
        DataGrid grid,
        DataGridColumn retainedColumn,
        DataGridColumn drawnColumn)
    {
        var retainedTheme = CreateConditionalTheme(
            typeof(DataGridCell),
            grid.CellTheme!);
        var drawnTheme = CreateConditionalTheme(
            typeof(DataGridCustomDrawingCell),
            FindTheme(grid, "DataGridOptimizedDrawingCellTheme"));
        var model = new ConditionalFormattingModel();
        model.Apply(new[]
        {
            new ConditionalFormattingDescriptor(
                ruleId: "retained-positive",
                @operator: ConditionalFormattingOperator.GreaterThan,
                columnId: retainedColumn,
                value: 0m,
                theme: retainedTheme),
            new ConditionalFormattingDescriptor(
                ruleId: "drawn-positive",
                @operator: ConditionalFormattingOperator.GreaterThan,
                columnId: drawnColumn,
                value: 0m,
                theme: drawnTheme)
        });
        grid.ConditionalFormattingModel = model;
    }

    private static ControlTheme CreateConditionalTheme(
        Type targetType,
        ControlTheme basedOn)
    {
        var theme = new ControlTheme(targetType)
        {
            BasedOn = basedOn
        };
        theme.Setters.Add(new Setter(TemplatedControl.BackgroundProperty, Brushes.Moccasin));
        return theme;
    }

    private static ControlTheme FindTheme(DataGrid grid, string resourceKey)
    {
        if ((grid.TryFindResource(resourceKey, out object? resource) ||
             Application.Current?.TryFindResource(resourceKey, out resource) == true) &&
            resource is ControlTheme theme)
        {
            return theme;
        }

        throw new InvalidOperationException($"Missing optimized theme '{resourceKey}'.");
    }

    private static IDisposable UseApplicationTheme(DataGridTheme theme)
    {
        Styles styles = ThemeHelper.GetThemeStyles(theme);
        Styles? applicationStyles = Application.Current?.Styles;
        applicationStyles?.Add(styles);
        return new ThemeScope(applicationStyles, styles);
    }

    private static void SetValidationError(DataGridCell cell)
    {
        cell.IsValid = false;
        cell.ValidationSeverity = DataGridValidationSeverity.Error;
        cell.UpdatePseudoClasses();
        DataValidationErrors.SetError(
            cell,
            new InvalidOperationException("Visual parity validation error."));
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
        string path = Path.GetFullPath(Path.Combine(directory, fileName));
        frame!.Save(path);
        Assert.True(new FileInfo(path).Length > 0);
    }

    private sealed record VisualItem(string Text, decimal Number);

    private sealed class ThemeScope : IDisposable
    {
        private readonly Styles? _applicationStyles;
        private readonly Styles _styles;

        public ThemeScope(Styles? applicationStyles, Styles styles)
        {
            _applicationStyles = applicationStyles;
            _styles = styles;
        }

        public void Dispose() => _applicationStyles?.Remove(_styles);
    }
}
