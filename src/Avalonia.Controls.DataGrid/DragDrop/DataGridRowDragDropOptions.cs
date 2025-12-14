// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

using Avalonia.Input;

namespace Avalonia.Controls.DataGridDragDrop
{
    public enum DataGridRowDragHandle
    {
        RowHeader,
        Row,
        RowHeaderAndRow
    }

    public enum DataGridRowDropPosition
    {
        Before,
        After,
        Inside
    }

    public class DataGridRowDragDropOptions
    {
        public DragDropEffects AllowedEffects { get; set; } = DragDropEffects.Move;

        public double HorizontalDragThreshold { get; set; } = 4;

        public double VerticalDragThreshold { get; set; } = 4;

        public bool DragSelectedRows { get; set; } = true;
    }
}
