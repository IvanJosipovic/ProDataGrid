// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using Xunit;

namespace Avalonia.Controls.DataGridTests;

public class DataGridDetachedFromVisualTreeTests
{
    [AvaloniaFact]
    public void Detach_clears_column_and_presenter_owners_and_headers()
    {
        var items = new ObservableCollection<Item>
        {
            new("A"),
            new("B")
        };

        var grid = new DataGrid
        {
            ItemsSource = items,
            HeadersVisibility = DataGridHeadersVisibility.Column,
            AutoGenerateColumns = false,
            Height = 200
        };

        grid.Columns.Add(new DataGridTextColumn
        {
            Header = "Name",
            Binding = new Binding(nameof(Item.Name))
        });

        var window = new Window
        {
            Width = 400,
            Height = 300
        };

        window.SetThemeStyles(DataGridTheme.SimpleV2);
        window.Content = grid;

        try
        {
            window.Show();
            Dispatcher.UIThread.RunJobs();
            window.UpdateLayout();
            grid.UpdateLayout();

            grid.ApplyTemplate();
            InvokePrivate(grid, "EnsureColumnHeadersPresenterChildren");

            var headersPresenter = GetPrivateField<DataGridColumnHeadersPresenter>(grid, "_columnHeadersPresenter");
            var rowsPresenter = GetPrivateField<DataGridRowsPresenter>(grid, "_rowsPresenter");

            Assert.NotNull(headersPresenter);
            Assert.NotNull(rowsPresenter);
            Assert.NotEmpty(headersPresenter!.Children);

            foreach (var column in grid.ColumnsInternal)
            {
                Assert.Same(grid, column.OwningGrid);
            }
            Assert.Same(grid, headersPresenter.OwningGrid);
            Assert.Same(grid, rowsPresenter.OwningGrid);

            window.Content = null;
            Dispatcher.UIThread.RunJobs();
            window.UpdateLayout();

            foreach (var column in grid.ColumnsInternal)
            {
                Assert.Null(column.OwningGrid);
            }
            Assert.Null(headersPresenter.OwningGrid);
            Assert.Null(rowsPresenter.OwningGrid);
            Assert.Empty(headersPresenter.Children);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void Detach_unwires_data_connection_events()
    {
        var items = new ObservableCollection<Item>
        {
            new("A"),
            new("B")
        };

        var grid = new DataGrid
        {
            ItemsSource = items,
            AutoGenerateColumns = false,
            Height = 200
        };

        grid.Columns.Add(new DataGridTextColumn
        {
            Header = "Name",
            Binding = new Binding(nameof(Item.Name))
        });

        var window = new Window
        {
            Width = 400,
            Height = 300
        };

        window.SetThemeStyles(DataGridTheme.SimpleV2);
        window.Content = grid;

        try
        {
            window.Show();
            Dispatcher.UIThread.RunJobs();
            window.UpdateLayout();
            grid.UpdateLayout();

            Assert.NotNull(grid.DataConnection.DataSource);
            Assert.True(grid.DataConnection.EventsWired);

            window.Content = null;
            Dispatcher.UIThread.RunJobs();
            window.UpdateLayout();

            Assert.False(grid.DataConnection.EventsWired);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void Detach_cancels_active_editing_row()
    {
        var items = new ObservableCollection<Item>
        {
            new("A"),
            new("B")
        };

        var grid = new DataGrid
        {
            ItemsSource = items,
            AutoGenerateColumns = false,
            Height = 200
        };

        grid.Columns.Add(new DataGridTextColumn
        {
            Header = "Name",
            Binding = new Binding(nameof(Item.Name))
        });

        var window = new Window
        {
            Width = 400,
            Height = 300
        };

        window.SetThemeStyles(DataGridTheme.SimpleV2);
        window.Content = grid;

        try
        {
            window.Show();
            Dispatcher.UIThread.RunJobs();
            window.UpdateLayout();
            grid.UpdateLayout();

            SetCurrentCell(grid, rowIndex: 0, columnIndex: 0);
            Assert.True(grid.BeginEdit());
            grid.UpdateLayout();
            Dispatcher.UIThread.RunJobs();
            Assert.NotNull(grid.EditingRow);

            window.Content = null;
            Dispatcher.UIThread.RunJobs();
            window.UpdateLayout();

            Assert.Null(grid.EditingRow);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void Detach_disposes_row_drag_drop_controller()
    {
        var items = new ObservableCollection<Item>
        {
            new("A"),
            new("B")
        };

        var grid = new DataGrid
        {
            ItemsSource = items,
            AutoGenerateColumns = false,
            CanUserReorderRows = true,
            Height = 200
        };

        grid.Columns.Add(new DataGridTextColumn
        {
            Header = "Name",
            Binding = new Binding(nameof(Item.Name))
        });

        var window = new Window
        {
            Width = 400,
            Height = 300
        };

        window.SetThemeStyles(DataGridTheme.SimpleV2);
        window.Content = grid;

        try
        {
            window.Show();
            Dispatcher.UIThread.RunJobs();
            window.UpdateLayout();
            grid.UpdateLayout();

            var controller = GetPrivateField<object>(grid, "_rowDragDropController");
            Assert.NotNull(controller);

            window.Content = null;
            Dispatcher.UIThread.RunJobs();
            window.UpdateLayout();

            controller = GetPrivateField<object>(grid, "_rowDragDropController");
            Assert.Null(controller);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void Detach_disposes_summary_service()
    {
        var items = new ObservableCollection<Item>
        {
            new("A"),
            new("B")
        };

        var grid = new DataGrid
        {
            ItemsSource = items,
            AutoGenerateColumns = false,
            Height = 200
        };

        grid.Columns.Add(new DataGridTextColumn
        {
            Header = "Name",
            Binding = new Binding(nameof(Item.Name))
        });

        var window = new Window
        {
            Width = 400,
            Height = 300
        };

        window.SetThemeStyles(DataGridTheme.SimpleV2);
        window.Content = grid;

        try
        {
            window.Show();
            Dispatcher.UIThread.RunJobs();
            window.UpdateLayout();
            grid.UpdateLayout();

            var summaryService = GetPrivateField<object>(grid, "_summaryService");
            Assert.NotNull(summaryService);

            window.Content = null;
            Dispatcher.UIThread.RunJobs();
            window.UpdateLayout();

            summaryService = GetPrivateField<object>(grid, "_summaryService");
            Assert.Null(summaryService);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void Detach_clears_pending_layout_refresh_flags_and_external_editing_element_and_validation_subscription_and_keyboard_subscriptions()
    {
        var items = new ObservableCollection<Item>
        {
            new("A"),
            new("B")
        };

        var (window, grid) = CreateWindowWithGrid(items);

        try
        {
            window.Show();
            Dispatcher.UIThread.RunJobs();
            window.UpdateLayout();
            grid.UpdateLayout();

            SetPrivateField(grid, "_pendingPointerOverRefresh", true);
            SetPrivateField(grid, "_pendingHierarchicalIndentationRefresh", true);
            SetPrivateField(grid, "_pendingGroupingIndentationRefresh", true);
            SetPrivateField(grid, "_groupingIndentationRefreshQueued", true);
            SetPrivateField(grid, "_pendingSelectionOverlayRefresh", true);
            SetPrivateField(grid, "_selectionOverlayLayoutHooked", true);

            var externalEditingElement = new TextBox();
            SetPrivateField(grid, "_externalEditingElement", externalEditingElement);

            var validationSubscription = new TrackingDisposable();
            SetPrivateField(grid, "_validationSubscription", validationSubscription);

            Assert.NotNull(GetPrivateField<object>(grid, "_keyDownRouteFinishedSubscription"));
            Assert.NotNull(GetPrivateField<object>(grid, "_keyUpRouteFinishedSubscription"));

            window.Content = null;
            Dispatcher.UIThread.RunJobs();
            window.UpdateLayout();

            Assert.False(GetPrivateFieldValue<bool>(grid, "_pendingPointerOverRefresh"));
            Assert.False(GetPrivateFieldValue<bool>(grid, "_pendingHierarchicalIndentationRefresh"));
            Assert.False(GetPrivateFieldValue<bool>(grid, "_pendingGroupingIndentationRefresh"));
            Assert.False(GetPrivateFieldValue<bool>(grid, "_groupingIndentationRefreshQueued"));
            Assert.False(GetPrivateFieldValue<bool>(grid, "_pendingSelectionOverlayRefresh"));
            Assert.False(GetPrivateFieldValue<bool>(grid, "_selectionOverlayLayoutHooked"));
            Assert.Null(GetPrivateField<object>(grid, "_externalEditingElement"));
            Assert.True(validationSubscription.IsDisposed);
            Assert.Null(GetPrivateField<object>(grid, "_validationSubscription"));
            Assert.Null(GetPrivateField<object>(grid, "_keyDownRouteFinishedSubscription"));
            Assert.Null(GetPrivateField<object>(grid, "_keyUpRouteFinishedSubscription"));
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void Detach_captures_grouping_state_and_scroll_state_and_clears_pending_auto_scroll()
    {
        var items = new ObservableCollection<GroupedItem>
        {
            new("A1", "G1"),
            new("A2", "G1"),
            new("B1", "G2")
        };

        var (window, grid) = CreateWindowWithGrid(items);

        try
        {
            window.Show();
            Dispatcher.UIThread.RunJobs();
            window.UpdateLayout();
            grid.UpdateLayout();

            var view = (DataGridCollectionView)grid.DataConnection.CollectionView;
            view.GroupDescriptions.Add(new DataGridPathGroupDescription(nameof(GroupedItem.Group)));
            view.Refresh();
            Dispatcher.UIThread.RunJobs();
            grid.UpdateLayout();

            Assert.False(grid.RowGroupHeadersTable.IsEmpty);

            SetPrivateField(grid, "_autoScrollPending", true);
            SetPrivateField(grid, "_autoScrollRequestToken", 41);

            window.Content = null;
            Dispatcher.UIThread.RunJobs();
            window.UpdateLayout();

            var pendingGroupingState = GetPrivateFieldValue<object>(grid, "_pendingGroupingState");
            Assert.NotNull(pendingGroupingState);

            var scrollManager = GetPrivateField<object>(grid, "_scrollStateManager");
            Assert.NotNull(scrollManager);
            Assert.True(GetPrivatePropertyValue<bool>(scrollManager!, "PendingRestore"));
            Assert.True(GetPrivatePropertyValue<bool>(scrollManager!, "PreserveOnAttach"));

            Assert.False(GetPrivateFieldValue<bool>(grid, "_autoScrollPending"));
            Assert.Equal(42, GetPrivateFieldValue<int>(grid, "_autoScrollRequestToken"));
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void Detach_removes_recycled_children_from_rows_presenter()
    {
        var items = new ObservableCollection<Item>
        {
            new("A"),
            new("B"),
            new("C")
        };

        var (window, grid) = CreateWindowWithGrid(items);

        try
        {
            window.Show();
            Dispatcher.UIThread.RunJobs();
            window.UpdateLayout();
            grid.UpdateLayout();

            grid.ApplyTemplate();
            var rowsPresenter = GetPrivateField<DataGridRowsPresenter>(grid, "_rowsPresenter");
            Assert.NotNull(rowsPresenter);
            Assert.True(rowsPresenter!.Children.OfType<DataGridRow>().Any());

            window.Content = null;
            Dispatcher.UIThread.RunJobs();
            window.UpdateLayout();

            Assert.False(rowsPresenter.Children.OfType<DataGridRow>().Any());
            Assert.False(rowsPresenter.Children.OfType<DataGridRowGroupHeader>().Any());
            Assert.False(rowsPresenter.Children.OfType<DataGridRowGroupFooter>().Any());
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void Detach_clears_drag_and_fill_auto_scroll_state()
    {
        var items = new ObservableCollection<Item>
        {
            new("A"),
            new("B")
        };

        var (window, grid) = CreateWindowWithGrid(items);

        try
        {
            window.Show();
            Dispatcher.UIThread.RunJobs();
            window.UpdateLayout();
            grid.UpdateLayout();

            SetPrivateField(grid, "_isDraggingSelection", true);
            SetPrivateField(grid, "_dragPointerId", (int?)17);
            SetPrivateField(grid, "_dragStartPoint", (Point?)new Point(1, 1));
            SetPrivateField(grid, "_dragLastPoint", (Point?)new Point(2, 2));
            SetPrivateField(grid, "_dragLastSlot", 3);
            SetPrivateField(grid, "_dragLastColumnIndex", 1);
            SetPrivateField(grid, "_dragAnchorSlot", 2);
            SetPrivateField(grid, "_dragCapturePending", true);
            SetPrivateField(grid, "_dragAutoScrollTimer", new DispatcherTimer());
            SetPrivateField(grid, "_dragAutoScrollDirectionX", 1);
            SetPrivateField(grid, "_dragAutoScrollDirectionY", -1);

            SetPrivateField(grid, "_isFillHandleDragging", true);
            SetPrivateField(grid, "_fillPointerId", (int?)22);
            SetPrivateField(grid, "_fillLastPoint", (Point?)new Point(3, 3));
            SetPrivateField(grid, "_fillAutoScrollTimer", new DispatcherTimer());
            SetPrivateField(grid, "_fillAutoScrollDirectionX", 1);
            SetPrivateField(grid, "_fillAutoScrollDirectionY", 1);

            window.Content = null;
            Dispatcher.UIThread.RunJobs();
            window.UpdateLayout();

            Assert.False(GetPrivateFieldValue<bool>(grid, "_isDraggingSelection"));
            Assert.Null(GetPrivateFieldValue<int?>(grid, "_dragPointerId"));
            Assert.Null(GetPrivateFieldValue<Point?>(grid, "_dragStartPoint"));
            Assert.Null(GetPrivateFieldValue<Point?>(grid, "_dragLastPoint"));
            Assert.Equal(-1, GetPrivateFieldValue<int>(grid, "_dragLastSlot"));
            Assert.Equal(-1, GetPrivateFieldValue<int>(grid, "_dragLastColumnIndex"));
            Assert.Equal(-1, GetPrivateFieldValue<int>(grid, "_dragAnchorSlot"));
            Assert.False(GetPrivateFieldValue<bool>(grid, "_dragCapturePending"));
            Assert.Null(GetPrivateField<object>(grid, "_dragAutoScrollTimer"));
            Assert.Equal(0, GetPrivateFieldValue<int>(grid, "_dragAutoScrollDirectionX"));
            Assert.Equal(0, GetPrivateFieldValue<int>(grid, "_dragAutoScrollDirectionY"));

            Assert.False(GetPrivateFieldValue<bool>(grid, "_isFillHandleDragging"));
            Assert.Null(GetPrivateFieldValue<int?>(grid, "_fillPointerId"));
            Assert.Null(GetPrivateFieldValue<Point?>(grid, "_fillLastPoint"));
            Assert.Null(GetPrivateField<object>(grid, "_fillAutoScrollTimer"));
            Assert.Equal(0, GetPrivateFieldValue<int>(grid, "_fillAutoScrollDirectionX"));
            Assert.Equal(0, GetPrivateFieldValue<int>(grid, "_fillAutoScrollDirectionY"));
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void Detach_detaches_adapter_views_and_resets_column_header_static_state()
    {
        var items = new ObservableCollection<Item>
        {
            new("A"),
            new("B")
        };

        var (window, grid) = CreateWindowWithGrid(items);

        try
        {
            window.Show();
            Dispatcher.UIThread.RunJobs();
            window.UpdateLayout();
            grid.UpdateLayout();

            var sortingAdapter = GetPrivateField<object>(grid, "_sortingAdapter");
            var filteringAdapter = GetPrivateField<object>(grid, "_filteringAdapter");
            var searchAdapter = GetPrivateField<object>(grid, "_searchAdapter");
            var conditionalAdapter = GetPrivateField<object>(grid, "_conditionalFormattingAdapter");

            Assert.NotNull(sortingAdapter);
            Assert.NotNull(filteringAdapter);
            Assert.NotNull(searchAdapter);
            Assert.NotNull(conditionalAdapter);

            Assert.NotNull(GetPrivatePropertyValue<object>(sortingAdapter, "View"));
            Assert.NotNull(GetPrivatePropertyValue<object>(filteringAdapter, "View"));
            Assert.NotNull(GetPrivatePropertyValue<object>(searchAdapter, "View"));
            Assert.NotNull(GetPrivatePropertyValue<object>(conditionalAdapter, "View"));

            SetPrivateField(grid, "_externalSubscriptionsDetached", true);
            SetColumnHeaderStaticState();

            window.Content = null;
            Dispatcher.UIThread.RunJobs();
            window.UpdateLayout();

            Assert.Null(GetPrivatePropertyValue<object>(sortingAdapter, "View"));
            Assert.Null(GetPrivatePropertyValue<object>(filteringAdapter, "View"));
            Assert.Null(GetPrivatePropertyValue<object>(searchAdapter, "View"));
            Assert.Null(GetPrivatePropertyValue<object>(conditionalAdapter, "View"));

            AssertColumnHeaderStaticStateReset();
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void Detach_clears_total_summary_row()
    {
        var items = new ObservableCollection<Item>
        {
            new("A"),
            new("B")
        };

        var (window, grid) = CreateWindowWithGrid(items, g => g.ShowTotalSummary = true);

        try
        {
            window.Show();
            Dispatcher.UIThread.RunJobs();
            window.UpdateLayout();
            grid.UpdateLayout();

            grid.ApplyTemplate();
            var summaryRow = GetPrivateField<DataGridSummaryRow>(grid, "_totalSummaryRow");
            Assert.NotNull(summaryRow);
            Assert.Same(grid, summaryRow!.OwningGrid);
            Assert.NotEmpty(summaryRow.Cells);

            window.Content = null;
            Dispatcher.UIThread.RunJobs();
            window.UpdateLayout();

            Assert.Null(summaryRow.OwningGrid);
            Assert.Empty(summaryRow.Cells);
        }
        finally
        {
            window.Close();
        }
    }

    private static void SetCurrentCell(DataGrid grid, int rowIndex, int columnIndex)
    {
        var slot = grid.SlotFromRowIndex(rowIndex);
        grid.UpdateSelectionAndCurrency(columnIndex, slot, DataGridSelectionAction.SelectCurrent, scrollIntoView: true);
        grid.UpdateLayout();
    }

    private static T? GetPrivateField<T>(object target, string name) where T : class
    {
        var field = GetRequiredField(target.GetType(), name, BindingFlags.Instance | BindingFlags.NonPublic);
        return field.GetValue(target) as T;
    }

    private static void InvokePrivate(object target, string name)
    {
        var method = target.GetType().GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic);
        method?.Invoke(target, Array.Empty<object>());
    }

    private static T GetPrivateFieldValue<T>(object target, string name)
    {
        var field = GetRequiredField(target.GetType(), name, BindingFlags.Instance | BindingFlags.NonPublic);
        return (T)field.GetValue(target)!;
    }

    private static void SetPrivateField(object target, string name, object value)
    {
        var field = GetRequiredField(target.GetType(), name, BindingFlags.Instance | BindingFlags.NonPublic);
        field.SetValue(target, value);
    }

    private static T GetPrivatePropertyValue<T>(object target, string name)
    {
        var property = target.GetType().GetProperty(name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        if (property == null)
        {
            throw new InvalidOperationException($"Property '{name}' not found on {target.GetType().Name}.");
        }
        return (T)property.GetValue(target)!;
    }

    private static FieldInfo GetRequiredField(Type type, string name, BindingFlags flags)
    {
        var field = type.GetField(name, flags);
        if (field == null)
        {
            throw new InvalidOperationException($"Field '{name}' not found on {type.Name}.");
        }
        return field;
    }

    private static (Window window, DataGrid grid) CreateWindowWithGrid<TItem>(ObservableCollection<TItem> items, Action<DataGrid>? configure = null)
    {
        var grid = new DataGrid
        {
            ItemsSource = items,
            AutoGenerateColumns = false,
            Height = 200
        };

        grid.Columns.Add(new DataGridTextColumn
        {
            Header = "Name",
            Binding = new Binding("Name")
        });

        configure?.Invoke(grid);

        var window = new Window
        {
            Width = 400,
            Height = 300
        };

        window.SetThemeStyles(DataGridTheme.SimpleV2);
        window.Content = grid;

        return (window, grid);
    }

    private static void SetColumnHeaderStaticState()
    {
        var headerType = typeof(DataGridColumnHeader);
        var dragModeField = GetRequiredField(headerType, "_dragMode", BindingFlags.Static | BindingFlags.NonPublic);
        var dragModeValue = Enum.ToObject(dragModeField.FieldType, 1);
        dragModeField.SetValue(null, dragModeValue);

        var dragColumn = new DataGridTextColumn { Header = "Drag" };
        GetRequiredField(headerType, "_dragColumn", BindingFlags.Static | BindingFlags.NonPublic).SetValue(null, dragColumn);
        GetRequiredField(headerType, "_dragStart", BindingFlags.Static | BindingFlags.NonPublic).SetValue(null, (Point?)new Point(1, 1));
        GetRequiredField(headerType, "_lastMousePositionHeaders", BindingFlags.Static | BindingFlags.NonPublic).SetValue(null, (Point?)new Point(2, 2));
    }

    private static void AssertColumnHeaderStaticStateReset()
    {
        var headerType = typeof(DataGridColumnHeader);
        var dragMode = GetRequiredField(headerType, "_dragMode", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
        Assert.Equal("None", dragMode?.ToString());
        Assert.Null(GetRequiredField(headerType, "_dragColumn", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null));
        Assert.Null(GetRequiredField(headerType, "_dragStart", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null));
        Assert.Null(GetRequiredField(headerType, "_lastMousePositionHeaders", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null));
    }

    private sealed class TrackingDisposable : IDisposable
    {
        public bool IsDisposed { get; private set; }

        public void Dispose()
        {
            IsDisposed = true;
        }
    }

    private record GroupedItem(string Name, string Group);

    private record Item(string Name);
}
