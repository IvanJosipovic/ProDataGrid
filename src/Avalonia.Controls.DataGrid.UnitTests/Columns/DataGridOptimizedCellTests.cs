// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Controls.DataGridHierarchical;
using Avalonia.Controls.Automation.Peers;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Xunit;

namespace Avalonia.Controls.DataGridTests.Columns;

public sealed class DataGridOptimizedCellTests
{
    [AvaloniaFact]
    public void DirectTextCell_UsesTypedAccessor_AndTracksItemChanges()
    {
        var item = new NotifyItem("First");
        var column = new DataGridTextColumn
        {
            Binding = new Binding(nameof(NotifyItem.Name)),
            UseDirectTextCell = true
        };
        DataGridColumnMetadata.SetValueAccessor(
            column,
            new DataGridColumnValueAccessor<NotifyItem, string>(value => value.Name));

        var cell = Assert.IsType<DataGridDirectTextCell>(column.CreateCell());
        cell.DataContext = item;

        Assert.True(cell.ConfigureValueAccessor(column));
        Assert.Equal("First", cell.Value);

        item.Name = "Second";

        Assert.Equal("Second", cell.Value);
    }

    [AvaloniaFact]
    public void DirectTextCell_TracksChanges_From_HierarchicalNodeItem()
    {
        var item = new NotifyItem("First");
        var node = new HierarchicalNode(item, isLeaf: true);
        var column = new DataGridTextColumn
        {
            Binding = new Binding("Item.Name"),
            UseDirectTextCell = true
        };
        DataGridColumnMetadata.SetValueAccessor(
            column,
            new DataGridColumnValueAccessor<HierarchicalNode, string>(
                value => ((NotifyItem)value.Item).Name));

        var cell = Assert.IsType<DataGridDirectTextCell>(column.CreateCell());
        cell.DataContext = node;

        Assert.True(cell.ConfigureValueAccessor(column));
        Assert.Equal("First", cell.Value);

        item.Name = "Second";

        Assert.Equal("Second", cell.Value);
    }

    [AvaloniaFact]
    public void DirectTextCell_FallsBackToBinding_ForExplicitSource()
    {
        var item = new NotifyItem("First");
        var column = new DataGridTextColumn
        {
            Binding = new Binding(nameof(NotifyItem.Name)) { Source = item },
            UseDirectTextCell = true
        };
        DataGridColumnMetadata.SetValueAccessor(
            column,
            new DataGridColumnValueAccessor<NotifyItem, string>(value => value.Name));

        var cell = Assert.IsType<DataGridDirectTextCell>(column.CreateCell());

        Assert.False(cell.ConfigureValueAccessor(column));
    }

    [AvaloniaFact]
    public void DirectTextCell_Can_Skip_Change_Subscriptions_For_Immutable_Data()
    {
        var item = new NotifyItem("First");
        var column = new DataGridTextColumn
        {
            Binding = new Binding(nameof(NotifyItem.Name)),
            UseDirectTextCell = true,
            TrackDirectTextValueChanges = false
        };
        DataGridColumnMetadata.SetValueAccessor(
            column,
            new DataGridColumnValueAccessor<NotifyItem, string>(value => value.Name));

        var cell = Assert.IsType<DataGridDirectTextCell>(column.CreateCell());
        cell.DataContext = item;
        Assert.True(cell.ConfigureValueAccessor(column));

        item.Name = "Second";

        Assert.Equal("First", cell.Value);
        cell.DataContext = new NotifyItem("Third");
        Assert.Equal("Third", cell.Value);
    }

    [AvaloniaFact]
    public void DirectHierarchicalCell_TracksNodeState()
    {
        var node = new HierarchicalNode("Node", level: 2, isLeaf: false);
        var cell = new DataGridDirectHierarchicalCell
        {
            Indent = 10,
            DataContext = node
        };

        Assert.Equal(2, cell.Level);
        Assert.Equal(new Thickness(20, 0, 0, 0), cell.Padding);
        Assert.True(cell.IsExpandable);
        Assert.False(cell.IsExpanded);

        node.IsExpanded = true;

        Assert.True(cell.IsExpanded);
    }

    [AvaloniaFact]
    public void DirectHierarchicalTextCell_UsesTypedAccessor_AndTracksItemChanges()
    {
        var item = new NotifyItem("First");
        var node = new HierarchicalNode(item, isLeaf: true);
        var column = new DataGridHierarchicalColumn
        {
            Binding = new Binding("Item.Name"),
            UseDirectCell = true,
            UseDirectTextContent = true
        };
        DataGridColumnMetadata.SetValueAccessor(
            column,
            new DataGridColumnValueAccessor<HierarchicalNode, string>(
                value => ((NotifyItem)value.Item).Name));

        var cell = Assert.IsType<DataGridDirectHierarchicalCell>(column.CreateCell());
        cell.DataContext = node;

        Assert.True(cell.ConfigureTextAccessor(column));
        Assert.Equal("First", cell.Value);

        item.Name = "Second";

        Assert.Equal("Second", cell.Value);
    }

    [AvaloniaFact]
    public void DirectHierarchicalTextCell_Can_Skip_Item_Change_Subscriptions_For_Immutable_Data()
    {
        var item = new NotifyItem("First");
        var node = new HierarchicalNode(item, isLeaf: true);
        var column = new DataGridHierarchicalColumn
        {
            Binding = new Binding("Item.Name"),
            UseDirectCell = true,
            UseDirectTextContent = true,
            TrackDirectTextValueChanges = false
        };
        DataGridColumnMetadata.SetValueAccessor(
            column,
            new DataGridColumnValueAccessor<HierarchicalNode, string>(
                value => ((NotifyItem)value.Item).Name));

        var cell = Assert.IsType<DataGridDirectHierarchicalCell>(column.CreateCell());
        cell.DataContext = node;
        Assert.True(cell.ConfigureTextAccessor(column));

        item.Name = "Second";

        Assert.Equal("First", cell.Value);
        var replacement = new HierarchicalNode(new NotifyItem("Third"), isLeaf: true);
        cell.DataContext = replacement;
        Assert.Equal("Third", cell.Value);

        replacement.IsExpanded = true;
        Assert.True(cell.IsExpanded);
    }

    [AvaloniaFact]
    public void DirectHierarchicalTextCell_Preserves_CustomTemplate_Path()
    {
        var column = new DataGridHierarchicalColumn
        {
            Binding = new Binding("Item.Name"),
            UseDirectCell = true,
            UseDirectTextContent = true,
            CellTemplate = new FuncDataTemplate<HierarchicalNode>((_, _) => new TextBlock())
        };
        DataGridColumnMetadata.SetValueAccessor(
            column,
            new DataGridColumnValueAccessor<HierarchicalNode, string>(value => value.Item.ToString()!));

        var cell = Assert.IsType<DataGridDirectHierarchicalCell>(column.CreateCell());

        Assert.False(cell.ConfigureTextAccessor(column));
    }

    [AvaloniaFact]
    public void OptimizedColumns_CreateCoalescedCellContainers()
    {
        var drawingColumn = new DataGridCustomDrawingColumn();
        var hierarchyColumn = new DataGridHierarchicalColumn { UseDirectCell = true };
        var textColumn = new DataGridTextColumn { UseDirectTextCell = true };

        Assert.IsType<DataGridCustomDrawingCell>(drawingColumn.CreateCell());
        Assert.IsType<DataGridDirectHierarchicalCell>(hierarchyColumn.CreateCell());
        Assert.IsType<DataGridDirectTextCell>(textColumn.CreateCell());
    }

    [AvaloniaFact]
    public void OrdinaryColumns_DrawnMode_CreatesCoalescedCellContainers()
    {
        var text = new DataGridTextColumn { DisplayMode = DataGridColumnDisplayMode.Drawn };
        var numeric = new DataGridNumericColumn { DisplayMode = DataGridColumnDisplayMode.Drawn };
        var progress = new DataGridProgressBarColumn { DisplayMode = DataGridColumnDisplayMode.Drawn };
        var image = new DataGridImageColumn
        {
            DisplayMode = DataGridColumnDisplayMode.Drawn,
            ImageWidth = 16,
            ImageHeight = 16
        };

        Assert.IsType<DataGridCustomDrawingCell>(text.CreateCell());
        Assert.IsType<DataGridCustomDrawingCell>(numeric.CreateCell());
        Assert.IsType<DataGridCustomDrawingCell>(progress.CreateCell());
        Assert.IsType<DataGridCustomDrawingCell>(image.CreateCell());
    }

    [AvaloniaFact]
    public void Unsupported_Draw_Configurations_Fall_Back_To_Retained_Cells()
    {
        var progress = new DataGridProgressBarColumn
        {
            DisplayMode = DataGridColumnDisplayMode.Drawn,
            ShowProgressText = true
        };
        var image = new DataGridImageColumn
        {
            DisplayMode = DataGridColumnDisplayMode.Drawn
        };

        Assert.IsType<DataGridCell>(progress.CreateCell());
        Assert.IsNotType<DataGridCustomDrawingCell>(progress.CreateCell());
        Assert.IsType<DataGridCell>(image.CreateCell());
        Assert.IsNotType<DataGridCustomDrawingCell>(image.CreateCell());
    }

    [AvaloniaFact]
    public void DrawnText_UsesTypedAccessor_AndTracksItemChanges()
    {
        var item = new NotifyItem("First");
        var column = new TestTextColumn
        {
            Binding = new Binding(nameof(NotifyItem.Name)),
            DisplayMode = DataGridColumnDisplayMode.Drawn
        };
        DataGridColumnMetadata.SetValueAccessor(
            column,
            new DataGridColumnValueAccessor<NotifyItem, string>(value => value.Name));

        var cell = Assert.IsType<DataGridCustomDrawingCell>(column.CreateCell());
        cell.DataContext = item;
        Assert.Null(column.GenerateDisplay(cell, item));
        Assert.Null(cell.Content);
        Assert.Equal("First", cell.Value);

        item.Name = "Second";

        Assert.Equal("Second", cell.Value);
    }

    [AvaloniaFact]
    public void DrawnText_Can_Skip_Change_Subscriptions_For_Immutable_Data()
    {
        var item = new NotifyItem("First");
        var column = new TestTextColumn
        {
            Binding = new Binding(nameof(NotifyItem.Name)),
            DisplayMode = DataGridColumnDisplayMode.Drawn,
            TrackDirectTextValueChanges = false
        };
        DataGridColumnMetadata.SetValueAccessor(
            column,
            new DataGridColumnValueAccessor<NotifyItem, string>(value => value.Name));

        var cell = Assert.IsType<DataGridCustomDrawingCell>(column.CreateCell());
        cell.DataContext = item;
        Assert.Null(column.GenerateDisplay(cell, item));

        item.Name = "Second";

        Assert.Equal("First", cell.Value);
        cell.DataContext = new NotifyItem("Third");
        Assert.Equal("Third", cell.Value);
    }

    [AvaloniaFact]
    public void CustomDrawingCell_Can_Use_Typed_Accessor_Without_Change_Subscription()
    {
        var item = new NotifyItem("First");
        var column = new TestCustomDrawingColumn
        {
            Binding = new Binding(nameof(NotifyItem.Name)),
            UseDirectValueAccessor = true,
            TrackDirectValueChanges = false
        };
        DataGridColumnMetadata.SetValueAccessor(
            column,
            new DataGridColumnValueAccessor<NotifyItem, string>(value => value.Name));

        var cell = Assert.IsType<DataGridCustomDrawingCell>(column.CreateCell());
        cell.DataContext = item;
        Assert.Null(column.GenerateDisplay(cell, item));
        Assert.Equal("First", cell.Value);

        item.Name = "Second";
        Assert.Equal("First", cell.Value);

        cell.DataContext = new NotifyItem("Third");
        Assert.Equal("Third", cell.Value);
    }

    [AvaloniaFact]
    public void DrawnNumeric_UsesFormattedTypedAccessor()
    {
        var item = new NumericItem(12.5m);
        var column = new TestNumericColumn
        {
            Binding = new Binding(nameof(NumericItem.Value)),
            DisplayMode = DataGridColumnDisplayMode.Drawn,
            FormatString = "N1"
        };
        DataGridColumnMetadata.SetValueAccessor(
            column,
            new DataGridColumnValueAccessor<NumericItem, decimal>(value => value.Value));

        var cell = Assert.IsType<DataGridCustomDrawingCell>(column.CreateCell());
        cell.DataContext = item;
        Assert.Null(column.GenerateDisplay(cell, item));

        Assert.Equal(12.5m.ToString("N1", column.NumberFormat ?? System.Globalization.CultureInfo.CurrentCulture.NumberFormat), cell.Value);
    }

    [AvaloniaFact]
    public void DrawnProgress_And_Image_UseAllocationFreeBuiltInRenderers()
    {
        var progressColumn = new TestProgressColumn
        {
            Binding = new Binding(nameof(NumericItem.Value)),
            DisplayMode = DataGridColumnDisplayMode.Drawn,
            Height = 6
        };
        var imageColumn = new TestImageColumn
        {
            Binding = new Binding(nameof(ImageItem.Image)),
            DisplayMode = DataGridColumnDisplayMode.Drawn,
            ImageWidth = 18,
            ImageHeight = 12
        };

        var progressCell = Assert.IsType<DataGridCustomDrawingCell>(progressColumn.CreateCell());
        var imageCell = Assert.IsType<DataGridCustomDrawingCell>(imageColumn.CreateCell());
        progressCell.OwningColumn = progressColumn;
        imageCell.OwningColumn = imageColumn;
        Assert.Null(progressColumn.GenerateDisplay(progressCell, new NumericItem(50m)));
        Assert.Null(imageColumn.GenerateDisplay(imageCell, new ImageItem(null)));

        progressCell.Measure(new Size(100, 24));
        imageCell.Measure(new Size(100, 24));

        Assert.Equal(6, progressCell.DesiredSize.Height);
        Assert.Equal(new Size(18, 12), imageCell.DesiredSize);
        Assert.Same(DataGridProgressCellRenderer.Instance, progressCell.BuiltInRendererForTesting);
        Assert.Same(DataGridImageCellRenderer.Instance, imageCell.BuiltInRendererForTesting);
    }

    [AvaloniaFact]
    public void DrawnDisplay_Still_Uses_Retained_Editors()
    {
        var text = new TestTextColumn { DisplayMode = DataGridColumnDisplayMode.Drawn };
        var numeric = new TestNumericColumn { DisplayMode = DataGridColumnDisplayMode.Drawn };
        var image = new TestImageColumn
        {
            DisplayMode = DataGridColumnDisplayMode.Drawn,
            ImageWidth = 16,
            ImageHeight = 16,
            AllowEditing = true
        };

        Assert.IsType<TextBox>(text.GenerateEditor(new DataGridCustomDrawingCell(), new object()));
        Assert.IsType<NumericUpDown>(numeric.GenerateEditor(new DataGridCustomDrawingCell(), new object()));
        Assert.IsType<TextBox>(image.GenerateEditor(new DataGridCustomDrawingCell(), new object()));
    }

    [Fact]
    public void DrawnCell_AutomationName_Uses_DisplayValue()
    {
        var cell = new DataGridCustomDrawingCell { Value = "Accessible value" };
        var peer = new DataGridCellAutomationPeer(cell);

        Assert.Equal("Accessible value", peer.GetName());
    }

    [Fact]
    public void ColumnDefinition_Applies_DrawMode()
    {
        var definition = new DataGridTextColumnDefinition
        {
            DisplayMode = DataGridColumnDisplayMode.Drawn
        };

        var column = definition.CreateColumn(new DataGridColumnDefinitionContext(new DataGrid()));

        Assert.Equal(DataGridColumnDisplayMode.Drawn, column.DisplayMode);
    }

    [AvaloniaFact]
    public void OrdinaryDrawnColumns_Recycle_Select_And_UseRetainedEditor()
    {
        var items = Enumerable.Range(0, 160)
            .Select(index => new DrawnItem($"Item {index}", index))
            .ToList();
        var textColumn = new DataGridTextColumn
        {
            Header = "Name",
            Binding = new Binding(nameof(DrawnItem.Name)),
            DisplayMode = DataGridColumnDisplayMode.Drawn,
            Width = new DataGridLength(160)
        };
        var numericColumn = new DataGridNumericColumn
        {
            Header = "Number",
            Binding = new Binding(nameof(DrawnItem.Number)),
            DisplayMode = DataGridColumnDisplayMode.Drawn,
            Width = new DataGridLength(100)
        };
        DataGridColumnMetadata.SetValueAccessor(
            textColumn,
            new DataGridColumnValueAccessor<DrawnItem, string>(item => item.Name, (item, value) => item.Name = value));
        DataGridColumnMetadata.SetValueAccessor(
            numericColumn,
            new DataGridColumnValueAccessor<DrawnItem, decimal>(item => item.Number, (item, value) => item.Number = value));

        var grid = new DataGrid
        {
            Width = 360,
            Height = 180,
            RowHeight = 24,
            ItemsSource = items,
            UseLogicalScrollable = true,
            AutoGenerateColumns = false
        };
        grid.ColumnsInternal.Add(textColumn);
        grid.ColumnsInternal.Add(numericColumn);

        var window = new Window { Width = 400, Height = 220 };
        window.SetThemeStyles(DataGridTheme.FluentV2);
        window.Content = grid;
        try
        {
            window.Show();
            grid.ApplyTemplate();
            Dispatcher.UIThread.RunJobs();
            window.UpdateLayout();
            grid.UpdateLayout();

            var firstRow = Assert.IsType<DataGridRow>(grid.GetRowFromItem(items[0]));
            for (var columnIndex = 0; columnIndex < 2; columnIndex++)
            {
                var cell = firstRow.Cells[columnIndex];
                Assert.IsType<DataGridCustomDrawingCell>(cell);
                Assert.Null(cell.Content);
            }

            var slot = grid.SlotFromRowIndex(0);
            grid.UpdateSelectionAndCurrency(0, slot, DataGridSelectionAction.SelectCurrent, scrollIntoView: false);
            Assert.True(grid.BeginEdit());
            Assert.IsType<TextBox>(firstRow.Cells[0].Content);
            Assert.True(grid.CommitEdit());
            Assert.Null(firstRow.Cells[0].Content);

            var originalCells = grid.GetVisualDescendants().OfType<DataGridCustomDrawingCell>().ToHashSet();
            grid.ScrollIntoView(items[^1], numericColumn);
            Dispatcher.UIThread.RunJobs();
            window.UpdateLayout();
            grid.UpdateLayout();

            var recycledCells = grid.GetVisualDescendants().OfType<DataGridCustomDrawingCell>().ToList();
            Assert.Contains(recycledCells, originalCells.Contains);
            Assert.All(recycledCells, cell => Assert.Null(cell.Content));
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void DisplayMode_Change_Recreates_Realized_Cells()
    {
        var item = new DrawnItem("Item", 1);
        var column = new DataGridTextColumn
        {
            Binding = new Binding(nameof(DrawnItem.Name)),
            Width = new DataGridLength(160)
        };
        var grid = new DataGrid
        {
            Width = 240,
            Height = 120,
            RowHeight = 24,
            ItemsSource = new[] { item },
            UseLogicalScrollable = true,
            AutoGenerateColumns = false
        };
        grid.ColumnsInternal.Add(column);

        var window = new Window { Width = 280, Height = 160 };
        window.SetThemeStyles(DataGridTheme.FluentV2);
        window.Content = grid;
        try
        {
            window.Show();
            grid.ApplyTemplate();
            Dispatcher.UIThread.RunJobs();
            window.UpdateLayout();
            grid.UpdateLayout();

            var row = Assert.IsType<DataGridRow>(grid.GetRowFromItem(item));
            Assert.IsAssignableFrom<TextBlock>(row.Cells[0].Content);
            var retainedCell = row.Cells[0];
            var clearing = new List<DataGridCell>();
            var prepared = new List<DataGridCell>();
            grid.CellClearing += (_, args) => clearing.Add(args.Cell);
            grid.CellPrepared += (_, args) => prepared.Add(args.Cell);
            grid.UpdateSelectionAndCurrency(
                columnIndex: 0,
                slot: grid.SlotFromRowIndex(0),
                action: DataGridSelectionAction.SelectCurrent,
                scrollIntoView: false);

            column.DisplayMode = DataGridColumnDisplayMode.Drawn;
            Dispatcher.UIThread.RunJobs();
            window.UpdateLayout();
            grid.UpdateLayout();

            row = Assert.IsType<DataGridRow>(grid.GetRowFromItem(item));
            Assert.IsType<DataGridCustomDrawingCell>(row.Cells[0]);
            Assert.Null(row.Cells[0].Content);
            Assert.Equal(0, grid.CurrentColumnIndex);
            Assert.Equal(grid.SlotFromRowIndex(0), grid.CurrentSlot);
            Assert.Equal(new[] { retainedCell }, clearing);
            Assert.Equal(new[] { row.Cells[0] }, prepared);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaTheory]
    [InlineData(false, true)]
    [InlineData(true, false)]
    public void LightweightFiller_AvoidsFillerCellCreation(bool useLightweightFiller, bool expectsFiller)
    {
        var grid = new DataGrid
        {
            Width = 500,
            Height = 160,
            ItemsSource = new[] { new NotifyItem("First") },
            UseLightweightFiller = useLightweightFiller,
            AutoGenerateColumns = false,
            HeadersVisibility = DataGridHeadersVisibility.Column
        };
        grid.ColumnsInternal.Add(new DataGridTextColumn
        {
            Width = new DataGridLength(100),
            Binding = new Binding(nameof(NotifyItem.Name))
        });

        var window = new Window { Width = 500, Height = 200 };
        window.SetThemeStyles(DataGridTheme.FluentV2);
        window.Content = grid;
        try
        {
            window.Show();
            Dispatcher.UIThread.RunJobs();
            window.UpdateLayout();
            grid.UpdateLayout();

            var row = Assert.Single(grid.GetVisualDescendants().OfType<DataGridRow>());
            Assert.Equal(expectsFiller, row.ExistingFillerCell != null);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void OptimizedUnfrozenRowTheme_Preserves_Retained_Cells_And_Horizontal_Scrolling()
    {
        var items = Enumerable.Range(0, 20).Select(index => new NotifyItem($"Item {index}")).ToList();
        var grid = new DataGrid
        {
            Width = 280,
            Height = 160,
            RowHeight = 24,
            ItemsSource = items,
            AutoGenerateColumns = false,
            HeadersVisibility = DataGridHeadersVisibility.Column,
            UseLogicalScrollable = true,
            UseLightweightFiller = true
        };
        for (var index = 0; index < 4; index++)
        {
            grid.ColumnsInternal.Add(new DataGridTextColumn
            {
                Header = $"Column {index}",
                Binding = new Binding(nameof(NotifyItem.Name)),
                Width = new DataGridLength(180)
            });
        }

        var window = new Window { Width = 320, Height = 200 };
        window.SetThemeStyles(DataGridTheme.FluentV2);
        window.Content = grid;
        try
        {
            Assert.True(grid.TryFindResource("DataGridOptimizedUnfrozenRowTheme", out var rowTheme));
            grid.RowTheme = Assert.IsType<ControlTheme>(rowTheme);
            window.Show();
            Dispatcher.UIThread.RunJobs();
            window.UpdateLayout();
            grid.UpdateLayout();

            var row = Assert.IsType<DataGridRow>(grid.GetRowFromItem(items[0]));
            Assert.Empty(row.GetVisualDescendants().OfType<DataGridFrozenGrid>());
            Assert.Single(row.GetVisualDescendants().OfType<DataGridCellsPresenter>());
            Assert.Equal(4, row.Cells.Count);
            foreach (DataGridCell cell in row.Cells)
            {
                Assert.NotNull(cell.Content);
            }

            var scrollViewer = grid.GetVisualDescendants()
                .OfType<ScrollViewer>()
                .First(viewer => viewer.Name == "PART_ScrollViewer");
            scrollViewer.Offset = new Vector(120, scrollViewer.Offset.Y);
            Dispatcher.UIThread.RunJobs();
            window.UpdateLayout();
            grid.UpdateLayout();

            Assert.True(scrollViewer.Offset.X > 0);
            Assert.Equal(4, row.Cells.Count);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void RecycledRetainedCells_UpdateIndexerBindingWithoutRegeneration()
    {
        var items = Enumerable.Range(0, 200)
            .Select(index => new IndexedItem($"Value {index}"))
            .ToList();
        var column = new DataGridTextColumn
        {
            Header = "Value",
            Width = new DataGridLength(180),
            Binding = new Binding("Fields[0]")
        };
        var grid = new DataGrid
        {
            Width = 320,
            Height = 160,
            RowHeight = 24,
            ItemsSource = items,
            UseLogicalScrollable = true,
            AutoGenerateColumns = false,
            HeadersVisibility = DataGridHeadersVisibility.Column
        };
        grid.ColumnsInternal.Add(column);

        var window = new Window { Width = 320, Height = 200 };
        window.SetThemeStyles(DataGridTheme.FluentV2);
        window.Content = grid;
        try
        {
            window.Show();
            Dispatcher.UIThread.RunJobs();
            window.UpdateLayout();
            grid.UpdateLayout();
            var originalCells = grid.GetVisualDescendants().OfType<DataGridCell>().ToHashSet();

            grid.ScrollIntoView(items[^1], column);
            Dispatcher.UIThread.RunJobs();
            window.UpdateLayout();
            grid.UpdateLayout();

            var displayedRows = grid.GetVisualDescendants()
                .OfType<DataGridRow>()
                .Where(row => row.DataContext is IndexedItem)
                .ToList();
            Assert.NotEmpty(displayedRows);
            Assert.Contains(
                displayedRows.SelectMany(row => row.GetVisualDescendants().OfType<DataGridCell>()),
                originalCells.Contains);
            foreach (var row in displayedRows)
            {
                var item = Assert.IsType<IndexedItem>(row.DataContext);
                var text = Assert.Single(row.GetVisualDescendants().OfType<TextBlock>());
                Assert.Equal(item.Fields[0], text.Text);
            }
        }
        finally
        {
            window.Close();
        }
    }

    private sealed class NotifyItem : INotifyPropertyChanged
    {
        private string _name;

        public NotifyItem(string name) => _name = name;

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

        public event PropertyChangedEventHandler? PropertyChanged;
    }

    private sealed class IndexedItem
    {
        public IndexedItem(string value) => Fields = new List<string> { value };

        public List<string> Fields { get; }
    }

    private sealed record NumericItem(decimal Value);

    private sealed record ImageItem(IImage? Image);

    private sealed class DrawnItem : INotifyPropertyChanged
    {
        private string _name;
        private decimal _number;

        public DrawnItem(string name, decimal number)
        {
            _name = name;
            _number = number;
        }

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

        public decimal Number
        {
            get => _number;
            set
            {
                if (_number == value)
                {
                    return;
                }

                _number = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Number)));
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
    }

    private sealed class TestTextColumn : DataGridTextColumn
    {
        public Control? GenerateDisplay(DataGridCell cell, object item) => GenerateElement(cell, item);

        public Control GenerateEditor(DataGridCell cell, object item) => GenerateEditingElementDirect(cell, item);
    }

    private sealed class TestNumericColumn : DataGridNumericColumn
    {
        public Control? GenerateDisplay(DataGridCell cell, object item) => GenerateElement(cell, item);

        public Control GenerateEditor(DataGridCell cell, object item) => GenerateEditingElementDirect(cell, item);
    }

    private sealed class TestProgressColumn : DataGridProgressBarColumn
    {
        public Control? GenerateDisplay(DataGridCell cell, object item) => GenerateElement(cell, item);
    }

    private sealed class TestImageColumn : DataGridImageColumn
    {
        public Control? GenerateDisplay(DataGridCell cell, object item) => GenerateElement(cell, item);

        public Control GenerateEditor(DataGridCell cell, object item) => GenerateEditingElementDirect(cell, item);
    }

    private sealed class TestCustomDrawingColumn : DataGridCustomDrawingColumn
    {
        public Control? GenerateDisplay(DataGridCell cell, object item) => GenerateElement(cell, item);
    }
}
