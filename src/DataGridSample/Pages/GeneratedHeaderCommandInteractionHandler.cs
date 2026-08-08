// Copyright (c) Wieslaw Soltes. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

using System.Threading.Tasks;
using Avalonia.Controls;

namespace DataGridSample.Pages;

public sealed class GeneratedHeaderCommandInteractionHandler :
    IDataGridGeneratedViewInteractionHandler<DataGridGeneratedHeaderCommandRequest, bool>
{
    public ValueTask<bool> HandleAsync(
        DataGridGeneratedViewInteractionContext<DataGridGeneratedHeaderCommandRequest> context)
    {
        context.CancellationToken.ThrowIfCancellationRequested();
        DataGrid grid = context.DataGrid;
        DataGridGeneratedHeaderCommandRequest request = context.Input;
        DataGridColumn? column = FindColumn(grid, request.ColumnKey);
        if (column is null)
        {
            return ValueTask.FromResult(false);
        }

        switch (request.Kind)
        {
            case DataGridGeneratedHeaderCommandKind.PinLeft:
                column.DisplayIndex = 0;
                grid.FrozenColumnCountRight = 0;
                grid.FrozenColumnCount = 1;
                break;
            case DataGridGeneratedHeaderCommandKind.PinRight:
                column.DisplayIndex = grid.Columns.Count - 1;
                grid.FrozenColumnCount = 0;
                grid.FrozenColumnCountRight = 1;
                break;
            case DataGridGeneratedHeaderCommandKind.Unpin:
            case DataGridGeneratedHeaderCommandKind.ClearFrozenColumns:
                grid.FrozenColumnCount = 0;
                grid.FrozenColumnCountRight = 0;
                break;
            case DataGridGeneratedHeaderCommandKind.FreezeThrough:
                grid.FrozenColumnCountRight = 0;
                grid.FrozenColumnCount = column.DisplayIndex + 1;
                break;
            case DataGridGeneratedHeaderCommandKind.AutoSize:
                column.Width = DataGridLength.Auto;
                break;
            case DataGridGeneratedHeaderCommandKind.ResetLayout:
                grid.FrozenColumnCount = 0;
                grid.FrozenColumnCountRight = 0;
                break;
            default:
                return ValueTask.FromResult(false);
        }

        return ValueTask.FromResult(true);
    }

    private static DataGridColumn? FindColumn(DataGrid grid, string columnKey)
    {
        for (int index = 0; index < grid.Columns.Count; index++)
        {
            DataGridColumn column = grid.Columns[index];
            if (Equals(column.ColumnKey, columnKey))
            {
                return column;
            }
        }

        return null;
    }
}
