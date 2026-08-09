// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.DataGridFormulas;
using Avalonia.Controls.DataGridHierarchical;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Data.Core;
using Avalonia.Headless.XUnit;
using Avalonia.VisualTree;
using Xunit;

namespace Avalonia.Controls.DataGridTests;

public class DataGridCellLifecycleTests
{
    static DataGridCellLifecycleTests()
    {
        Avalonia12TestCompat.EnsureDataValidator("ExceptionValidationPlugin");
    }

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
    public void Prepared_On_Recycled_Row_Observes_Content_For_The_New_Item()
    {
        ObservableCollection<Item> items = CreateItems(100);
        DataDependentLifecycleColumn column = new();
        (Window window, DataGrid grid) = CreateGrid(items, column);
        var preparedItems = new List<Item>();
        grid.CellPrepared += (_, e) =>
        {
            if (!ReferenceEquals(e.Column, column))
            {
                return;
            }

            Item item = Assert.IsType<Item>(e.Item);
            TextBlock content = Assert.IsType<TextBlock>(e.Cell.Content);
            Assert.Equal(item.Name, content.Text);
            Assert.False(e.Row.IsRecycled);
            preparedItems.Add(item);
        };

        window.Show();
        grid.UpdateLayout();
        preparedItems.Clear();

        grid.ScrollIntoView(items[60], column);
        grid.UpdateLayout();

        Assert.NotEmpty(preparedItems);
        Assert.All(preparedItems, item => Assert.Contains(item, items));
    }

    [AvaloniaFact]
    public void Placeholder_Replacement_Clears_Old_Assignment_Then_Prepares_Final_Content_Once()
    {
        var items = new ObservableCollection<EditableLifecycleItem>
        {
            new() { Name = "existing" },
        };
        (Window window, DataGrid grid) = CreateGrid(items, new DataGridTextColumn
        {
            Header = "Name",
            Binding = new Binding(nameof(EditableLifecycleItem.Name)),
        });
        grid.CanUserAddRows = true;
        grid.IsReadOnly = false;
        var events = new List<(string Kind, DataGridCellLifecycleEventArgs Args)>();
        grid.CellClearing += (_, e) => events.Add(("clearing", e));
        grid.CellPrepared += (_, e) => events.Add(("prepared", e));

        window.Show();
        grid.UpdateLayout();
        int placeholderIndex = grid.DataConnection.Count - 1;
        grid.ScrollIntoView(DataGridCollectionView.NewItemPlaceholder, grid.ColumnsInternal[0]);
        grid.UpdateSelectionAndCurrency(
            columnIndex: 0,
            slot: grid.SlotFromRowIndex(placeholderIndex),
            action: DataGridSelectionAction.SelectCurrent,
            scrollIntoView: false);
        grid.UpdateLayout();
        events.Clear();

        Assert.True(grid.BeginEdit());
        grid.UpdateLayout();

        int cellCount = grid.ColumnsInternal.Count;
        Assert.Equal(cellCount * 2, events.Count);
        Assert.All(events.Take(cellCount), entry =>
        {
            Assert.Equal("clearing", entry.Kind);
            Assert.Same(DataGridCollectionView.NewItemPlaceholder, entry.Args.RowDataContext);
            Assert.Same(DataGridCollectionView.NewItemPlaceholder, entry.Args.Item);
        });
        Assert.All(events.Skip(cellCount), entry =>
        {
            Assert.Equal("prepared", entry.Kind);
            Assert.IsType<EditableLifecycleItem>(entry.Args.Item);
            Assert.Same(entry.Args.Row.DataContext, entry.Args.RowDataContext);
            Assert.False(entry.Args.Row.IsPlaceholder);
            Assert.False(entry.Args.Row.IsRecycled);
            Assert.NotNull(entry.Args.Cell.Content);
        });
        Assert.Equal(
            events.Take(cellCount).Select(entry => entry.Args.Cell),
            events.Skip(cellCount).Select(entry => entry.Args.Cell));
    }

    [AvaloniaFact]
    public void DisplayMode_Change_Updates_Recycle_Pool_Without_False_Lifecycle_Events()
    {
        ObservableCollection<Item> items = CreateItems(100);
        DataGridTextColumn column = CreateTextColumn();
        (Window window, DataGrid grid) = CreateGrid(items, column);
        grid.RowHeight = 24;
        grid.Height = 120;
        grid.KeepRecycledContainersInVisualTree = true;
        grid.TrimRecycledContainers = false;

        window.Show();
        grid.UpdateLayout();
        // Contract the viewport without realizing a replacement range so the surplus live
        // rows enter the recycle pool and remain under the rows presenter.
        grid.Height = 48;
        grid.UpdateLayout();

        DataGridRow[] recycledRows = grid.GetVisualDescendants()
            .OfType<DataGridRow>()
            .Where(row => row.IsRecycled)
            .ToArray();
        Assert.NotEmpty(recycledRows);
        DataGridCell[] recycledCells = recycledRows
            .Select(row => row.Cells[column.Index])
            .ToArray();
        var clearing = new List<DataGridCellLifecycleEventArgs>();
        var prepared = new List<DataGridCellLifecycleEventArgs>();
        grid.CellClearing += (_, e) => clearing.Add(e);
        grid.CellPrepared += (_, e) => prepared.Add(e);

        column.DisplayMode = DataGridColumnDisplayMode.Drawn;
        grid.UpdateLayout();

        Assert.NotEmpty(clearing);
        Assert.Equal(clearing.Count, prepared.Count);
        Assert.All(clearing.Concat(prepared), e =>
        {
            Assert.True(e.Row.Slot >= 0);
            Assert.False(e.Row.IsRecycled);
        });
        Assert.DoesNotContain(clearing, e => recycledRows.Contains(e.Row));
        Assert.DoesNotContain(prepared, e => recycledRows.Contains(e.Row));
        Assert.All(recycledRows, row =>
            Assert.IsType<DataGridCustomDrawingCell>(row.Cells[column.Index]));
        Assert.All(recycledCells, oldCell =>
            Assert.DoesNotContain(oldCell, clearing.Select(e => e.Cell)));
    }

    [AvaloniaFact]
    public void Dynamic_Column_Add_And_Remove_Raise_One_Lifecycle_Event_Per_Realized_Cell()
    {
        ObservableCollection<Item> items = CreateItems(20);
        (Window window, DataGrid grid) = CreateGrid(items);
        var prepared = new List<DataGridCellLifecycleEventArgs>();
        var clearing = new List<DataGridCellLifecycleEventArgs>();
        grid.CellPrepared += (_, e) => prepared.Add(e);
        grid.CellClearing += (_, e) =>
        {
            Assert.Same(e.Column, e.Cell.OwningColumn);
            Assert.True(Enumerable.Range(0, e.Row.Cells.Count)
                .Any(index => ReferenceEquals(e.Row.Cells[index], e.Cell)));
            clearing.Add(e);
        };

        window.Show();
        grid.UpdateLayout();
        prepared.Clear();
        clearing.Clear();

        DataGridTextColumn dynamicColumn = new()
        {
            Header = "Dynamic",
            Binding = new Binding(nameof(Item.Name)),
        };
        grid.ColumnsInternal.Add(dynamicColumn);
        grid.UpdateLayout();

        DataGridCellLifecycleEventArgs[] added = prepared
            .Where(e => ReferenceEquals(e.Column, dynamicColumn))
            .ToArray();
        Assert.NotEmpty(added);
        Assert.Equal(added.Length, added.Select(e => e.Cell).Distinct().Count());
        Assert.All(added, e =>
        {
            Assert.Same(dynamicColumn, e.Cell.OwningColumn);
            Assert.Same(e.Row.DataContext, e.RowDataContext);
        });
        Assert.Empty(clearing);

        prepared.Clear();
        grid.ColumnsInternal.Remove(dynamicColumn);
        grid.UpdateLayout();

        DataGridCellLifecycleEventArgs[] removed = clearing
            .Where(e => ReferenceEquals(e.Column, dynamicColumn))
            .ToArray();
        Assert.Equal(added.Length, removed.Length);
        Assert.Equal(removed.Length, removed.Select(e => e.Cell).Distinct().Count());
        Assert.Equal(
            added.Select(e => e.Cell).OrderBy(cell => cell.GetHashCode()),
            removed.Select(e => e.Cell).OrderBy(cell => cell.GetHashCode()));
        Assert.Empty(prepared);
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

    [AvaloniaFact]
    public void Template_Column_Edit_Commit_Uses_The_Same_Value_Changed_Contract()
    {
        var item = new Item("old");
        var column = new DataGridTemplateColumn
        {
            Header = "Template name",
            CellTemplate = new FuncDataTemplate<Item>((_, _) =>
            {
                var text = new TextBlock();
                text.Bind(TextBlock.TextProperty, new Binding(nameof(Item.Name)));
                return text;
            }),
            CellEditingTemplate = new FuncDataTemplate<Item>((_, _) =>
            {
                var editor = new TextBox();
                editor.Bind(TextBox.TextProperty, new Binding(nameof(Item.Name))
                {
                    Mode = BindingMode.TwoWay,
                    UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged,
                });
                return editor;
            }),
        };
        DataGridColumnMetadata.SetValueAccessor(
            column,
            new DataGridColumnValueAccessor<Item, string>(
                static value => value.Name,
                static (value, text) => value.Name = text));
        (Window window, DataGrid grid) = CreateGrid(new[] { item }, column);
        var changes = new List<DataGridCellValueChangedEventArgs>();
        grid.CellValueChanged += (_, e) => changes.Add(e);

        window.Show();
        grid.UpdateLayout();
        BeginEdit(grid, 0, 0);
        TextBox editor = Assert.IsType<TextBox>(GetCell(grid, 0, 0).Content);
        editor.Text = "new";
        BindingOperations.GetBindingExpressionBase(editor, TextBox.TextProperty)?.UpdateSource();

        Assert.True(grid.CommitEdit());
        grid.UpdateLayout();

        DataGridCellValueChangedEventArgs change = Assert.Single(changes);
        Assert.Same(column, change.Column);
        Assert.Same(item, change.Item);
        Assert.Equal("old", change.OldValue);
        Assert.Equal("new", change.NewValue);
        Assert.Equal("new", item.Name);
    }

    [AvaloniaFact]
    public void Validation_Failure_Does_Not_Raise_Value_Changed()
    {
        var item = new ValidatedItem("valid");
        var column = new DataGridTextColumn
        {
            Header = "Validated name",
            Binding = new Binding(nameof(ValidatedItem.Name))
            {
                Mode = BindingMode.TwoWay,
                UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged,
            },
        };
        DataGridColumnMetadata.SetValueAccessor(
            column,
            new DataGridColumnValueAccessor<ValidatedItem, string>(
                static value => value.Name,
                static (value, text) => value.Name = text));
        (Window window, DataGrid grid) = CreateGrid(new[] { item }, column);
        var changes = new List<DataGridCellValueChangedEventArgs>();
        grid.CellValueChanged += (_, e) => changes.Add(e);

        window.Show();
        grid.UpdateLayout();
        BeginEdit(grid, 0, 0);
        TextBox editor = Assert.IsType<TextBox>(GetCell(grid, 0, 0).Content);
        editor.Text = string.Empty;
        BindingOperations.GetBindingExpressionBase(editor, TextBox.TextProperty)?.UpdateSource();

        Assert.False(grid.CommitEdit());
        grid.UpdateLayout();

        Assert.Empty(changes);
        Assert.Equal("valid", item.Name);
        Assert.False(GetCell(grid, 0, 0).IsValid);
    }

    [AvaloniaFact]
    public void Formula_Recalculation_Does_Not_Raise_Grid_Commit_Value_Changed()
    {
        var item = new FormulaItem(10d);
        var builder = DataGridColumnDefinitionBuilder.For<FormulaItem>();
        var amountProperty = new ClrPropertyInfo(
            nameof(FormulaItem.Amount),
            target => ((FormulaItem)target).Amount,
            (target, value) => ((FormulaItem)target).Amount = value is double number ? number : 0d,
            typeof(double));
        DataGridColumnDefinition amountDefinition = builder.Numeric(
            header: "Amount",
            property: amountProperty,
            getter: static row => row.Amount,
            setter: static (row, value) => row.Amount = value,
            configure: static definition => definition.ColumnKey = "Amount");
        DataGridFormulaColumnDefinition formulaDefinition = builder.Formula(
            header: "Calculated",
            formula: "=[@Amount]*2",
            formulaName: "Calculated",
            configure: static definition => definition.ColumnKey = "Calculated");
        var grid = new DataGrid
        {
            AutoGenerateColumns = false,
            ItemsSource = new[] { item },
            ColumnDefinitionsSource = new ObservableCollection<DataGridColumnDefinition>
            {
                amountDefinition,
                formulaDefinition,
            },
        };
        var window = new Window
        {
            Width = 320,
            Height = 120,
            Content = grid,
        };
        window.SetThemeStyles();
        var changes = new List<DataGridCellValueChangedEventArgs>();
        grid.CellValueChanged += (_, e) => changes.Add(e);

        window.Show();
        grid.UpdateLayout();
        var formulaModel = Assert.IsType<DataGridFormulaModel>(grid.FormulaModel);
        formulaModel.Recalculate();
        Assert.Equal(20d, formulaModel.Evaluate(item, formulaDefinition));
        Assert.Contains(grid.ColumnsInternal, static column => column is DataGridFormulaTextColumn);

        Assert.True(formulaModel.TrySetCellFormula(
            item,
            formulaDefinition,
            "=[@Amount]*3",
            out string? error));
        Assert.True(string.IsNullOrWhiteSpace(error));
        formulaModel.Recalculate();
        grid.UpdateLayout();

        Assert.Equal(30d, formulaModel.Evaluate(item, formulaDefinition));
        Assert.Empty(changes);
    }

    [AvaloniaFact]
    public void External_Undo_And_Redo_After_Commit_Do_Not_Duplicate_Value_Changed()
    {
        var item = new Item("old");
        DataGridTextColumn column = CreateTextColumn();
        DataGridColumnMetadata.SetValueAccessor(
            column,
            new DataGridColumnValueAccessor<Item, string>(
                static value => value.Name,
                static (value, text) => value.Name = text));
        (Window window, DataGrid grid) = CreateGrid(new[] { item }, column);
        var changes = new List<DataGridCellValueChangedEventArgs>();
        grid.CellValueChanged += (_, e) => changes.Add(e);

        window.Show();
        grid.UpdateLayout();
        BeginEdit(grid, 0, 0);
        Assert.IsType<TextBox>(GetCell(grid, 0, 0).Content).Text = "new";
        Assert.True(grid.CommitEdit());
        Assert.Single(changes);
        changes.Clear();

        // A host undo manager applies model changes outside a DataGrid edit transaction.
        // Those notifications must not duplicate the one commit event raised above.
        item.Name = "old"; // Undo.
        item.Name = "new"; // Redo.
        grid.UpdateLayout();

        Assert.Equal("new", item.Name);
        Assert.Empty(changes);
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

    private sealed class DataDependentLifecycleColumn : DataGridTextColumn
    {
        protected override Control GenerateElement(DataGridCell cell, object dataItem)
        {
            return new TextBlock { Text = ((Item)dataItem).Name };
        }
    }

    private sealed class EditableLifecycleItem
    {
        public EditableLifecycleItem()
        {
        }

        public string? Name { get; set; }
    }

    private sealed class ValidatedItem
    {
        private string _name;

        public ValidatedItem(string name)
        {
            _name = name;
        }

        public string Name
        {
            get => _name;
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                {
                    throw new DataValidationException("Name is required.");
                }

                _name = value;
            }
        }
    }

    private sealed class FormulaItem
    {
        public FormulaItem(double amount)
        {
            Amount = amount;
        }

        public double Amount { get; set; }
    }
}
