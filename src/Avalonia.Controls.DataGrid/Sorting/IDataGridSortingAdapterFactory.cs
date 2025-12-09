// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

using Avalonia.Controls;

namespace Avalonia.Controls.DataGridSorting
{
    /// <summary>
    /// Factory for creating a <see cref="DataGridSortingAdapter"/> without subclassing <see cref="DataGrid"/>.
    /// </summary>
    public interface IDataGridSortingAdapterFactory
    {
        /// <summary>
        /// Creates a sorting adapter for the given grid and model.
        /// </summary>
        /// <param name="grid">Owning grid.</param>
        /// <param name="model">Sorting model to bridge.</param>
        /// <returns>Adapter instance.</returns>
        DataGridSortingAdapter Create(DataGrid grid, ISortingModel model);
    }
}
