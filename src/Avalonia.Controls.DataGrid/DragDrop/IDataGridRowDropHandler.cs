// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

using System.Collections;
using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Controls.DataGridDragDrop;

namespace Avalonia.Controls.DataGridDragDrop
{
    public sealed class DataGridRowDropEventArgs
    {
        public DataGridRowDropEventArgs(
            DataGrid grid,
            IList? targetList,
            IReadOnlyList<object> items,
            IReadOnlyList<int> sourceIndices,
            object? targetItem,
            int targetIndex,
            int insertIndex,
            DataGridRow? targetRow,
            DataGridRowDropPosition position,
            bool isSameGrid,
            DragDropEffects requestedEffect,
            DragEventArgs dragEventArgs)
        {
            Grid = grid;
            TargetList = targetList;
            Items = items;
            SourceIndices = sourceIndices;
            TargetItem = targetItem;
            TargetIndex = targetIndex;
            InsertIndex = insertIndex;
            TargetRow = targetRow;
            Position = position;
            IsSameGrid = isSameGrid;
            RequestedEffect = requestedEffect;
            DragEventArgs = dragEventArgs;
        }

        public DataGrid Grid { get; }

        public IList? TargetList { get; }

        public IReadOnlyList<object> Items { get; }

        public IReadOnlyList<int> SourceIndices { get; }

        public object? TargetItem { get; }

        public int TargetIndex { get; }

        public int InsertIndex { get; }

        public DataGridRow? TargetRow { get; }

        public DataGridRowDropPosition Position { get; }

        public bool IsSameGrid { get; }

        public DragDropEffects RequestedEffect { get; set; }

        public DragEventArgs DragEventArgs { get; }
    }

    public interface IDataGridRowDropHandler
    {
        bool Validate(DataGridRowDropEventArgs args);

        bool Execute(DataGridRowDropEventArgs args);
    }
}
