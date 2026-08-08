// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using Avalonia.Controls;
using Avalonia.Controls.DataGridHierarchical;
using Avalonia.Data;
using Avalonia.Headless.XUnit;
using Avalonia.VisualTree;
using Xunit;

namespace Avalonia.Controls.DataGridTests;

public class DataGridCellLifecycleTests
{
    [AvaloniaFact]
    public void Prepared_Is_Raised_Once_For_Each_Realization_Assignment()
    {
        var items = CreateItems(100);
        var (window, grid) = CreateGrid(items);
        var prepared = new List<DataGridCellLifecycleEventArgs>();
        grid.CellPrepared += (_, e) => prepared.Add(e);

        window.Show();
        grid.UpdateLayout();

        Assert.NotEmpty(prepared);
        Assert.Equal(
            prepared.Count,
            prepared.Select(e => (e.Cell, e.RowDataContext)).Distinct().Count());
        Assert.All(prepared, e =>
        {
            Assert.Same(e.Row.DataContext, e.RowDataContext);
            Assert.Same(e.RowDataContext, e.Item);
            Assert.Same(e.Column, e.Cell.OwningColumn);
            Assert.Empty(e.HierarchyPath);
            Assert.Null(e.HierarchicalNode);
        });

        prepared.Clear();
        grid.ScrollIntoView(items[60], grid.ColumnsInternal[0]);
        grid.UpdateLayout();

        Assert.NotEmpty(prepared);
        Assert.Equal(
            prepared.Count,
            prepared.Select(e => (e.Cell, e.RowDataContext)).Distinct().Count());
    }

    [AvaloniaFact]
    public void Clearing_Is_Raised_Once_Before_Recycled_Row_Context_Changes()
    {
        var items = CreateItems(100);
        var (window, grid) = CreateGrid(items);
        var clearing = new List<DataGridCellLifecycleEventArgs>();
        grid.CellClearing += (_, e) =>
        {
            Assert.Same(e.RowDataContext, e.Row.DataContext);
            Assert.Same(e.Column, e.Cell.OwningColumn);
            clearing.Add(e);
        };

        window.Show();
        grid.UpdateLayout();
        grid.ScrollIntoView(items[60], grid.ColumnsInternal[0]);
        grid.UpdateLayout();

        Assert.NotEmpty(clearing);
        Assert.Equal(
            clearing.Count,
            clearing.Select(e => (e.Cell, e.RowDataContext)).Distinct().Count());
        Assert.All(clearing, e => Assert.Contains(e.RowDataContext, items));
    }

    [AvaloniaFact]
    public void Lifecycle_Events_Expose_Underlying_Item_And_Root_To_Node_Path()
    {
        var parentItem = new Item("parent");
        var childItem = new Item("child");
        var parent = new HierarchicalNode(parentItem);
        var child = new HierarchicalNode(childItem, parent, level: 1, isLeaf: true);
        var (window, grid) = CreateGrid(new[] { child });
        DataGridCellLifecycleEventArgs? prepared = null;
        grid.CellPrepared += (_, e) => prepared ??= e;

        window.Show();
        grid.UpdateLayout();

        Assert.NotNull(prepared);
        Assert.Same(child, prepared.RowDataContext);
        Assert.Same(childItem, prepared.Item);
        Assert.Same(child, prepared.HierarchicalNode);
        Assert.Equal(new[] { parent, child }, prepared.HierarchyPath);
    }

    [AvaloniaFact]
    public void Successful_Edit_Commit_Raises_One_Value_Changed_Event_With_Typed_Values()
    {
        var item = new Item("old");
        var column = CreateTextColumn();
        DataGridColumnMetadata.SetValueAccessor(
            column,
            new DataGridColumnValueAccessor<Item, string>(
                static value => value.Name,
                static (value, text) => value.Name = text));
        var (window, grid) = CreateGrid(new[] { item }, column);
        var changes = new List<DataGridCellValueChangedEventArgs>();
        grid.CellValueChanged += (_, e) => changes.Add(e);

        window.Show();
        grid.UpdateLayout();
        BeginEdit(grid, 0, 0);
        DataGridCell cell = GetCell(grid, 0, 0);
        Assert.IsType<TextBox>(cell.Content).Text = "new";

        Assert.True(grid.CommitEdit());
        grid.UpdateLayout();

        DataGridCellValueChangedEventArgs change = Assert.Single(changes);
        Assert.Same(cell, change.Cell);
        Assert.Same(item, change.Item);
        Assert.Same(item, change.RowDataContext);
        Assert.Same(column, change.Column);
        Assert.Equal("old", change.OldValue);
        Assert.Equal("new", change.NewValue);
        Assert.Equal(DataGridCellValueChangeOrigin.EditCommit, change.Origin);
    }

    [AvaloniaFact]
    public void Cancel_NoOp_And_Programmatic_Changes_Do_Not_Raise_Value_Changed()
    {
        var item = new Item("old");
        var column = CreateTextColumn();
        DataGridColumnMetadata.SetValueAccessor(
            column,
            new DataGridColumnValueAccessor<Item, string>(
                static value => value.Name,
                static (value, text) => value.Name = text));
        var (window, grid) = CreateGrid(new[] { item }, column);
        var changes = new List<DataGridCellValueChangedEventArgs>();
        grid.CellValueChanged += (_, e) => changes.Add(e);

        window.Show();
        grid.UpdateLayout();

        BeginEdit(grid, 0, 0);
        Assert.IsType<TextBox>(GetCell(grid, 0, 0).Content).Text = "cancelled";
        grid.CancelEdit();

        BeginEdit(grid, 0, 0);
        Assert.True(grid.CommitEdit());

        item.Name = "programmatic";
        grid.UpdateLayout();

        Assert.Empty(changes);
    }

    [AvaloniaFact]
    public void CheckBox_Edit_Commit_Uses_The_Same_Value_Changed_Contract()
    {
        var item = new Item("item") { Flag = false };
        var column = new DataGridCheckBoxColumn
        {
            Header = "Flag",
            Binding = new Binding(nameof(Item.Flag)),
        };
        DataGridColumnMetadata.SetValueAccessor(
            column,
            new DataGridColumnValueAccessor<Item, bool>(
                static value => value.Flag,
                static (value, flag) => value.Flag = flag));
        var (window, grid) = CreateGrid(new[] { item }, column);
        var changes = new List<DataGridCellValueChangedEventArgs>();
        grid.CellValueChanged += (_, e) => changes.Add(e);

        window.Show();
        grid.UpdateLayout();
        BeginEdit(grid, 0, 0);
        Assert.IsType<CheckBox>(GetCell(grid, 0, 0).Content).IsChecked = true;

        Assert.True(grid.CommitEdit());

        DataGridCellValueChangedEventArgs change = Assert.Single(changes);
        Assert.Equal(false, change.OldValue);
        Assert.Equal(true, change.NewValue);
        Assert.True(item.Flag);
    }

    private static ObservableCollection<Item> CreateItems(int count) =>
        new(Enumerable.Range(0, count).Select(index => new Item($"item-{index}")));

    private static DataGridTextColumn CreateTextColumn() => new()
    {
        Header = "Name",
        Binding = new Binding(nameof(Item.Name)),
    };

    private static (Window Window, DataGrid Grid) CreateGrid(
        IEnumerable<object> items,
        DataGridColumn? column = null)
    {
        var window = new Window
        {
            Width = 320,
            Height = 120,
        };
        window.SetThemeStyles();

        var grid = new DataGrid
        {
            AutoGenerateColumns = false,
            ItemsSource = items,
            SelectionMode = DataGridSelectionMode.Single,
            SelectionUnit = DataGridSelectionUnit.Cell,
            UseLogicalScrollable = true,
        };
        grid.ColumnsInternal.Add(column ?? CreateTextColumn());
        grid.ColumnsInternal.Add(new DataGridTextColumn
        {
            Header = "Second",
            Binding = new Binding(nameof(Item.Name)),
        });
        window.Content = grid;
        return (window, grid);
    }

    private static void BeginEdit(DataGrid grid, int rowIndex, int columnIndex)
    {
        int slot = grid.SlotFromRowIndex(rowIndex);
        grid.UpdateSelectionAndCurrency(
            columnIndex,
            slot,
            DataGridSelectionAction.SelectCurrent,
            scrollIntoView: false);
        grid.UpdateLayout();
        Assert.True(grid.BeginEdit());
        grid.UpdateLayout();
    }

    private static DataGridCell GetCell(DataGrid grid, int rowIndex, int columnIndex) =>
        grid.GetVisualDescendants()
            .OfType<DataGridCell>()
            .Single(cell => cell.OwningRow?.Index == rowIndex && cell.OwningColumn?.Index == columnIndex);

    private sealed class Item : INotifyPropertyChanged
    {
        private string _name;
        private bool _flag;

        public Item(string name)
        {
            _name = name;
        }

        public string Name
        {
            get => _name;
            set => SetField(ref _name, value);
        }

        public bool Flag
        {
            get => _flag;
            set => SetField(ref _flag, value);
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private void SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value))
            {
                return;
            }

            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
