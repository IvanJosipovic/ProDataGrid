using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using DataGridSample.Models;

namespace DataGridSample
{
    public static class StateSampleOptionsFactory
    {
        public static DataGridStateOptions Create(DataGrid grid, IEnumerable<StateSampleItem> items)
        {
            return new DataGridStateOptions
            {
                ItemKeySelector = item => item is StateSampleItem sample ? sample.Id : null,
                ItemKeyResolver = key => key is int id ? items.FirstOrDefault(item => item.Id == id) : null,
                ColumnKeySelector = column => column.Header?.ToString(),
                ColumnKeyResolver = key => grid.Columns.FirstOrDefault(
                    column => string.Equals(column.Header?.ToString(), key?.ToString(), StringComparison.Ordinal)),
            };
        }
    }
}
