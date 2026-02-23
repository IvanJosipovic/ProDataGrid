// Copyright (c) Wieslaw Soltes. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

using Avalonia.Controls;
using Avalonia.Controls.DataGridSearching;

namespace DataGridSample.Adapters
{
    /// <summary>
    /// Factory that creates the accessor-only search adapter.
    /// </summary>
    public sealed class AccessorSearchAdapterFactory : IDataGridSearchAdapterFactory
    {
        public DataGridSearchAdapter Create(DataGrid grid, ISearchModel model)
        {
            return new DataGridAccessorSearchAdapter(model, () => grid.ColumnDefinitions, grid?.FastPathOptions);
        }
    }
}
