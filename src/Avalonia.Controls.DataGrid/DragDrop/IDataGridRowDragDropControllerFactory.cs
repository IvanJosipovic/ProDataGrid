// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

namespace Avalonia.Controls.DataGridDragDrop
{
    public interface IDataGridRowDragDropController : System.IDisposable
    {
        DataGrid Grid { get; }
    }

    public interface IDataGridRowDragDropControllerFactory
    {
        IDataGridRowDragDropController Create(
            DataGrid grid,
            IDataGridRowDropHandler dropHandler,
            DataGridRowDragDropOptions options);
    }
}
