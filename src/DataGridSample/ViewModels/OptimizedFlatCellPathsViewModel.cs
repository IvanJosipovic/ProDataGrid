using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Avalonia.Controls;
using DataGridSample.Behaviors;
using DataGridSample.CustomDrawing;
using DataGridSample.Helpers;
using DataGridSample.Models;
using ReactiveUI;
using ReactiveUI.Primitives;

namespace DataGridSample.ViewModels;

public sealed class OptimizedFlatCellPathsViewModel : ReactiveObject
{
    private const int PreviewRowCount = 1_000;
    private const int MaximumRowCount = 1_000_000;

    private static readonly string[] s_categories =
        { "Compute", "Storage", "Network", "Identity", "Analytics", "Messaging" };
    private static readonly string[] s_owners =
        { "Atlas", "Beacon", "Cirrus", "Delta", "Ember", "Flux", "Gaia", "Helix" };
    private static readonly string[] s_regions =
        { "eu-central", "eu-west", "us-east", "us-west", "ap-south", "ap-northeast" };
    private static readonly string[] s_states =
        { "Healthy", "Busy", "Pending", "Throttled", "Recovering" };

    private readonly IReadOnlyDictionary<string, IReadOnlyList<DataGridColumnDefinition>> _columnsByPath;
    private IReadOnlyList<OptimizedCellSampleRow> _items;
    private IReadOnlyList<DataGridColumnDefinition> _activeColumns;
    private OptimizedCellPathOption _selectedPath;
    private OptimizedCellSampleRow? _selectedItem;
    private int _targetRowCount = 250_000;
    private string _summary;
    private string _managedMemorySummary;

    public OptimizedFlatCellPathsViewModel()
    {
        Paths = CreatePaths();
        var customDrawingFactory = new SkiaTextCellDrawOperationFactory
        {
            MetricsCacheCapacity = 8_192,
        };
        _columnsByPath = new Dictionary<string, IReadOnlyList<DataGridColumnDefinition>>(StringComparer.Ordinal)
        {
            ["standard"] = CreateColumns(FlatColumnPath.Standard, customDrawingFactory),
            ["optimized-theme"] = CreateColumns(FlatColumnPath.Standard, customDrawingFactory),
            ["direct-accessor"] = CreateColumns(FlatColumnPath.DirectAccessor, customDrawingFactory),
            ["direct-cell"] = CreateColumns(FlatColumnPath.DirectCell, customDrawingFactory),
            ["built-in-drawn"] = CreateColumns(FlatColumnPath.BuiltInDrawn, customDrawingFactory),
            ["custom-drawn"] = CreateColumns(FlatColumnPath.CustomDrawn, customDrawingFactory),
        };

        _selectedPath = Paths[0];
        _activeColumns = _columnsByPath[_selectedPath.Key];
        _items = CreateRows(PreviewRowCount);
        _summary = $"Preview: {_items.Count:n0} rows. Load the representative workload for manual profiling.";
        _managedMemorySummary = CreateManagedMemorySummary();

        LoadRepresentativeWorkloadCommand = ReactiveCommand.CreateFromTask(LoadRepresentativeWorkloadAsync);
        JumpToFirstCommand = ReactiveCommand.Create(JumpToFirst);
        JumpToMiddleCommand = ReactiveCommand.Create(JumpToMiddle);
        JumpToLastCommand = ReactiveCommand.Create(JumpToLast);
        RefreshManagedMemoryCommand = ReactiveCommand.Create(RefreshManagedMemory);
    }

    public IReadOnlyList<OptimizedCellPathOption> Paths { get; }

    public IReadOnlyList<OptimizedCellSampleRow> Items
    {
        get => _items;
        private set => this.RaiseAndSetIfChanged(ref _items, value);
    }

    public IReadOnlyList<DataGridColumnDefinition> ActiveColumns
    {
        get => _activeColumns;
        private set => this.RaiseAndSetIfChanged(ref _activeColumns, value);
    }

    public OptimizedCellPathOption SelectedPath
    {
        get => _selectedPath;
        set
        {
            if (value is null || ReferenceEquals(_selectedPath, value))
            {
                return;
            }

            this.RaiseAndSetIfChanged(ref _selectedPath, value);
            ActiveColumns = _columnsByPath[value.Key];
            this.RaisePropertyChanged(nameof(UseOptimizedTheme));
        }
    }

    public bool UseOptimizedTheme => SelectedPath.UsesOptimizedTheme;

    public OptimizedCellSampleRow? SelectedItem
    {
        get => _selectedItem;
        set => this.RaiseAndSetIfChanged(ref _selectedItem, value);
    }

    public int TargetRowCount
    {
        get => _targetRowCount;
        set => this.RaiseAndSetIfChanged(ref _targetRowCount, Math.Clamp(value, PreviewRowCount, MaximumRowCount));
    }

    public string Summary
    {
        get => _summary;
        private set => this.RaiseAndSetIfChanged(ref _summary, value);
    }

    public string ManagedMemorySummary
    {
        get => _managedMemorySummary;
        private set => this.RaiseAndSetIfChanged(ref _managedMemorySummary, value);
    }

    public ReactiveCommand<RxVoid, RxVoid> LoadRepresentativeWorkloadCommand { get; }

    public ReactiveCommand<RxVoid, RxVoid> JumpToFirstCommand { get; }

    public ReactiveCommand<RxVoid, RxVoid> JumpToMiddleCommand { get; }

    public ReactiveCommand<RxVoid, RxVoid> JumpToLastCommand { get; }

    public ReactiveCommand<RxVoid, RxVoid> RefreshManagedMemoryCommand { get; }

    public async Task LoadRepresentativeWorkloadAsync()
    {
        int count = TargetRowCount;
        Summary = $"Generating {count:n0} immutable rows off the UI thread...";
        var stopwatch = Stopwatch.StartNew();
        IReadOnlyList<OptimizedCellSampleRow> rows = await Task.Run(() => CreateRows(count));
        stopwatch.Stop();

        Items = rows;
        SelectedItem = null;
        Summary = $"Loaded {rows.Count:n0} rows in {stopwatch.Elapsed.TotalSeconds:n2}s. Scroll or use the equal-distance jumps.";
        RefreshManagedMemory();
    }

    private static IReadOnlyList<OptimizedCellPathOption> CreatePaths() =>
        new[]
        {
            new OptimizedCellPathOption(
                "standard",
                "Standard retained",
                "DataGridCell + normal retained TextBlock/content template",
                "DisplayMode=Retained; generic row, cell, and header themes",
                "Compatibility baseline for arbitrary templates, converters, validation, and dynamic resources.",
                false),
            new OptimizedCellPathOption(
                "optimized-theme",
                "Optimized retained theme",
                "DataGridCell + normal retained TextBlock",
                "DataGridOptimizedFeatureUnfrozenRowTheme + DataGridOptimizedCellTheme + optimized feature header",
                "Keeps Avalonia layout and standard bindings while removing redundant chrome and presenters.",
                true),
            new OptimizedCellPathOption(
                "direct-accessor",
                "Retained direct accessor",
                "DataGridCell + retained TextBlock",
                "UseDirectTextContent=True; typed accessors; immutable-value tracking disabled",
                "Keeps the normal retained content path but avoids one display binding expression per realized text cell.",
                true),
            new OptimizedCellPathOption(
                "direct-cell",
                "Direct retained text cell",
                "DataGridDirectTextCell",
                "UseDirectTextCell=True; typed accessors; immutable-value tracking disabled",
                "Coalesces the retained cell and text presentation while preserving selection, automation, and editing.",
                true),
            new OptimizedCellPathOption(
                "built-in-drawn",
                "Built-in drawn text",
                "DataGridCustomDrawingCell",
                "DisplayMode=Drawn; typed accessors; retained editor on edit",
                "Uses the built-in drawn display renderer for the smallest realized display tree.",
                true),
            new OptimizedCellPathOption(
                "custom-drawn",
                "Custom Skia draw operation",
                "DataGridCustomDrawingCell",
                "DataGridCustomDrawingColumn + direct accessor + cached Skia draw-operation metrics",
                "Shows the extensible custom-rendering path with shared bounded text measurement caching.",
                true),
        };

    private static IReadOnlyList<DataGridColumnDefinition> CreateColumns(
        FlatColumnPath path,
        SkiaTextCellDrawOperationFactory customDrawingFactory) =>
        new[]
        {
            CreateColumn("ID", nameof(OptimizedCellSampleRow.Id), static row => row.Id, 0.7, path, customDrawingFactory),
            CreateColumn("Name", nameof(OptimizedCellSampleRow.Name), static row => row.Name, 1.4, path, customDrawingFactory),
            CreateColumn("Category", nameof(OptimizedCellSampleRow.Category), static row => row.Category, 1.0, path, customDrawingFactory),
            CreateColumn("Owner", nameof(OptimizedCellSampleRow.Owner), static row => row.Owner, 1.0, path, customDrawingFactory),
            CreateColumn("Region", nameof(OptimizedCellSampleRow.Region), static row => row.Region, 1.0, path, customDrawingFactory),
            CreateColumn("State", nameof(OptimizedCellSampleRow.State), static row => row.State, 1.0, path, customDrawingFactory),
            CreateColumn("Detail", nameof(OptimizedCellSampleRow.Detail), static row => row.Detail, 2.4, path, customDrawingFactory),
        };

    private static DataGridColumnDefinition CreateColumn(
        string header,
        string propertyName,
        Func<OptimizedCellSampleRow, string> getter,
        double width,
        FlatColumnPath path,
        SkiaTextCellDrawOperationFactory customDrawingFactory)
    {
        DataGridBindingDefinition binding = ColumnDefinitionBindingFactory.CreateBinding(propertyName, getter);
        if (path == FlatColumnPath.CustomDrawn)
        {
            return new DataGridCustomDrawingColumnDefinition
            {
                Header = header,
                Binding = binding,
                Width = new DataGridLength(width, DataGridLengthUnitType.Star),
                IsReadOnly = true,
                UseDirectValueAccessor = true,
                TrackDirectValueChanges = false,
                DrawingMode = DataGridCustomDrawingMode.DrawOperation,
                RenderBackend = DataGridCustomDrawingRenderBackend.CompositionCustomVisual,
                DrawOperationFactory = customDrawingFactory,
                DrawOperationLayoutFastPath = true,
                TextLayoutCacheMode = DataGridCustomDrawingTextLayoutCacheMode.Shared,
                SharedTextLayoutCacheCapacity = 4_096,
            };
        }

        return new DataGridTextColumnDefinition
        {
            Header = header,
            Binding = binding,
            Width = new DataGridLength(width, DataGridLengthUnitType.Star),
            IsReadOnly = true,
            DisplayMode = path == FlatColumnPath.BuiltInDrawn
                ? DataGridColumnDisplayMode.Drawn
                : DataGridColumnDisplayMode.Retained,
            UseDirectTextContent = path == FlatColumnPath.DirectAccessor,
            UseDirectTextCell = path == FlatColumnPath.DirectCell,
            TrackDirectTextValueChanges = false,
        };
    }

    private static IReadOnlyList<OptimizedCellSampleRow> CreateRows(int count)
    {
        var rows = new OptimizedCellSampleRow[count];
        for (int index = 0; index < rows.Length; index++)
        {
            int id = index + 1;
            string category = s_categories[index % s_categories.Length];
            string owner = s_owners[(index / s_categories.Length) % s_owners.Length];
            string region = s_regions[(index / 11) % s_regions.Length];
            string state = s_states[(index / 17) % s_states.Length];
            rows[index] = new OptimizedCellSampleRow(
                id.ToString("D7"),
                $"Work item {id:D7}",
                category,
                owner,
                region,
                state,
                $"{category} workload shard {id % 4_096:D4} owned by {owner}");
        }

        return rows;
    }

    private void JumpToFirst() => SelectedItem = Items.Count == 0 ? null : Items[0];

    private void JumpToMiddle() => SelectedItem = Items.Count == 0 ? null : Items[Items.Count / 2];

    private void JumpToLast() => SelectedItem = Items.Count == 0 ? null : Items[^1];

    private void RefreshManagedMemory() => ManagedMemorySummary = CreateManagedMemorySummary();

    private static string CreateManagedMemorySummary() =>
        $"Process managed heap snapshot: {GC.GetTotalMemory(false) / (1024d * 1024d):n1} MiB";

    private enum FlatColumnPath
    {
        Standard,
        DirectAccessor,
        DirectCell,
        BuiltInDrawn,
        CustomDrawn,
    }
}

public sealed class OptimizedFlatCellPathsViewModelFactory : IDataContextFactory
{
    public object Create() => new OptimizedFlatCellPathsViewModel();
}
