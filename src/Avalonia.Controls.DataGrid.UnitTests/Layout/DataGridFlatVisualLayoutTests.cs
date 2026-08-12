// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Xunit;

namespace Avalonia.Controls.DataGridTests.Layout;

public class DataGridFlatVisualLayoutTests
{
    [AvaloniaTheory]
    [InlineData(DataGridTheme.SimpleFlat)]
    [InlineData(DataGridTheme.FluentFlat)]
    public void Flat_Theme_Promotes_Realized_Cells_To_Rows_Presenter(DataGridTheme theme)
    {
        (Window window, DataGrid grid) = CreateGrid(theme, useFlatTheme: true);

        try
        {
            window.Show();
            PumpLayout(grid);

            DataGridRowsPresenter presenter = GetRowsPresenter(grid);
            DataGridRow[] rows = presenter.Children.OfType<DataGridRow>().ToArray();
            DataGridCell[] cells = presenter.GetVisualChildren().OfType<DataGridCell>().ToArray();

            Assert.Equal(DataGridVisualLayoutMode.Flat, grid.VisualLayoutMode);
            Assert.NotEmpty(rows);
            Assert.NotEmpty(cells);
            Assert.Equal(cells.Length, presenter.FlatRealizedCellCount);
            Assert.All(cells, cell => Assert.Same(presenter, cell.GetVisualParent()));
            Assert.All(cells, cell => Assert.Same(cell.OwningRow, cell.Parent));
            Assert.All(cells, cell => Assert.Same(cell.OwningRow.DataContext, cell.DataContext));
            Assert.All(cells, cell => Assert.True(cell.IsSet(StyledElement.DataContextProperty)));
            Assert.All(rows, row => Assert.Empty(row.GetVisualDescendants().OfType<DataGridCellsPresenter>()));
            foreach (IGrouping<DataGridRow, DataGridCell> rowCells in cells.GroupBy(cell => cell.OwningRow))
            {
                double top = rowCells.First().Bounds.Y;
                Assert.All(rowCells, cell => Assert.Equal(top, cell.Bounds.Y, precision: 3));
                Assert.All(rowCells, cell => Assert.Equal(32, cell.Bounds.Height, precision: 3));
            }
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaTheory]
    [InlineData(DataGridTheme.SimpleFlat)]
    [InlineData(DataGridTheme.FluentFlat)]
    public void Virtualized_Layout_Uses_One_Surface_And_No_Display_Cell_Controls(DataGridTheme theme)
    {
        (Window window, DataGrid grid) = CreateGrid(theme, useFlatTheme: true);
        grid.VisualLayoutMode = DataGridVisualLayoutMode.Virtualized;

        try
        {
            window.Show();
            PumpLayout(grid);

            DataGridRowsPresenter presenter = GetRowsPresenter(grid);
            DataGridRow[] rows = presenter.Children.OfType<DataGridRow>().ToArray();

            Assert.NotEmpty(rows);
            Assert.Equal(1, presenter.VirtualSurfaceCount);
            Assert.Equal(0, presenter.FlatRealizedCellCount);
            Assert.Empty(presenter.GetVisualDescendants().OfType<DataGridCell>());
            Assert.All(rows, row => Assert.Equal(0, row.Cells.Count));
            Assert.Equal(
                100 * DataGridRow.GetFlatDesiredHeight(grid, grid.RowHeight),
                presenter.Extent.Height,
                precision: 3);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void Virtualized_Layout_Materializes_Only_The_Active_Editor_Overlay()
    {
        (Window window, DataGrid grid) = CreateGrid(DataGridTheme.SimpleFlat, useFlatTheme: true);
        grid.VisualLayoutMode = DataGridVisualLayoutMode.Virtualized;

        try
        {
            window.Show();
            PumpLayout(grid);

            DataGridRowsPresenter presenter = GetRowsPresenter(grid);
            int slot = grid.SlotFromRowIndex(0);
            DataGridColumn column = grid.ColumnsInternal[1];
            Assert.True(grid.UpdateSelectionAndCurrency(
                column.Index,
                slot,
                DataGridSelectionAction.SelectCurrent,
                scrollIntoView: false));
            PumpLayout(grid);

            Assert.True(grid.BeginEdit());
            PumpLayout(grid);

            DataGridCell editorCell = Assert.Single(
                presenter.GetVisualChildren().OfType<DataGridCell>());
            Assert.Equal(column.Index, editorCell.OwningColumn.Index);
            Assert.IsType<TextBox>(editorCell.Content);
            Assert.True(grid.CommitEdit());
            PumpLayout(grid);

            Assert.Empty(presenter.GetVisualChildren().OfType<DataGridCell>());
            Assert.Equal(1, presenter.VirtualSurfaceCount);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void Virtualized_Layout_Scrolls_Without_Creating_Display_Cells()
    {
        (Window window, DataGrid grid) = CreateGrid(
            DataGridTheme.SimpleFlat,
            useFlatTheme: true,
            itemCount: 500);
        grid.VisualLayoutMode = DataGridVisualLayoutMode.Virtualized;

        try
        {
            window.Show();
            PumpLayout(grid);

            DataGridRowsPresenter presenter = GetRowsPresenter(grid);
            grid.ScrollIntoView(grid.ItemsSource!.Cast<Item>().ElementAt(450), grid.ColumnsInternal[0]);
            PumpLayout(grid);

            Assert.Contains(presenter.Children.OfType<DataGridRow>(), row => row.Index >= 450);
            Assert.Empty(presenter.GetVisualDescendants().OfType<DataGridCell>());
            Assert.Equal(1, presenter.VirtualSurfaceCount);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void Virtualized_Layout_Recycles_Default_Rows_Without_Null_DataContext_Transition()
    {
        (Window window, DataGrid grid) = CreateGrid(
            DataGridTheme.SimpleFlat,
            useFlatTheme: true,
            itemCount: 500);
        grid.VisualLayoutMode = DataGridVisualLayoutMode.Virtualized;

        try
        {
            window.Show();
            PumpLayout(grid);

            DataGridRowsPresenter presenter = GetRowsPresenter(grid);
            DataGridRow[] rows = presenter.Children.OfType<DataGridRow>().ToArray();
            Assert.NotEmpty(rows);
            int nullDataContextTransitions = 0;
            foreach (DataGridRow row in rows)
            {
                row.PropertyChanged += (_, change) =>
                {
                    if (change.Property == StyledElement.DataContextProperty &&
                        change.GetNewValue<object?>() is null)
                    {
                        nullDataContextTransitions++;
                    }
                };
            }

            double rowHeight = DataGridRow.GetFlatDesiredHeight(grid, grid.RowHeight);
            presenter.Offset = new Vector(0, 400 * rowHeight);
            PumpLayout(grid);

            DataGridRow[] jumpedRows = presenter.Children.OfType<DataGridRow>().ToArray();
            Assert.Equal(rows.Length, jumpedRows.Length);
            Assert.True(rows.SequenceEqual(jumpedRows));
            Assert.Equal(0, nullDataContextTransitions);
            Assert.Contains(jumpedRows, row => row.Index >= 400);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void Virtualized_Layout_Retargets_Handler_Free_Default_Rows_In_Place()
    {
        (Window window, DataGrid grid) = CreateGrid(
            DataGridTheme.SimpleFlat,
            useFlatTheme: true,
            itemCount: 500);
        grid.VisualLayoutMode = DataGridVisualLayoutMode.Virtualized;

        try
        {
            window.Show();
            PumpLayout(grid);

            DataGridRowsPresenter presenter = GetRowsPresenter(grid);
            long retargetedBeforeJump = grid.DisplayData.RetargetedRowCount;
            double rowHeight = DataGridRow.GetFlatDesiredHeight(grid, grid.RowHeight);
            presenter.Offset = new Vector(0, 400 * rowHeight);
            PumpLayout(grid);

            Assert.True(grid.DisplayData.RetargetedRowCount > retargetedBeforeJump);
            Assert.Contains(presenter.Children.OfType<DataGridRow>(), row => row.Index >= 400);
            Assert.All(presenter.Children.OfType<DataGridRow>(), row => Assert.Equal(0, row.Cells.Count));
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void Virtualized_Layout_Recycled_Rows_Restore_State_And_Raise_Lifecycle_Events()
    {
        (Window window, DataGrid grid) = CreateGrid(
            DataGridTheme.SimpleFlat,
            useFlatTheme: true,
            itemCount: 500);
        grid.VisualLayoutMode = DataGridVisualLayoutMode.Virtualized;
        int loadingRows = 0;
        int unloadingRows = 0;
        grid.LoadingRow += (_, args) =>
        {
            loadingRows++;
            Assert.Same(args.Row.DataContext, grid.ItemsSource!.Cast<Item>().ElementAt(args.Row.Index));
            Assert.False(args.Row.IsSelected);
            Assert.True(args.Row.IsValid);
            Assert.Equal(DataGridValidationSeverity.None, args.Row.ValidationSeverity);
        };
        grid.UnloadingRow += (_, _) => unloadingRows++;

        try
        {
            window.Show();
            PumpLayout(grid);

            DataGridRowsPresenter presenter = GetRowsPresenter(grid);
            DataGridRow recycledRow = presenter.Children.OfType<DataGridRow>().First();
            recycledRow.IsSelected = true;
            recycledRow.IsValid = false;
            recycledRow.ValidationSeverity = DataGridValidationSeverity.Error;
            int loadingRowsBeforeJump = loadingRows;
            long retargetedBeforeJump = grid.DisplayData.RetargetedRowCount;

            double rowHeight = DataGridRow.GetFlatDesiredHeight(grid, grid.RowHeight);
            presenter.Offset = new Vector(0, 400 * rowHeight);
            PumpLayout(grid);

            DataGridRow[] jumpedRows = presenter.Children.OfType<DataGridRow>().ToArray();
            Assert.True(loadingRows > loadingRowsBeforeJump);
            Assert.True(unloadingRows > 0);
            Assert.Equal(retargetedBeforeJump, grid.DisplayData.RetargetedRowCount);
            Assert.Contains(recycledRow, jumpedRows);
            Assert.All(jumpedRows, row =>
            {
                Assert.False(row.IsSelected);
                Assert.True(row.IsValid);
                Assert.Equal(DataGridValidationSeverity.None, row.ValidationSeverity);
                Assert.False(row.IsPlaceholder);
                Assert.Equal(0, row.Cells.Count);
            });
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void Virtualized_Layout_Falls_Back_To_Flat_Retained_For_Unsupported_Interactive_Columns()
    {
        (Window window, DataGrid grid) = CreateGrid(DataGridTheme.SimpleFlat, useFlatTheme: true);
        grid.Columns.Add(new DataGridButtonColumn
        {
            Header = "Interactive",
            Width = new DataGridLength(90),
            Content = "Open",
        });
        grid.VisualLayoutMode = DataGridVisualLayoutMode.Virtualized;

        try
        {
            window.Show();
            PumpLayout(grid);

            DataGridRowsPresenter presenter = GetRowsPresenter(grid);
            Assert.True(grid.UsesVirtualCellSurfaceFallback);
            Assert.Equal(0, presenter.VirtualSurfaceCount);
            Assert.NotEmpty(presenter.GetVisualDescendants().OfType<DataGridCell>());
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void Virtualized_Layout_Draws_Typed_CheckBox_Column_And_Materializes_It_Only_For_Editing()
    {
        (Window window, DataGrid grid) = CreateGrid(DataGridTheme.SimpleFlat, useFlatTheme: true);
        var checkBoxColumn = new DataGridCheckBoxColumn
        {
            Header = "Active",
            Width = new DataGridLength(90),
            Binding = new Binding(nameof(Item.IsActive)),
            IsThreeState = true,
        };
        DataGridColumnMetadata.SetValueAccessor(
            checkBoxColumn,
            new DataGridColumnValueAccessor<Item, bool?>(item => item.IsActive));
        grid.Columns.Add(checkBoxColumn);
        grid.VisualLayoutMode = DataGridVisualLayoutMode.Virtualized;

        try
        {
            window.Show();
            PumpLayout(grid);

            DataGridRowsPresenter presenter = GetRowsPresenter(grid);
            Assert.False(grid.UsesVirtualCellSurfaceFallback);
            Assert.Equal(1, presenter.VirtualSurfaceCount);
            Assert.Empty(presenter.GetVisualDescendants().OfType<DataGridCell>());
            Assert.All(presenter.Children.OfType<DataGridRow>(), row => Assert.Equal(0, row.Cells.Count));

            int valueChangeCount = presenter.VirtualValueChangeCount;
            Item firstItem = grid.ItemsSource!.Cast<Item>().First();
            firstItem.IsActive = null;
            Dispatcher.UIThread.RunJobs();
            Assert.Equal(valueChangeCount + 1, presenter.VirtualValueChangeCount);

            int slot = grid.SlotFromRowIndex(0);
            Assert.True(grid.UpdateSelectionAndCurrency(
                checkBoxColumn.Index,
                slot,
                DataGridSelectionAction.SelectCurrent,
                scrollIntoView: false));
            var keyArgs = new KeyEventArgs
            {
                RoutedEvent = InputElement.KeyDownEvent,
                Route = InputElement.KeyDownEvent.RoutingStrategies,
                Key = Key.Space,
                Source = grid,
                KeyDeviceType = KeyDeviceType.Keyboard,
            };
            grid.RaiseEvent(keyArgs);
            PumpLayout(grid);

            Assert.True(keyArgs.Handled);
            DataGridCell editorCell = Assert.Single(
                presenter.GetVisualChildren().OfType<DataGridCell>());
            Assert.Same(checkBoxColumn, editorCell.OwningColumn);
            Assert.IsType<CheckBox>(editorCell.Content);

            Assert.True(grid.CommitEdit());
            PumpLayout(grid);
            Assert.Empty(presenter.GetVisualChildren().OfType<DataGridCell>());
            Assert.Equal(1, presenter.VirtualSurfaceCount);

            grid.ScrollIntoView(grid.ItemsSource.Cast<Item>().ElementAt(90), checkBoxColumn);
            PumpLayout(grid);
            Assert.Empty(presenter.GetVisualDescendants().OfType<DataGridCell>());
            Assert.All(presenter.Children.OfType<DataGridRow>(), row => Assert.Equal(0, row.Cells.Count));
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void Virtualized_Layout_Preserves_Cell_Lifecycle_Events_Through_Retained_Fallback()
    {
        (Window window, DataGrid grid) = CreateGrid(DataGridTheme.SimpleFlat, useFlatTheme: true);
        grid.VisualLayoutMode = DataGridVisualLayoutMode.Virtualized;
        int prepared = 0;
        grid.CellPrepared += (_, _) => prepared++;

        try
        {
            window.Show();
            PumpLayout(grid);

            DataGridRowsPresenter presenter = GetRowsPresenter(grid);
            Assert.True(grid.UsesVirtualCellSurfaceFallback);
            Assert.True(prepared > 0);
            Assert.NotEmpty(presenter.GetVisualDescendants().OfType<DataGridCell>());
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void Virtualized_Surface_Pointer_Hit_Selects_The_Cell()
    {
        (Window window, DataGrid grid) = CreateGrid(DataGridTheme.SimpleFlat, useFlatTheme: true);
        grid.VisualLayoutMode = DataGridVisualLayoutMode.Virtualized;

        try
        {
            window.Show();
            PumpLayout(grid);

            DataGridRowsPresenter presenter = GetRowsPresenter(grid);
            DataGridVirtualCellSurface surface = Assert.Single(
                presenter.GetVisualChildren().OfType<DataGridVirtualCellSurface>());
            DataGridRow row = presenter.Children.OfType<DataGridRow>().First();
            Assert.True(presenter.TryGetVirtualCellBounds(row, grid.ColumnsInternal[0], out Rect bounds));

            var pointer = new Pointer(Pointer.GetNextFreeId(), PointerType.Mouse, isPrimary: true);
            var properties = new PointerPointProperties(
                RawInputModifiers.LeftMouseButton,
                PointerUpdateKind.LeftButtonPressed);
            var args = new PointerPressedEventArgs(
                surface,
                pointer,
                surface,
                bounds.Center,
                0,
                properties,
                KeyModifiers.None);
            surface.RaiseEvent(args);
            PumpLayout(grid);

            Assert.True(grid.CurrentCell.IsValid);
            Assert.Same(row.DataContext, grid.CurrentCell.Item);
            Assert.Same(grid.ColumnsInternal[0], grid.CurrentCell.Column);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void Virtualized_Surface_Tracks_Visible_Item_Value_Changes()
    {
        (Window window, DataGrid grid) = CreateGrid(DataGridTheme.SimpleFlat, useFlatTheme: true);
        grid.VisualLayoutMode = DataGridVisualLayoutMode.Virtualized;

        try
        {
            window.Show();
            PumpLayout(grid);

            DataGridRowsPresenter presenter = GetRowsPresenter(grid);
            Assert.True(presenter.VirtualTrackedValueNotifierCount > 0);
            int changeCount = presenter.VirtualValueChangeCount;

            Item item = grid.ItemsSource!.Cast<Item>().First();
            item.Name = "Updated";
            Dispatcher.UIThread.RunJobs();

            Assert.Equal(changeCount + 1, presenter.VirtualValueChangeCount);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void Virtualized_Surface_Skips_Value_Tracking_When_All_Columns_Opt_Out()
    {
        (Window window, DataGrid grid) = CreateGrid(DataGridTheme.SimpleFlat, useFlatTheme: true);
        foreach (DataGridTextColumn column in grid.ColumnsInternal.OfType<DataGridTextColumn>())
        {
            column.TrackDirectTextValueChanges = false;
        }
        grid.VisualLayoutMode = DataGridVisualLayoutMode.Virtualized;

        try
        {
            window.Show();
            PumpLayout(grid);

            DataGridRowsPresenter presenter = GetRowsPresenter(grid);
            Assert.Equal(0, presenter.VirtualTrackedValueNotifierCount);
            int changeCount = presenter.VirtualValueChangeCount;

            Item item = grid.ItemsSource!.Cast<Item>().First();
            item.Name = "Updated";
            Dispatcher.UIThread.RunJobs();

            Assert.Equal(changeCount, presenter.VirtualValueChangeCount);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void Virtualized_Surface_Updates_Value_Tracking_When_Column_Policy_Changes()
    {
        (Window window, DataGrid grid) = CreateGrid(DataGridTheme.SimpleFlat, useFlatTheme: true);
        grid.VisualLayoutMode = DataGridVisualLayoutMode.Virtualized;

        try
        {
            window.Show();
            PumpLayout(grid);

            DataGridRowsPresenter presenter = GetRowsPresenter(grid);
            Assert.True(presenter.VirtualTrackedValueNotifierCount > 0);

            foreach (DataGridTextColumn column in grid.ColumnsInternal.OfType<DataGridTextColumn>())
            {
                column.TrackDirectTextValueChanges = false;
            }
            PumpLayout(grid);
            Assert.Equal(0, presenter.VirtualTrackedValueNotifierCount);

            DataGridTextColumn firstColumn = grid.ColumnsInternal.OfType<DataGridTextColumn>().First();
            firstColumn.TrackDirectTextValueChanges = true;
            PumpLayout(grid);
            Assert.True(presenter.VirtualTrackedValueNotifierCount > 0);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void Virtualized_Layout_Falls_Back_When_A_Typed_Accessor_Is_Not_Available()
    {
        (Window window, DataGrid grid) = CreateGrid(DataGridTheme.SimpleFlat, useFlatTheme: true);
        DataGridColumnMetadata.ClearValueAccessor(grid.ColumnsInternal[0]);
        grid.VisualLayoutMode = DataGridVisualLayoutMode.Virtualized;

        try
        {
            window.Show();
            PumpLayout(grid);

            Assert.True(grid.UsesVirtualCellSurfaceFallback);
            Assert.NotEmpty(GetRowsPresenter(grid).GetVisualDescendants().OfType<DataGridCell>());
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void Virtualized_Layout_Falls_Back_For_Auto_Cell_Sizing()
    {
        (Window window, DataGrid grid) = CreateGrid(DataGridTheme.SimpleFlat, useFlatTheme: true);
        grid.ColumnsInternal[0].Width = DataGridLength.SizeToCells;
        grid.VisualLayoutMode = DataGridVisualLayoutMode.Virtualized;

        try
        {
            window.Show();
            PumpLayout(grid);

            Assert.True(grid.UsesVirtualCellSurfaceFallback);
            Assert.NotEmpty(GetRowsPresenter(grid).GetVisualDescendants().OfType<DataGridCell>());
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void Virtualized_Layout_Transitions_Between_Surface_And_Retained_Fallback()
    {
        (Window window, DataGrid grid) = CreateGrid(DataGridTheme.SimpleFlat, useFlatTheme: true);
        grid.VisualLayoutMode = DataGridVisualLayoutMode.Virtualized;

        try
        {
            window.Show();
            PumpLayout(grid);
            DataGridRowsPresenter presenter = GetRowsPresenter(grid);
            Assert.Equal(1, presenter.VirtualSurfaceCount);

            grid.ColumnsInternal[0].Width = DataGridLength.SizeToCells;
            PumpLayout(grid);
            Assert.True(grid.UsesVirtualCellSurfaceFallback);
            Assert.NotEmpty(presenter.GetVisualDescendants().OfType<DataGridCell>());

            grid.ColumnsInternal[0].Width = new DataGridLength(80);
            PumpLayout(grid);
            Assert.False(grid.UsesVirtualCellSurfaceFallback);
            Assert.Equal(1, presenter.VirtualSurfaceCount);
            Assert.Empty(presenter.GetVisualDescendants().OfType<DataGridCell>());
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void Virtualized_Layout_Uses_Retained_Fallback_For_Custom_Grid_Cell_Theme()
    {
        (Window window, DataGrid grid) = CreateGrid(DataGridTheme.SimpleFlat, useFlatTheme: true);
        grid.VisualLayoutMode = DataGridVisualLayoutMode.Virtualized;

        try
        {
            window.Show();
            PumpLayout(grid);
            DataGridRowsPresenter presenter = GetRowsPresenter(grid);
            Assert.Equal(1, presenter.VirtualSurfaceCount);

            grid.CellTheme = new ControlTheme(typeof(DataGridCell));
            PumpLayout(grid);

            Assert.True(grid.UsesVirtualCellSurfaceFallback);
            Assert.NotEmpty(presenter.GetVisualDescendants().OfType<DataGridCell>());
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void Nested_Theme_Preserves_Per_Row_Cells_Presenter()
    {
        // Loading Flat.xaml is inert until the keyed DataGridFlatTheme is selected.
        (Window window, DataGrid grid) = CreateGrid(DataGridTheme.SimpleFlat, useFlatTheme: false);

        try
        {
            window.Show();
            PumpLayout(grid);

            DataGridRowsPresenter presenter = GetRowsPresenter(grid);
            DataGridRow[] rows = presenter.Children.OfType<DataGridRow>().ToArray();
            DataGridCell[] directCells = presenter.GetVisualChildren().OfType<DataGridCell>().ToArray();

            Assert.Equal(DataGridVisualLayoutMode.Nested, grid.VisualLayoutMode);
            Assert.NotEmpty(rows);
            Assert.Empty(directCells);
            Assert.Equal(0, presenter.FlatRealizedCellCount);
            Assert.All(rows, row => Assert.NotEmpty(row.GetVisualDescendants().OfType<DataGridCellsPresenter>()));
            Assert.All(rows, row =>
            {
                for (int columnIndex = 0; columnIndex < row.Cells.Count; columnIndex++)
                {
                    Assert.IsType<DataGridCellsPresenter>(row.Cells[columnIndex].GetVisualParent());
                }
            });
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void Flat_Layout_Recycles_Direct_Cells_When_Scrolling()
    {
        (Window window, DataGrid grid) = CreateGrid(DataGridTheme.SimpleFlat, useFlatTheme: true, itemCount: 500);

        try
        {
            window.Show();
            PumpLayout(grid);

            DataGridRowsPresenter presenter = GetRowsPresenter(grid);
            DataGridCell[] initialCells = presenter.GetVisualChildren().OfType<DataGridCell>().ToArray();
            Assert.NotEmpty(initialCells);

            grid.ScrollIntoView(grid.ItemsSource!.Cast<Item>().ElementAt(450), grid.ColumnsInternal[0]);
            PumpLayout(grid);

            DataGridCell[] scrolledCells = presenter.GetVisualChildren().OfType<DataGridCell>().ToArray();
            Assert.NotEmpty(scrolledCells);
            Assert.Equal(scrolledCells.Length, presenter.FlatRealizedCellCount);
            Assert.All(scrolledCells, cell => Assert.Same(presenter, cell.GetVisualParent()));
            Assert.All(scrolledCells, cell => Assert.Same(cell.OwningRow, cell.Parent));
            Assert.All(scrolledCells, cell => Assert.Same(cell.OwningRow.DataContext, cell.DataContext));
            Assert.All(scrolledCells, cell => Assert.True(cell.IsSet(StyledElement.DataContextProperty)));
            Assert.Contains(presenter.Children.OfType<DataGridRow>(), row => row.Index >= 450);
            Assert.True(scrolledCells.Length <= initialCells.Length + 6,
                $"Expected recycling to keep the direct-cell set bounded. Initial={initialCells.Length}, Scrolled={scrolledCells.Length}");
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void Flat_Layout_Preserves_Cell_Editing()
    {
        (Window window, DataGrid grid) = CreateGrid(DataGridTheme.SimpleFlat, useFlatTheme: true);

        try
        {
            window.Show();
            PumpLayout(grid);

            DataGridRowsPresenter presenter = GetRowsPresenter(grid);
            int slot = grid.SlotFromRowIndex(0);
            DataGridColumn column = grid.ColumnsInternal[1];
            Assert.True(grid.UpdateSelectionAndCurrency(
                column.Index,
                slot,
                DataGridSelectionAction.SelectCurrent,
                scrollIntoView: false));
            PumpLayout(grid);

            Assert.True(grid.BeginEdit());
            PumpLayout(grid);

            DataGridCell editedCell = Assert.Single(
                presenter.GetVisualChildren().OfType<DataGridCell>(),
                cell => cell.OwningRow.Index == 0 && cell.OwningColumn.Index == column.Index);
            Assert.Same(presenter, editedCell.GetVisualParent());
            Assert.IsType<TextBox>(editedCell.Content);
            Assert.True(grid.CommitEdit());
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void Flat_Layout_Uses_Equivalent_Row_And_Cell_Height_With_Horizontal_Grid_Lines()
    {
        (Window window, DataGrid grid) = CreateGrid(DataGridTheme.SimpleFlat, useFlatTheme: true);
        grid.GridLinesVisibility = DataGridGridLinesVisibility.Horizontal;

        try
        {
            window.Show();
            PumpLayout(grid);

            DataGridRowsPresenter presenter = GetRowsPresenter(grid);
            Assert.All(
                presenter.Children.OfType<DataGridRow>(),
                row => Assert.Equal(33, row.Bounds.Height, precision: 3));
            Assert.All(
                presenter.GetVisualChildren().OfType<DataGridCell>(),
                cell => Assert.Equal(32, cell.Bounds.Height, precision: 3));

            grid.GridLinesVisibility = DataGridGridLinesVisibility.None;
            PumpLayout(grid);

            Assert.All(
                presenter.Children.OfType<DataGridRow>(),
                row => Assert.Equal(32, row.Bounds.Height, precision: 3));
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void Flat_Layout_Shares_Drawn_Text_Cache_And_Restores_Cell_Settings_On_Reset()
    {
        (Window window, DataGrid grid) = CreateGrid(
            DataGridTheme.SimpleFlat,
            useFlatTheme: true,
            useDrawnText: true);

        try
        {
            window.Show();
            PumpLayout(grid);

            DataGridRowsPresenter presenter = GetRowsPresenter(grid);
            DataGridCustomDrawingCell[] cells = presenter.GetVisualChildren()
                .OfType<DataGridCustomDrawingCell>()
                .ToArray();
            Assert.NotEmpty(cells);
            DataGridCustomDrawingTextLayoutCache cache = Assert.IsType<DataGridCustomDrawingTextLayoutCache>(
                cells[0].SharedTextLayoutCache);
            Assert.All(cells, cell =>
            {
                Assert.Equal(DataGridCustomDrawingTextLayoutCacheMode.Shared, cell.TextLayoutCacheMode);
                Assert.Same(cache, cell.SharedTextLayoutCache);
            });

            presenter.ResetFlatVisualLayout();

            Assert.All(cells, cell =>
            {
                Assert.Equal(DataGridCustomDrawingTextLayoutCacheMode.Shared, cell.TextLayoutCacheMode);
                Assert.NotNull(cell.SharedTextLayoutCache);
                Assert.NotSame(cache, cell.SharedTextLayoutCache);
            });
            Assert.All(
                cells.GroupBy(cell => cell.OwningColumn),
                columnCells => Assert.Single(columnCells.Select(cell => cell.SharedTextLayoutCache).Distinct()));
        }
        finally
        {
            window.Close();
        }
    }

    private static (Window Window, DataGrid Grid) CreateGrid(
        DataGridTheme theme,
        bool useFlatTheme,
        int itemCount = 100,
        bool useDrawnText = false)
    {
        var items = Enumerable.Range(0, itemCount)
            .Select(index => new Item(index, $"Item {index}", index * 1.5))
            .ToList();
        var grid = new DataGrid
        {
            Width = 420,
            Height = 220,
            AutoGenerateColumns = false,
            HeadersVisibility = DataGridHeadersVisibility.Column,
            ItemsSource = items,
            RowHeight = 32,
            UseLogicalScrollable = true,
        };
        var idColumn = new DataGridTextColumn
        {
            Header = "Id",
            Width = new DataGridLength(80),
            Binding = new Binding(nameof(Item.Id)),
            DisplayMode = useDrawnText ? DataGridColumnDisplayMode.Drawn : DataGridColumnDisplayMode.Retained,
        };
        DataGridColumnMetadata.SetValueAccessor(
            idColumn,
            new DataGridColumnValueAccessor<Item, int>(item => item.Id));
        grid.Columns.Add(idColumn);
        var nameColumn = new DataGridTextColumn
        {
            Header = "Name",
            Width = new DataGridLength(180),
            Binding = new Binding(nameof(Item.Name)),
            DisplayMode = useDrawnText ? DataGridColumnDisplayMode.Drawn : DataGridColumnDisplayMode.Retained,
        };
        DataGridColumnMetadata.SetValueAccessor(
            nameColumn,
            new DataGridColumnValueAccessor<Item, string>(item => item.Name));
        grid.Columns.Add(nameColumn);
        var valueColumn = new DataGridTextColumn
        {
            Header = "Value",
            Width = new DataGridLength(120),
            Binding = new Binding(nameof(Item.Value)),
            DisplayMode = useDrawnText ? DataGridColumnDisplayMode.Drawn : DataGridColumnDisplayMode.Retained,
        };
        DataGridColumnMetadata.SetValueAccessor(
            valueColumn,
            new DataGridColumnValueAccessor<Item, double>(item => item.Value));
        grid.Columns.Add(valueColumn);

        var window = new Window
        {
            Width = 460,
            Height = 260,
            Content = grid,
        };
        window.SetThemeStyles(theme);
        if (useFlatTheme)
        {
            Assert.True(grid.TryFindResource("DataGridFlatTheme", out object? resource));
            grid.Theme = Assert.IsType<ControlTheme>(resource);
        }
        else
        {
            Assert.True(grid.TryFindResource(typeof(DataGrid), out object? resource));
            grid.Theme = Assert.IsType<ControlTheme>(resource);
        }

        return (window, grid);
    }

    private static DataGridRowsPresenter GetRowsPresenter(DataGrid grid)
    {
        return Assert.Single(grid.GetVisualDescendants().OfType<DataGridRowsPresenter>());
    }

    private static void PumpLayout(Control control)
    {
        control.UpdateLayout();
        Dispatcher.UIThread.RunJobs();
        control.UpdateLayout();
    }

    private sealed class Item : INotifyPropertyChanged
    {
        private string _name;
        private bool? _isActive;

        public Item(int id, string name, double value)
        {
            Id = id;
            _name = name;
            Value = value;
            _isActive = id % 2 == 0;
        }

        public int Id { get; set; }

        public string Name
        {
            get => _name;
            set
            {
                if (_name == value)
                {
                    return;
                }

                _name = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Name)));
            }
        }

        public double Value { get; set; }

        public bool? IsActive
        {
            get => _isActive;
            set
            {
                if (_isActive == value)
                {
                    return;
                }

                _isActive = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsActive)));
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
    }
}
