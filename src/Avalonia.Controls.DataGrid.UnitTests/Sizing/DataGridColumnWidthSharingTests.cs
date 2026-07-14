// Copyright (c) Wieslaw Soltes. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

using System.Collections.ObjectModel;
using Avalonia.Controls;
using Avalonia.Controls.DataGridSizing;
using Avalonia.Markup.Xaml;
using Xunit;

namespace Avalonia.Controls.DataGridTests.Sizing;

public class DataGridColumnWidthSharingTests
{
    [Fact]
    public void Scope_Uses_Largest_Initial_Width_And_Propagates_Later_Changes()
    {
        var scope = new DataGridColumnWidthSharingScope();
        DataGridTextColumn firstColumn = CreateColumn(100, "name");
        DataGridTextColumn secondColumn = CreateColumn(180, "name");

        DataGrid firstGrid = CreateGrid(scope, firstColumn);
        DataGrid secondGrid = CreateGrid(scope, secondColumn);

        Assert.Equal(180, firstColumn.ActualWidth);
        Assert.Equal(180, secondColumn.ActualWidth);

        firstColumn.Resize(firstColumn.Width, new DataGridLength(240), userInitiated: true);

        Assert.Equal(240, firstColumn.ActualWidth);
        Assert.Equal(240, secondColumn.ActualWidth);

        secondColumn.Width = new DataGridLength(80);

        Assert.Equal(80, firstColumn.ActualWidth);
        Assert.Equal(80, secondColumn.ActualWidth);
        Assert.Same(scope, firstGrid.ColumnWidthSharingScope);
        Assert.Same(scope, secondGrid.ColumnWidthSharingScope);
    }

    [Fact]
    public void Scope_Keeps_Different_Groups_Independent()
    {
        var scope = new DataGridColumnWidthSharingScope();
        DataGridTextColumn nameColumn = CreateColumn(100, "name");
        DataGridTextColumn amountColumn = CreateColumn(160, "amount");
        CreateGrid(scope, nameColumn);
        CreateGrid(scope, amountColumn);

        nameColumn.Width = new DataGridLength(225);

        Assert.Equal(225, nameColumn.ActualWidth);
        Assert.Equal(160, amountColumn.ActualWidth);
    }

    [Fact]
    public void Removed_Column_Is_No_Longer_Synchronized()
    {
        var scope = new DataGridColumnWidthSharingScope();
        DataGridTextColumn firstColumn = CreateColumn(100, "name");
        DataGridTextColumn removedColumn = CreateColumn(140, "name");
        CreateGrid(scope, firstColumn);
        DataGrid secondGrid = CreateGrid(scope, removedColumn);

        secondGrid.Columns.Remove(removedColumn);
        var independentGrid = new DataGrid();
        independentGrid.Columns.Add(removedColumn);
        firstColumn.Width = new DataGridLength(260);

        Assert.Equal(260, firstColumn.ActualWidth);
        Assert.Equal(140, removedColumn.ActualWidth);
    }

    [Fact]
    public void Changing_Group_Updates_Registration()
    {
        var scope = new DataGridColumnWidthSharingScope();
        DataGridTextColumn firstColumn = CreateColumn(100, "name");
        DataGridTextColumn secondColumn = CreateColumn(160, "name");
        CreateGrid(scope, firstColumn);
        CreateGrid(scope, secondColumn);

        DataGridColumnWidthSharing.SetGroup(secondColumn, "other");
        firstColumn.Width = new DataGridLength(210);

        Assert.Equal(210, firstColumn.ActualWidth);
        Assert.Equal(160, secondColumn.ActualWidth);

        DataGridColumnWidthSharing.SetGroup(secondColumn, "name");

        Assert.Equal(210, secondColumn.ActualWidth);
    }

    [Fact]
    public void Changing_Grid_Scope_Unregisters_Columns_From_The_Old_Scope()
    {
        var oldScope = new DataGridColumnWidthSharingScope();
        var newScope = new DataGridColumnWidthSharingScope();
        DataGridTextColumn firstColumn = CreateColumn(100, "name");
        DataGridTextColumn secondColumn = CreateColumn(160, "name");
        CreateGrid(oldScope, firstColumn);
        DataGrid secondGrid = CreateGrid(oldScope, secondColumn);

        secondGrid.ColumnWidthSharingScope = newScope;
        firstColumn.Width = new DataGridLength(220);

        Assert.Equal(220, firstColumn.ActualWidth);
        Assert.Equal(160, secondColumn.ActualWidth);
    }

    [Fact]
    public void Scope_Uses_Common_Constraints_And_Preserves_Auto_Sizing_Mode()
    {
        var scope = new DataGridColumnWidthSharingScope();
        DataGridTextColumn constrainedColumn = CreateColumn(100, "name");
        constrainedColumn.MaxWidth = 150;
        DataGridTextColumn autoColumn = CreateColumn(DataGridLength.Auto, "name");
        CreateGrid(scope, constrainedColumn);
        CreateGrid(scope, autoColumn);

        autoColumn.Width = new DataGridLength(220);

        Assert.Equal(150, constrainedColumn.ActualWidth);
        Assert.Equal(150, autoColumn.ActualWidth);

        autoColumn.Width = DataGridLength.Auto;
        scope.Synchronize();

        Assert.True(autoColumn.Width.IsAuto);
        Assert.Equal(constrainedColumn.ActualWidth, autoColumn.ActualWidth);
    }

    [Fact]
    public void Column_Definitions_Apply_And_Update_Width_Sharing_Group()
    {
        var scope = new DataGridColumnWidthSharingScope();
        var firstDefinition = new DataGridTextColumnDefinition
        {
            Width = new DataGridLength(100),
            WidthSharingGroup = "name"
        };
        var secondDefinition = new DataGridTextColumnDefinition
        {
            Width = new DataGridLength(175),
            WidthSharingGroup = "name"
        };
        DataGrid firstGrid = CreateGrid(scope, firstDefinition);
        DataGrid secondGrid = CreateGrid(scope, secondDefinition);
        DataGridColumn firstColumn = firstGrid.Columns[0];
        DataGridColumn secondColumn = secondGrid.Columns[0];

        Assert.Equal("name", DataGridColumnWidthSharing.GetGroup(firstColumn));
        Assert.Equal(175, firstColumn.ActualWidth);
        Assert.Equal(175, secondColumn.ActualWidth);

        secondDefinition.WidthSharingGroup = "other";
        firstColumn.Width = new DataGridLength(230);

        Assert.Equal("other", DataGridColumnWidthSharing.GetGroup(secondColumn));
        Assert.Equal(230, firstColumn.ActualWidth);
        Assert.Equal(175, secondColumn.ActualWidth);
    }

    [Fact]
    public void Scope_And_Attached_Group_Can_Be_Configured_In_Xaml()
    {
        const string xaml = """
                            <StackPanel xmlns="https://github.com/avaloniaui"
                                        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
                                        xmlns:sizing="using:Avalonia.Controls.DataGridSizing">
                              <StackPanel.Resources>
                                <sizing:DataGridColumnWidthSharingScope x:Key="SharedWidths" />
                              </StackPanel.Resources>
                              <DataGrid ColumnWidthSharingScope="{StaticResource SharedWidths}">
                                <DataGrid.Columns>
                                  <DataGridTextColumn Width="100"
                                                      sizing:DataGridColumnWidthSharing.Group="name" />
                                </DataGrid.Columns>
                              </DataGrid>
                              <DataGrid ColumnWidthSharingScope="{StaticResource SharedWidths}">
                                <DataGrid.Columns>
                                  <DataGridTextColumn Width="180"
                                                      sizing:DataGridColumnWidthSharing.Group="name" />
                                </DataGrid.Columns>
                              </DataGrid>
                            </StackPanel>
                            """;

        StackPanel panel = AvaloniaRuntimeXamlLoader.Parse<StackPanel>(xaml, typeof(DataGrid).Assembly);
        DataGrid firstGrid = Assert.IsType<DataGrid>(panel.Children[0]);
        DataGrid secondGrid = Assert.IsType<DataGrid>(panel.Children[1]);

        Assert.Same(firstGrid.ColumnWidthSharingScope, secondGrid.ColumnWidthSharingScope);
        Assert.Equal(180, firstGrid.Columns[0].ActualWidth);
        Assert.Equal(180, secondGrid.Columns[0].ActualWidth);
    }

    private static DataGridTextColumn CreateColumn(double width, string group)
    {
        return CreateColumn(new DataGridLength(width), group);
    }

    private static DataGridTextColumn CreateColumn(DataGridLength width, string group)
    {
        var column = new DataGridTextColumn
        {
            Width = width
        };
        DataGridColumnWidthSharing.SetGroup(column, group);
        return column;
    }

    private static DataGrid CreateGrid(
        DataGridColumnWidthSharingScope scope,
        DataGridColumn column)
    {
        var grid = new DataGrid
        {
            ColumnWidthSharingScope = scope
        };
        grid.Columns.Add(column);
        return grid;
    }

    private static DataGrid CreateGrid(
        DataGridColumnWidthSharingScope scope,
        DataGridColumnDefinition definition)
    {
        return new DataGrid
        {
            ColumnWidthSharingScope = scope,
            ColumnDefinitionsSource = new ObservableCollection<DataGridColumnDefinition> { definition }
        };
    }
}
