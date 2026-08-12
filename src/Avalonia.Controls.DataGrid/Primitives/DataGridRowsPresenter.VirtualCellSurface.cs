// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

#nullable enable

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Threading;
using Avalonia.Controls.DataGridHierarchical;
using Avalonia.Controls.Documents;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace Avalonia.Controls.Primitives;

sealed partial class DataGridRowsPresenter
{
    private const double VirtualCellHorizontalPadding = 12d;
    private const double VirtualExpanderSize = 16d;
    // Keep several viewports of shaped text so discontinuous scrolling does not
    // evict the entire working set between render passes.
    private const int VirtualTextLayoutCacheCapacity = 4096;

    private DataGridVirtualCellSurface? _virtualCellSurface;
    private readonly DataGridVirtualTextLayoutCache _virtualTextLayoutCache =
        new(VirtualTextLayoutCacheCapacity);
    private HashSet<INotifyPropertyChanged> _virtualValueNotifiers =
        new(ReferenceEqualityComparer.Instance);
    private HashSet<INotifyPropertyChanged> _nextVirtualValueNotifiers =
        new(ReferenceEqualityComparer.Instance);
    private int _virtualValueChangeCount;

    internal int VirtualSurfaceCount => _virtualCellSurface?.GetVisualParent() == this ? 1 : 0;

    internal int VirtualTrackedValueNotifierCount => _virtualValueNotifiers.Count;

    internal int VirtualValueChangeCount => Volatile.Read(ref _virtualValueChangeCount);

    internal void InvalidateVirtualCellSurface() => _virtualCellSurface?.InvalidateVisual();

    private void SyncVirtualCellSurface()
    {
        if (OwningGrid?.UsesVirtualCellSurface != true)
        {
            DetachVirtualCellSurface();
            return;
        }

        _virtualCellSurface ??= new DataGridVirtualCellSurface
        {
            Owner = this,
            Focusable = true,
            ClipToBounds = true,
        };

        if (!ReferenceEquals(_virtualCellSurface.GetVisualParent(), this))
        {
            _virtualCellSurface.SetValue(Panel.ZIndexProperty, 0);
            VisualChildren.Add(_virtualCellSurface);
        }
    }

    private void DetachVirtualCellSurface()
    {
        if (_virtualCellSurface is null)
        {
            return;
        }

        if (ReferenceEquals(_virtualCellSurface.GetVisualParent(), this))
        {
            VisualChildren.Remove(_virtualCellSurface);
        }

        _virtualCellSurface.Owner = null;
        _virtualCellSurface = null;
        _virtualTextLayoutCache.Clear();
        ClearVirtualValueNotifiers();
    }

    private void MeasureVirtualCellSurface(Size availableSize)
    {
        if (_virtualCellSurface is null)
        {
            return;
        }

        double width = double.IsFinite(availableSize.Width) ? Math.Max(0d, availableSize.Width) : 0d;
        double height = double.IsFinite(availableSize.Height) ? Math.Max(0d, availableSize.Height) : 0d;
        _virtualCellSurface.Measure(new Size(width, height));
    }

    private void ArrangeVirtualCellSurface(double width, double height)
    {
        if (_virtualCellSurface is null)
        {
            return;
        }

        _virtualCellSurface.Arrange(new Rect(0d, 0d, Math.Max(0d, width), Math.Max(0d, height)));
        SyncVirtualValueNotifiers();
        _virtualCellSurface.InvalidateVisual();
    }

    internal void RenderVirtualCells(DrawingContext context)
    {
        DataGrid? grid = OwningGrid;
        if (grid?.UsesVirtualCellSurface != true)
        {
            return;
        }

        ResolveVirtualTextStyle(out FontFamily fontFamily, out double fontSize, out IBrush foreground);
        IBrush? selectedBackground = FindVirtualResource<IBrush>(
            grid.IsKeyboardFocusWithin
                ? "DataGridCellSelectedBackgroundBrush"
                : "DataGridCellSelectedUnfocusedBackgroundBrush");
        IBrush? currentBorder = FindVirtualResource<IBrush>("DataGridCurrencyVisualPrimaryBrush");
        Pen? currentPen = currentBorder is null ? null : new Pen(currentBorder, 1d);
        Pen? verticalGridPen = grid.VerticalGridLinesBrush is null ? null : new Pen(grid.VerticalGridLinesBrush, 1d);
        Pen expanderPen = new(foreground, 1.5d, lineCap: PenLineCap.Round, lineJoin: PenLineJoin.Round);
        bool drawVerticalGridLines = grid.GridLinesVisibility is DataGridGridLinesVisibility.Vertical or DataGridGridLinesVisibility.All;

        foreach (DataGridRow row in grid.DisplayData.GetScrollingRows())
        {
            double rowHeight = row.GetFlatCellsHeight();
            if (rowHeight <= 0d || row.Bounds.Bottom < 0d || row.Bounds.Top > Bounds.Height)
            {
                continue;
            }

            for (int layoutIndex = 0; layoutIndex < _flatColumnLayouts.Count; layoutIndex++)
            {
                FlatColumnLayout layout = _flatColumnLayouts[layoutIndex];
                if (!layout.ShouldDisplay)
                {
                    continue;
                }

                DataGridColumn column = layout.Column;
                if (grid.IsVirtualCompatibilityCell(row, column))
                {
                    continue;
                }
                Rect cellBounds = GetVirtualCellBounds(grid, row, layout, rowHeight);
                Rect visibleBounds = GetVisibleVirtualCellBounds(grid, column, cellBounds);
                if (visibleBounds.Width <= 0d || visibleBounds.Height <= 0d)
                {
                    continue;
                }

                if (AreClose(cellBounds, visibleBounds))
                {
                    DrawVirtualCell(
                        context,
                        grid,
                        row,
                        column,
                        cellBounds,
                        fontFamily,
                        fontSize,
                        foreground,
                        expanderPen,
                        selectedBackground,
                        currentPen,
                        verticalGridPen,
                        drawVerticalGridLines);
                    continue;
                }

                using (context.PushClip(visibleBounds))
                {
                    DrawVirtualCell(
                        context,
                        grid,
                        row,
                        column,
                        cellBounds,
                        fontFamily,
                        fontSize,
                        foreground,
                        expanderPen,
                        selectedBackground,
                        currentPen,
                        verticalGridPen,
                        drawVerticalGridLines);
                }
            }
        }
    }

    private void DrawVirtualCell(
        DrawingContext context,
        DataGrid grid,
        DataGridRow row,
        DataGridColumn column,
        Rect cellBounds,
        FontFamily fontFamily,
        double fontSize,
        IBrush foreground,
        Pen expanderPen,
        IBrush? selectedBackground,
        Pen? currentPen,
        Pen? verticalGridPen,
        bool drawVerticalGridLines)
    {
        bool selected = grid.IsCellSelected(row.Index, column.Index);
        bool current = grid.CurrentSlot == row.Slot && grid.CurrentColumnIndex == column.Index;
        if (selected && selectedBackground is not null)
        {
            context.DrawRectangle(selectedBackground, null, cellBounds);
        }

        DrawVirtualCellContent(
            context,
            grid,
            row,
            column,
            cellBounds,
            fontFamily,
            fontSize,
            foreground,
            expanderPen);
        if (current && currentPen is not null)
        {
            context.DrawRectangle(null, currentPen, cellBounds.Deflate(0.5d));
        }

        if (drawVerticalGridLines && verticalGridPen is not null &&
            (!ReferenceEquals(column, grid.ColumnsInternal.LastVisibleColumn) || grid.ColumnsInternal.FillerColumn.IsActive))
        {
            double x = Math.Max(cellBounds.Left, cellBounds.Right - 0.5d);
            context.DrawLine(verticalGridPen, new Point(x, cellBounds.Top), new Point(x, cellBounds.Bottom));
        }
    }

    private void DrawVirtualCellContent(
        DrawingContext context,
        DataGrid grid,
        DataGridRow row,
        DataGridColumn column,
        Rect bounds,
        FontFamily fontFamily,
        double fontSize,
        IBrush foreground,
        Pen expanderPen)
    {
        object? item = row.DataContext;
        object? value = GetVirtualCellValue(column, item);

        if (column is DataGridProgressBarColumn progressColumn)
        {
            DrawVirtualProgress(context, progressColumn, value, bounds, foreground);
            return;
        }

        if (column is DataGridCheckBoxColumn checkBoxColumn)
        {
            DrawVirtualCheckBox(context, checkBoxColumn, value, bounds, expanderPen);
            return;
        }

        if (column is DataGridImageColumn && value is IImage image)
        {
            DrawVirtualImage(context, (DataGridImageColumn)column, image, bounds);
            return;
        }

        string text = value?.ToString() ?? string.Empty;
        double left = bounds.Left + VirtualCellHorizontalPadding;
        if (column is DataGridHierarchicalColumn hierarchicalColumn && item is HierarchicalNode node)
        {
            double indent = Math.Max(0, node.Level) * hierarchicalColumn.Indent;
            DrawVirtualExpander(context, node, bounds.Left + indent, bounds.Top, bounds.Height, expanderPen);
            left += indent + VirtualExpanderSize;
        }

        FontStyle fontStyle = FontStyle.Normal;
        FontWeight fontWeight = FontWeight.Normal;
        FontStretch fontStretch = FontStretch.Normal;
        TextAlignment textAlignment = column switch
        {
            DataGridNumericColumn => TextAlignment.Right,
            DataGridDatePickerColumn dateColumn => dateColumn.GetTextAlignment(),
            _ => TextAlignment.Left,
        };
        if (column is DataGridTextColumn textColumn)
        {
            fontFamily = textColumn.FontFamily ?? fontFamily;
            fontSize = double.IsFinite(textColumn.FontSize) && textColumn.FontSize > 0d
                ? textColumn.FontSize
                : fontSize;
            fontStyle = textColumn.FontStyle;
            fontWeight = textColumn.FontWeight;
            fontStretch = textColumn.FontStretch;
            foreground = textColumn.Foreground ?? foreground;
        }

        double maxWidth = Math.Max(0d, bounds.Right - left - VirtualCellHorizontalPadding);
        if (maxWidth <= 0d || string.IsNullOrEmpty(text))
        {
            return;
        }

        TextLayout textLayout = GetVirtualTextLayout(
            text,
            fontFamily,
            fontStyle,
            fontWeight,
            fontStretch,
            fontSize,
            foreground,
            textAlignment,
            maxWidth,
            bounds.Height);
        double top = bounds.Top + Math.Max(0d, (bounds.Height - textLayout.Height) * 0.5d);
        textLayout.Draw(context, new Point(left, top));
    }

    private object? GetVirtualCellValue(DataGridColumn column, object? item)
    {
        if (column is DataGridHierarchicalColumn hierarchicalColumn)
        {
            return hierarchicalColumn.GetDirectText(item);
        }

        if (item is not null && column is IDataGridDrawnCellValueProvider provider)
        {
            return provider.GetDrawnCellValue(item);
        }

        IDataGridColumnValueAccessor? accessor = DataGridColumnMetadata.GetValueAccessor(column);
        if (item is not null && accessor is not null && accessor.ItemType.IsInstanceOfType(item))
        {
            return accessor.GetValue(item);
        }

        return null;
    }

    private TextLayout GetVirtualTextLayout(
        string text,
        FontFamily fontFamily,
        FontStyle fontStyle,
        FontWeight fontWeight,
        FontStretch fontStretch,
        double fontSize,
        IBrush foreground,
        TextAlignment textAlignment,
        double maxWidth,
        double maxHeight)
    {
        GetBrushIdentity(foreground, out byte foregroundKind, out Color foregroundColor, out double foregroundOpacity, out int foregroundIdentity);
        var key = new DataGridCustomDrawingTextLayoutCache.CacheKey(
            text,
            fontFamily.Name,
            fontStyle,
            fontWeight,
            fontStretch,
            fontSize,
            textAlignment,
            TextTrimming.CharacterEllipsis,
            FlowDirection.LeftToRight,
            CultureInfo.CurrentCulture.LCID,
            maxWidth,
            maxHeight,
            foregroundKind,
            foregroundColor,
            foregroundOpacity,
            foregroundIdentity);

        if (_virtualTextLayoutCache.TryGet(key, out TextLayout? textLayout))
        {
            return textLayout;
        }

        textLayout = new TextLayout(
            text,
            new Typeface(fontFamily, fontStyle, fontWeight, fontStretch),
            fontSize,
            foreground,
            textAlignment,
            TextWrapping.NoWrap,
            TextTrimming.CharacterEllipsis,
            flowDirection: FlowDirection.LeftToRight,
            maxWidth: maxWidth,
            maxHeight: maxHeight,
            maxLines: 1);
        return _virtualTextLayoutCache.Add(key, textLayout);
    }

    private static void DrawVirtualProgress(
        DrawingContext context,
        DataGridProgressBarColumn column,
        object? value,
        Rect bounds,
        IBrush fallbackForeground)
    {
        double height = double.IsNaN(column.Height) ? 4d : Math.Max(0d, column.Height);
        Rect bar = new(
            bounds.Left + VirtualCellHorizontalPadding,
            bounds.Top + Math.Max(0d, (bounds.Height - height) * 0.5d),
            Math.Max(0d, bounds.Width - (VirtualCellHorizontalPadding * 2d)),
            Math.Min(height, bounds.Height));
        context.DrawRectangle(column.Background ?? Brushes.Transparent, null, bar);

        double numeric = column.Minimum;
        if (value is IConvertible convertible)
        {
            try
            {
                numeric = convertible.ToDouble(CultureInfo.InvariantCulture);
            }
            catch (Exception exception) when (exception is FormatException or InvalidCastException or OverflowException)
            {
            }
        }

        double range = column.Maximum - column.Minimum;
        double ratio = range > 0d ? Math.Clamp((numeric - column.Minimum) / range, 0d, 1d) : 0d;
        context.DrawRectangle(
            column.Foreground ?? fallbackForeground,
            null,
            new Rect(bar.X, bar.Y, bar.Width * ratio, bar.Height));
    }

    private static void DrawVirtualCheckBox(
        DrawingContext context,
        DataGridCheckBoxColumn column,
        object? value,
        Rect bounds,
        Pen pen)
    {
        const double indicatorSize = 14d;
        double size = Math.Min(indicatorSize, Math.Max(0d, Math.Min(bounds.Width, bounds.Height) - 4d));
        if (size <= 0d)
        {
            return;
        }

        Rect indicator = new(
            bounds.Left + ((bounds.Width - size) * 0.5d),
            bounds.Top + ((bounds.Height - size) * 0.5d),
            size,
            size);
        context.DrawRectangle(null, pen, indicator.Deflate(0.75d));

        if (value is true)
        {
            double left = indicator.Left + (size * 0.22d);
            double middle = indicator.Left + (size * 0.43d);
            double right = indicator.Left + (size * 0.80d);
            double center = indicator.Top + (size * 0.52d);
            context.DrawLine(pen, new Point(left, center), new Point(middle, indicator.Top + (size * 0.72d)));
            context.DrawLine(pen, new Point(middle, indicator.Top + (size * 0.72d)), new Point(right, indicator.Top + (size * 0.28d)));
        }
        else if (value is null && column.IsThreeState)
        {
            double inset = size * 0.25d;
            double center = indicator.Top + (size * 0.5d);
            context.DrawLine(
                pen,
                new Point(indicator.Left + inset, center),
                new Point(indicator.Right - inset, center));
        }
    }

    private static void DrawVirtualImage(
        DrawingContext context,
        DataGridImageColumn column,
        IImage image,
        Rect bounds)
    {
        Size source = image.Size;
        if (source.Width <= 0d || source.Height <= 0d)
        {
            return;
        }

        Rect content = bounds.Deflate(new Thickness(VirtualCellHorizontalPadding, 2d));
        double targetWidth = Math.Min(Math.Max(0d, column.ImageWidth), content.Width);
        double targetHeight = Math.Min(Math.Max(0d, column.ImageHeight), content.Height);
        double scaleX = targetWidth / source.Width;
        double scaleY = targetHeight / source.Height;
        double scale = column.Stretch switch
        {
            Stretch.None => 1d,
            Stretch.UniformToFill => Math.Max(scaleX, scaleY),
            _ => Math.Min(scaleX, scaleY),
        };
        double width;
        double height;
        if (column.Stretch == Stretch.Fill)
        {
            width = source.Width * ApplyVirtualStretchDirection(scaleX, column.StretchDirection);
            height = source.Height * ApplyVirtualStretchDirection(scaleY, column.StretchDirection);
        }
        else
        {
            scale = ApplyVirtualStretchDirection(scale, column.StretchDirection);
            width = source.Width * scale;
            height = source.Height * scale;
        }
        Rect target = new(
            content.X + Math.Max(0d, (content.Width - width) * 0.5d),
            content.Y + Math.Max(0d, (content.Height - height) * 0.5d),
            width,
            height);
        context.DrawImage(image, new Rect(source), target);
    }

    private static double ApplyVirtualStretchDirection(double scale, StretchDirection direction) => direction switch
    {
        StretchDirection.UpOnly => Math.Max(1d, scale),
        StretchDirection.DownOnly => Math.Min(1d, scale),
        _ => scale,
    };

    private static void DrawVirtualExpander(
        DrawingContext context,
        HierarchicalNode node,
        double left,
        double top,
        double height,
        Pen pen)
    {
        if (node.IsLeaf)
        {
            return;
        }

        double centerX = left + (VirtualExpanderSize * 0.5d);
        double centerY = top + (height * 0.5d);
        if (node.IsExpanded)
        {
            context.DrawLine(pen, new Point(centerX - 4d, centerY - 2d), new Point(centerX, centerY + 2d));
            context.DrawLine(pen, new Point(centerX, centerY + 2d), new Point(centerX + 4d, centerY - 2d));
        }
        else
        {
            context.DrawLine(pen, new Point(centerX - 2d, centerY - 4d), new Point(centerX + 2d, centerY));
            context.DrawLine(pen, new Point(centerX + 2d, centerY), new Point(centerX - 2d, centerY + 4d));
        }
    }

    internal bool IsVirtualCellPoint(Point point) => TryHitVirtualCell(point, out _, out _);

    internal bool HandleVirtualCellPointerPressed(PointerPressedEventArgs e)
    {
        DataGrid? grid = OwningGrid;
        if (grid?.UsesVirtualCellSurface != true ||
            !e.GetCurrentPoint(_virtualCellSurface).Properties.IsLeftButtonPressed ||
            !TryHitVirtualCell(e.GetPosition(_virtualCellSurface), out DataGridRow? row, out DataGridColumn? column))
        {
            return false;
        }

        Point point = e.GetPosition(_virtualCellSurface);
        if (column is DataGridHierarchicalColumn && row.DataContext is HierarchicalNode node && !node.IsLeaf)
        {
            FlatColumnLayout layout = FindFlatColumnLayout(column);
            double expanderLeft = GetVirtualCellBounds(grid, row, layout, row.GetFlatCellsHeight()).Left +
                (Math.Max(0, node.Level) * ((DataGridHierarchicalColumn)column).Indent);
            if (point.X >= expanderLeft && point.X <= expanderLeft + VirtualExpanderSize)
            {
                return grid.TryToggleHierarchicalAtSlot(row.Slot);
            }
        }

        bool allowEdit = !grid.IsReadOnly && !column.IsReadOnly;
        return grid.UpdateStateOnMouseLeftButtonDown(e, column.Index, row.Slot, allowEdit);
    }

    private bool TryHitVirtualCell(Point point, out DataGridRow? row, out DataGridColumn? column)
    {
        row = null;
        column = null;
        DataGrid? grid = OwningGrid;
        if (grid?.UsesVirtualCellSurface != true)
        {
            return false;
        }

        double headerWidth = grid.AreRowHeadersVisible ? grid.RowHeadersDesiredWidth : 0d;
        if (point.X < headerWidth)
        {
            return false;
        }

        foreach (DataGridRow candidate in grid.DisplayData.GetScrollingRows())
        {
            double height = candidate.GetFlatCellsHeight();
            if (point.Y < candidate.Bounds.Top || point.Y >= candidate.Bounds.Top + height)
            {
                continue;
            }

            for (int layoutIndex = _flatColumnLayouts.Count - 1; layoutIndex >= 0; layoutIndex--)
            {
                FlatColumnLayout layout = _flatColumnLayouts[layoutIndex];
                if (!layout.ShouldDisplay)
                {
                    continue;
                }

                Rect cellBounds = GetVirtualCellBounds(grid, candidate, layout, height);
                if (GetVisibleVirtualCellBounds(grid, layout.Column, cellBounds).Contains(point))
                {
                    row = candidate;
                    column = layout.Column;
                    return true;
                }
            }

            return false;
        }

        return false;
    }

    private FlatColumnLayout FindFlatColumnLayout(DataGridColumn column)
    {
        for (int index = 0; index < _flatColumnLayouts.Count; index++)
        {
            FlatColumnLayout layout = _flatColumnLayouts[index];
            if (ReferenceEquals(layout.Column, column))
            {
                return layout;
            }
        }

        return default;
    }

    private static Rect GetVirtualCellBounds(
        DataGrid grid,
        DataGridRow row,
        FlatColumnLayout layout,
        double height)
    {
        double headerWidth = grid.AreRowHeadersVisible ? grid.RowHeadersDesiredWidth : 0d;
        return new Rect(
            headerWidth + layout.Left,
            row.Bounds.Top,
            layout.Column.LayoutRoundedWidth,
            height);
    }

    private static Rect GetVisibleVirtualCellBounds(
        DataGrid grid,
        DataGridColumn column,
        Rect bounds)
    {
        double headerWidth = grid.AreRowHeadersVisible ? grid.RowHeadersDesiredWidth : 0d;
        double left = Math.Max(bounds.Left, headerWidth);
        double right = Math.Min(bounds.Right, headerWidth + grid.CellsWidth);
        if (!column.IsFrozen)
        {
            left = Math.Max(left, headerWidth + grid.GetVisibleFrozenColumnsWidthLeft());
            double frozenRight = grid.GetVisibleFrozenColumnsWidthRight();
            if (frozenRight > 0d)
            {
                right = Math.Min(right, headerWidth + Math.Max(0d, grid.CellsWidth - frozenRight));
            }
        }

        return new Rect(left, bounds.Top, Math.Max(0d, right - left), bounds.Height);
    }

    internal bool TryGetVirtualCellBounds(DataGridRow row, DataGridColumn column, out Rect bounds)
    {
        bounds = default;
        DataGrid? grid = OwningGrid;
        if (grid?.UsesVirtualCellSurface != true)
        {
            return false;
        }

        FlatColumnLayout layout = FindFlatColumnLayout(column);
        if (!ReferenceEquals(layout.Column, column) || !layout.ShouldDisplay)
        {
            return false;
        }

        bounds = GetVisibleVirtualCellBounds(
            grid,
            column,
            GetVirtualCellBounds(grid, row, layout, row.GetFlatCellsHeight()));
        return bounds.Width > 0d && bounds.Height > 0d;
    }

    private void SyncVirtualValueNotifiers()
    {
        DataGrid? grid = OwningGrid;
        if (grid?.UsesVirtualCellSurface != true)
        {
            ClearVirtualValueNotifiers();
            return;
        }

        if (!ShouldTrackVirtualValueChanges())
        {
            ClearVirtualValueNotifiers();
            return;
        }

        _nextVirtualValueNotifiers.Clear();
        foreach (DataGridRow row in grid.DisplayData.GetScrollingRows())
        {
            if (row.DataContext is INotifyPropertyChanged rowNotifier)
            {
                _nextVirtualValueNotifiers.Add(rowNotifier);
            }

            if (row.DataContext is HierarchicalNode node &&
                node.Item is INotifyPropertyChanged itemNotifier)
            {
                _nextVirtualValueNotifiers.Add(itemNotifier);
            }
        }

        foreach (INotifyPropertyChanged notifier in _virtualValueNotifiers)
        {
            if (!_nextVirtualValueNotifiers.Contains(notifier))
            {
                notifier.PropertyChanged -= OnVirtualValuePropertyChanged;
            }
        }

        foreach (INotifyPropertyChanged notifier in _nextVirtualValueNotifiers)
        {
            if (!_virtualValueNotifiers.Contains(notifier))
            {
                notifier.PropertyChanged += OnVirtualValuePropertyChanged;
            }
        }

        (_virtualValueNotifiers, _nextVirtualValueNotifiers) =
            (_nextVirtualValueNotifiers, _virtualValueNotifiers);
    }

    private bool ShouldTrackVirtualValueChanges()
    {
        for (int index = 0; index < _flatColumnLayouts.Count; index++)
        {
            DataGridColumn column = _flatColumnLayouts[index].Column;
            if (column is IDataGridDrawnCellValueChangeTracking tracking)
            {
                if (tracking.TrackDrawnCellValueChanges)
                {
                    return true;
                }

                continue;
            }

            if (column is DataGridHierarchicalColumn hierarchicalColumn)
            {
                if (hierarchicalColumn.TrackDirectTextValueChanges)
                {
                    return true;
                }

                continue;
            }

            return true;
        }

        return false;
    }

    private void ClearVirtualValueNotifiers()
    {
        foreach (INotifyPropertyChanged notifier in _virtualValueNotifiers)
        {
            notifier.PropertyChanged -= OnVirtualValuePropertyChanged;
        }

        _virtualValueNotifiers.Clear();
        _nextVirtualValueNotifiers.Clear();
    }

    private void OnVirtualValuePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        Interlocked.Increment(ref _virtualValueChangeCount);
        if (Dispatcher.UIThread.CheckAccess())
        {
            InvalidateVirtualCellSurface();
        }
        else
        {
            Dispatcher.UIThread.Post(InvalidateVirtualCellSurface, DispatcherPriority.Render);
        }
    }

    private void ResolveVirtualTextStyle(out FontFamily fontFamily, out double fontSize, out IBrush foreground)
    {
        fontFamily = _virtualCellSurface?.GetValue(TextElement.FontFamilyProperty) ?? FontFamily.Default;
        fontSize = FindVirtualResource<double>("DataGridCellFontSize");
        if (!double.IsFinite(fontSize) || fontSize <= 0d)
        {
            fontSize = 14d;
        }

        foreground = _virtualCellSurface?.GetValue(TextElement.ForegroundProperty) ?? Brushes.Black;
    }

    private T? FindVirtualResource<T>(string key)
    {
        return OwningGrid?.TryFindResource(key, out object? resource) == true && resource is T value
            ? value
            : default;
    }

    private static void GetBrushIdentity(
        IBrush brush,
        out byte kind,
        out Color color,
        out double opacity,
        out int identity)
    {
        if (brush is ISolidColorBrush solid)
        {
            kind = 1;
            color = solid.Color;
            opacity = solid.Opacity;
            identity = 0;
            return;
        }

        kind = 2;
        color = default;
        opacity = brush.Opacity;
        identity = RuntimeHelpers.GetHashCode(brush);
    }
}
