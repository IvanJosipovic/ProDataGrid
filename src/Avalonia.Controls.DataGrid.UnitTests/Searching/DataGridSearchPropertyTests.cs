// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

using System;
using System.Collections.Generic;
using System.Reflection;
using Avalonia.Controls;
using Avalonia.Controls.DataGridSearching;
using Avalonia.Headless.XUnit;
using Xunit;

namespace Avalonia.Controls.DataGridTests.Searching;

public class DataGridSearchPropertyTests
{
    [AvaloniaFact]
    public void SearchModel_Property_Raises_PropertyChanged_On_Replace()
    {
        var grid = new DataGrid();
        var newModel = new SearchModel();
        var propertyNames = new List<string>();

        grid.PropertyChanged += (_, e) =>
        {
            if (e.Property == DataGrid.SearchModelProperty)
            {
                propertyNames.Add(e.Property.Name);
                Assert.Same(newModel, e.NewValue);
            }
        };

        grid.SearchModel = newModel;

        Assert.Equal(new[] { nameof(DataGrid.SearchModel) }, propertyNames);
    }

    [AvaloniaFact]
    public void Changing_SearchAdapterFactory_Recreates_Adapter()
    {
        var grid = new DataGrid();
        var factory = new CountingSearchAdapterFactory();

        grid.SearchAdapterFactory = factory;

        Assert.Equal(1, factory.CreateCount);

        var field = typeof(DataGrid).GetField("_searchAdapter", BindingFlags.Instance | BindingFlags.NonPublic);
        var adapter = field!.GetValue(grid);
        Assert.IsType<CountingSearchAdapterFactory.CountingSearchAdapter>(adapter);
    }

    [AvaloniaFact]
    public void Changing_Search_FastPath_Options_Recreates_Accessor_Adapter()
    {
        var grid = new DataGrid
        {
            SearchModel = new SearchModel(),
            FastPathOptions = new DataGridFastPathOptions
            {
                UseAccessorsOnly = true
            }
        };

        var field = typeof(DataGrid).GetField("_searchAdapter", BindingFlags.Instance | BindingFlags.NonPublic);
        var firstAdapter = field!.GetValue(grid);
        Assert.IsType<DataGridAccessorSearchAdapter>(firstAdapter);

        grid.FastPathOptions.EnableHighPerformanceSearching = true;

        var secondAdapter = field.GetValue(grid);
        Assert.IsType<DataGridAccessorSearchAdapter>(secondAdapter);
        Assert.NotSame(firstAdapter, secondAdapter);

        grid.FastPathOptions.HighPerformanceSearchTrackItemChanges = false;

        var thirdAdapter = field.GetValue(grid);
        Assert.IsType<DataGridAccessorSearchAdapter>(thirdAdapter);
        Assert.NotSame(secondAdapter, thirdAdapter);
    }

    private sealed class CountingSearchAdapterFactory : IDataGridSearchAdapterFactory
    {
        public int CreateCount { get; private set; }

        public DataGridSearchAdapter Create(DataGrid grid, ISearchModel model)
        {
            CreateCount++;
            return new CountingSearchAdapter(model, () => grid.ColumnDefinitions);
        }

        internal sealed class CountingSearchAdapter : DataGridSearchAdapter
        {
            public CountingSearchAdapter(ISearchModel model, Func<IEnumerable<DataGridColumn>> columns)
                : base(model, columns)
            {
            }
        }
    }
}
