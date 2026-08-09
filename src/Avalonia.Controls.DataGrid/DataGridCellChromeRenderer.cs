// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

#nullable enable

using System;
using Avalonia.Media;

namespace Avalonia.Controls
{
    internal static class DataGridCellChromeRenderer
    {
        public static void Render(
            DataGridCell cell,
            DrawingContext context,
            ref IBrush? cachedBorderBrush,
            ref double cachedBorderThickness,
            ref Pen? cachedBorderPen)
        {
            var bounds = new Rect(cell.Bounds.Size);
            var borderThickness = cell.BorderThickness;
            var borderBrush = cell.BorderBrush;
            var hasBorder = borderBrush != null &&
                            (borderThickness.Left > 0d ||
                             borderThickness.Top > 0d ||
                             borderThickness.Right > 0d ||
                             borderThickness.Bottom > 0d);

            if (cell.Background != null || hasBorder)
            {
                if (hasBorder && borderThickness.IsUniform)
                {
                    var thickness = borderThickness.Top;
                    if (!ReferenceEquals(cachedBorderBrush, borderBrush) ||
                        !cachedBorderThickness.Equals(thickness))
                    {
                        cachedBorderBrush = borderBrush;
                        cachedBorderThickness = thickness;
                        cachedBorderPen = new Pen(borderBrush, thickness);
                    }

                    var inset = thickness * 0.5d;
                    var chromeBounds = new Rect(
                        inset,
                        inset,
                        Math.Max(0d, bounds.Width - thickness),
                        Math.Max(0d, bounds.Height - thickness));
                    context.DrawRectangle(
                        cell.Background,
                        cachedBorderPen,
                        new RoundedRect(chromeBounds, cell.CornerRadius));
                }
                else
                {
                    using (context.PushClip(new RoundedRect(bounds, cell.CornerRadius)))
                    {
                        if (cell.Background != null)
                        {
                            context.DrawRectangle(cell.Background, null, bounds);
                        }

                        if (hasBorder)
                        {
                            DrawBorderSides(context, borderBrush!, borderThickness, bounds);
                        }
                    }
                }
            }

            DrawVerticalGridLine(cell, context, bounds);
        }

        private static void DrawBorderSides(
            DrawingContext context,
            IBrush borderBrush,
            Thickness thickness,
            Rect bounds)
        {
            if (thickness.Top > 0d)
            {
                context.DrawRectangle(
                    borderBrush,
                    null,
                    new Rect(0d, 0d, bounds.Width, Math.Min(thickness.Top, bounds.Height)));
            }

            if (thickness.Bottom > 0d)
            {
                var height = Math.Min(thickness.Bottom, bounds.Height);
                context.DrawRectangle(
                    borderBrush,
                    null,
                    new Rect(0d, Math.Max(0d, bounds.Height - height), bounds.Width, height));
            }

            if (thickness.Left > 0d)
            {
                context.DrawRectangle(
                    borderBrush,
                    null,
                    new Rect(0d, 0d, Math.Min(thickness.Left, bounds.Width), bounds.Height));
            }

            if (thickness.Right > 0d)
            {
                var width = Math.Min(thickness.Right, bounds.Width);
                context.DrawRectangle(
                    borderBrush,
                    null,
                    new Rect(Math.Max(0d, bounds.Width - width), 0d, width, bounds.Height));
            }
        }

        private static void DrawVerticalGridLine(DataGridCell cell, DrawingContext context, Rect bounds)
        {
            var grid = cell.OwningGrid;
            if (grid == null ||
                grid.VerticalGridLinesBrush == null ||
                (grid.GridLinesVisibility != DataGridGridLinesVisibility.Vertical &&
                 grid.GridLinesVisibility != DataGridGridLinesVisibility.All) ||
                (!grid.ColumnsInternal.FillerColumn.IsActive &&
                 ReferenceEquals(cell.OwningColumn, grid.ColumnsInternal.LastVisibleColumn)))
            {
                return;
            }

            const double width = 1d;
            context.DrawRectangle(
                grid.VerticalGridLinesBrush,
                null,
                new Rect(Math.Max(0d, bounds.Width - width), 0d, Math.Min(width, bounds.Width), bounds.Height));
        }
    }
}
