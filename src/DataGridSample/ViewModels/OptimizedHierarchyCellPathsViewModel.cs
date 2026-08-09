using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Controls.DataGridHierarchical;
using DataGridSample.Behaviors;
using DataGridSample.CustomDrawing;
using DataGridSample.Helpers;
using DataGridSample.Models;
using ReactiveUI;
using ReactiveUI.Primitives;

namespace DataGridSample.ViewModels;

public sealed class OptimizedHierarchyCellPathsViewModel : ReactiveObject
{
    private const long MaximumNodeCount = 1_000_000;

    private static readonly string[] s_kinds =
        { "Portfolio", "Program", "Service", "Workload", "Instance", "Shard" };
    private static readonly string[] s_owners =
        { "Atlas", "Beacon", "Cirrus", "Delta", "Ember", "Flux", "Gaia", "Helix" };
    private static readonly string[] s_regions =
        { "eu-central", "eu-west", "us-east", "us-west", "ap-south", "ap-northeast" };
    private static readonly string[] s_states =
        { "Healthy", "Busy", "Pending", "Throttled", "Recovering" };

    private readonly IReadOnlyDictionary<string, IReadOnlyList<DataGridColumnDefinition>> _columnsByPath;
    private HierarchicalModel<OptimizedHierarchyCellSampleNode> _model;
    private IReadOnlyList<DataGridColumnDefinition> _activeColumns;
    private OptimizedCellPathOption _selectedPath;
    private OptimizedHierarchyCellSampleNode? _selectedItem;
    private int _rootCount = 32;
    private int _branchingFactor = 8;
    private int _depth = 4;
    private int _totalNodeCount;
    private string _summary;
    private string _managedMemorySummary;

    public OptimizedHierarchyCellPathsViewModel()
    {
        Paths = CreatePaths();
        var customDrawingFactory = new SkiaTextCellDrawOperationFactory
        {
            MetricsCacheCapacity = 8_192,
        };
        _columnsByPath = new Dictionary<string, IReadOnlyList<DataGridColumnDefinition>>(StringComparer.Ordinal)
        {
            ["standard"] = CreateColumns(HierarchyColumnPath.Standard, customDrawingFactory),
            ["optimized-theme"] = CreateColumns(HierarchyColumnPath.Standard, customDrawingFactory),
            ["optimized-presenter"] = CreateColumns(HierarchyColumnPath.OptimizedPresenter, customDrawingFactory),
            ["direct-hierarchy"] = CreateColumns(HierarchyColumnPath.DirectHierarchy, customDrawingFactory),
            ["built-in-drawn"] = CreateColumns(HierarchyColumnPath.BuiltInDrawn, customDrawingFactory),
            ["custom-drawn"] = CreateColumns(HierarchyColumnPath.CustomDrawn, customDrawingFactory),
        };

        _selectedPath = Paths[0];
        _activeColumns = _columnsByPath[_selectedPath.Key];

        (IReadOnlyList<OptimizedHierarchyCellSampleNode> previewRoots, int previewCount) =
            CreateTree(rootCount: 4, branchingFactor: 4, depth: 3);
        _model = CreateModel(previewRoots);
        _totalNodeCount = previewCount;
        _summary = $"Preview: {_totalNodeCount:n0} total nodes, {_model.Flattened.Count:n0} visible. Load the representative workload for profiling.";
        _managedMemorySummary = CreateManagedMemorySummary();

        LoadRepresentativeWorkloadCommand = ReactiveCommand.CreateFromTask(LoadRepresentativeWorkloadAsync);
        ExpandAllCommand = ReactiveCommand.Create(ExpandAll);
        CollapseAllCommand = ReactiveCommand.Create(CollapseAll);
        JumpToFirstCommand = ReactiveCommand.Create(JumpToFirst);
        JumpToMiddleCommand = ReactiveCommand.Create(JumpToMiddle);
        JumpToLastCommand = ReactiveCommand.Create(JumpToLast);
        RefreshManagedMemoryCommand = ReactiveCommand.Create(RefreshManagedMemory);
    }

    public IReadOnlyList<OptimizedCellPathOption> Paths { get; }

    public HierarchicalModel<OptimizedHierarchyCellSampleNode> Model
    {
        get => _model;
        private set
        {
            this.RaiseAndSetIfChanged(ref _model, value);
            this.RaisePropertyChanged(nameof(VisibleNodeSummary));
        }
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

    public OptimizedHierarchyCellSampleNode? SelectedItem
    {
        get => _selectedItem;
        set => this.RaiseAndSetIfChanged(ref _selectedItem, value);
    }

    public int RootCount
    {
        get => _rootCount;
        set
        {
            int next = Math.Clamp(value, 1, 128);
            if (_rootCount != next)
            {
                this.RaiseAndSetIfChanged(ref _rootCount, next);
                this.RaisePropertyChanged(nameof(TargetNodeSummary));
            }
        }
    }

    public int BranchingFactor
    {
        get => _branchingFactor;
        set
        {
            int next = Math.Clamp(value, 2, 12);
            if (_branchingFactor != next)
            {
                this.RaiseAndSetIfChanged(ref _branchingFactor, next);
                this.RaisePropertyChanged(nameof(TargetNodeSummary));
            }
        }
    }

    public int Depth
    {
        get => _depth;
        set
        {
            int next = Math.Clamp(value, 1, 5);
            if (_depth != next)
            {
                this.RaiseAndSetIfChanged(ref _depth, next);
                this.RaisePropertyChanged(nameof(TargetNodeSummary));
            }
        }
    }

    public string TargetNodeSummary
    {
        get
        {
            long count = CalculateNodeCount(RootCount, BranchingFactor, Depth);
            return count > MaximumNodeCount
                ? $"Target: {count:n0} nodes (reduce dimensions below {MaximumNodeCount:n0})"
                : $"Target: {count:n0} nodes";
        }
    }

    public string VisibleNodeSummary =>
        $"Visible: {Model.Flattened.Count:n0} / {_totalNodeCount:n0} nodes";

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

    public ReactiveCommand<RxVoid, RxVoid> ExpandAllCommand { get; }

    public ReactiveCommand<RxVoid, RxVoid> CollapseAllCommand { get; }

    public ReactiveCommand<RxVoid, RxVoid> JumpToFirstCommand { get; }

    public ReactiveCommand<RxVoid, RxVoid> JumpToMiddleCommand { get; }

    public ReactiveCommand<RxVoid, RxVoid> JumpToLastCommand { get; }

    public ReactiveCommand<RxVoid, RxVoid> RefreshManagedMemoryCommand { get; }

    public async Task LoadRepresentativeWorkloadAsync()
    {
        int roots = RootCount;
        int branching = BranchingFactor;
        int depth = Depth;
        long requestedCount = CalculateNodeCount(roots, branching, depth);
        if (requestedCount > MaximumNodeCount)
        {
            Summary = $"Requested tree has {requestedCount:n0} nodes. Reduce the dimensions to {MaximumNodeCount:n0} nodes or fewer.";
            return;
        }

        Summary = $"Generating {requestedCount:n0} immutable hierarchy nodes off the UI thread...";
        var stopwatch = Stopwatch.StartNew();
        (IReadOnlyList<OptimizedHierarchyCellSampleNode> generatedRoots, int generatedCount) =
            await Task.Run(() => CreateTree(roots, branching, depth));
        var model = CreateModel(generatedRoots);
        stopwatch.Stop();

        Model = model;
        _totalNodeCount = generatedCount;
        SelectedItem = null;
        this.RaisePropertyChanged(nameof(VisibleNodeSummary));
        Summary = $"Loaded {generatedCount:n0} nodes in {stopwatch.Elapsed.TotalSeconds:n2}s. Roots start expanded; use Expand all for the full visible workload.";
        RefreshManagedMemory();
    }

    private static IReadOnlyList<OptimizedCellPathOption> CreatePaths() =>
        new[]
        {
            new OptimizedCellPathOption(
                "standard",
                "Standard retained",
                "DataGridCell + DataGridHierarchicalPresenter + retained companion cells",
                "DisplayMode=Retained; generic row, cell, and header themes",
                "Compatibility baseline with the normal hierarchy presenter and binding expressions.",
                false),
            new OptimizedCellPathOption(
                "optimized-theme",
                "Optimized retained theme",
                "DataGridCell + DataGridHierarchicalPresenter + retained companion cells",
                "Optimized feature row/cell/header themes; standard hierarchy presenter",
                "Keeps the standard hierarchy and Avalonia layout path while reducing surrounding visual chrome.",
                true),
            new OptimizedCellPathOption(
                "optimized-presenter",
                "Optimized hierarchy presenter",
                "DataGridDirectHierarchicalCell with retained TextBlock + DataGridCell companions",
                "UseOptimizedPresenter=True; UseDirectTextContent=True; typed immutable accessors",
                "Combines the hierarchy cell and expander roles while retaining normal text controls and direct-accessor companion content.",
                true),
            new OptimizedCellPathOption(
                "direct-hierarchy",
                "Direct retained hierarchy",
                "DataGridDirectHierarchicalCell + DataGridDirectTextCell companions",
                "UseDirectCell=True; UseDirectTextContent=True; companion UseDirectTextCell=True",
                "Uses the leanest retained hierarchy container and coalesced retained companion text cells.",
                true),
            new OptimizedCellPathOption(
                "built-in-drawn",
                "Direct hierarchy + built-in drawn",
                "DataGridDirectHierarchicalCell + DataGridCustomDrawingCell companions",
                "Direct retained hierarchy column; companion DisplayMode=Drawn",
                "Keeps retained expander input and automation while drawing the read-only companion values.",
                true),
            new OptimizedCellPathOption(
                "custom-drawn",
                "Direct hierarchy + custom Skia",
                "DataGridDirectHierarchicalCell + custom DataGridCustomDrawingCell companions",
                "Direct retained hierarchy column; custom direct-accessor Skia companion columns",
                "Uses cached custom draw operations for companion values while preserving the retained hierarchy expander.",
                true),
        };

    private static IReadOnlyList<DataGridColumnDefinition> CreateColumns(
        HierarchyColumnPath path,
        SkiaTextCellDrawOperationFactory customDrawingFactory)
    {
        var columns = new List<DataGridColumnDefinition>(6)
        {
            CreateHierarchyColumn(path),
            CreateCompanionColumn("Kind", nameof(OptimizedHierarchyCellSampleNode.Kind), static item => item.Kind, 0.9, path, customDrawingFactory),
            CreateCompanionColumn("Owner", nameof(OptimizedHierarchyCellSampleNode.Owner), static item => item.Owner, 0.9, path, customDrawingFactory),
            CreateCompanionColumn("Region", nameof(OptimizedHierarchyCellSampleNode.Region), static item => item.Region, 1.0, path, customDrawingFactory),
            CreateCompanionColumn("State", nameof(OptimizedHierarchyCellSampleNode.State), static item => item.State, 0.9, path, customDrawingFactory),
            CreateCompanionColumn("Detail", nameof(OptimizedHierarchyCellSampleNode.Detail), static item => item.Detail, 2.2, path, customDrawingFactory),
        };
        return columns;
    }

    private static DataGridColumnDefinition CreateHierarchyColumn(HierarchyColumnPath path)
    {
        DataGridBindingDefinition binding = CreateNodeBinding(
            nameof(OptimizedHierarchyCellSampleNode.Name),
            static item => item.Name);
        return new DataGridHierarchicalColumnDefinition
        {
            Header = "Name",
            Binding = binding,
            Width = new DataGridLength(1.8, DataGridLengthUnitType.Star),
            IsReadOnly = true,
            UseOptimizedPresenter = path == HierarchyColumnPath.OptimizedPresenter,
            UseDirectCell = path is HierarchyColumnPath.DirectHierarchy or
                HierarchyColumnPath.BuiltInDrawn or HierarchyColumnPath.CustomDrawn,
            UseDirectTextContent = path is not HierarchyColumnPath.Standard,
            TrackDirectTextValueChanges = false,
        };
    }

    private static DataGridColumnDefinition CreateCompanionColumn(
        string header,
        string propertyName,
        Func<OptimizedHierarchyCellSampleNode, string> getter,
        double width,
        HierarchyColumnPath path,
        SkiaTextCellDrawOperationFactory customDrawingFactory)
    {
        DataGridBindingDefinition binding = CreateNodeBinding(propertyName, getter);
        if (path == HierarchyColumnPath.CustomDrawn)
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
            DisplayMode = path == HierarchyColumnPath.BuiltInDrawn
                ? DataGridColumnDisplayMode.Drawn
                : DataGridColumnDisplayMode.Retained,
            UseDirectTextContent = path == HierarchyColumnPath.OptimizedPresenter,
            UseDirectTextCell = path == HierarchyColumnPath.DirectHierarchy,
            TrackDirectTextValueChanges = false,
        };
    }

    private static DataGridBindingDefinition CreateNodeBinding(
        string propertyName,
        Func<OptimizedHierarchyCellSampleNode, string> getter) =>
        ColumnDefinitionBindingFactory.CreateBinding<HierarchicalNode, string>(
            propertyName,
            node => getter((OptimizedHierarchyCellSampleNode)node.Item));

    private static HierarchicalModel<OptimizedHierarchyCellSampleNode> CreateModel(
        IReadOnlyList<OptimizedHierarchyCellSampleNode> roots)
    {
        var model = new HierarchicalModel<OptimizedHierarchyCellSampleNode>(
            new HierarchicalOptions<OptimizedHierarchyCellSampleNode>
            {
                ChildrenSelector = static item => item.Children,
                IsExpandedSelector = static item => item.IsExpanded,
                IsExpandedSetter = static (item, value) => item.IsExpanded = value,
                VirtualizeChildren = false,
            });
        model.SetRoots(roots);
        return model;
    }

    private static (IReadOnlyList<OptimizedHierarchyCellSampleNode> Roots, int Count) CreateTree(
        int rootCount,
        int branchingFactor,
        int depth)
    {
        var roots = new OptimizedHierarchyCellSampleNode[rootCount];
        int sequence = 0;
        for (int rootIndex = 0; rootIndex < rootCount; rootIndex++)
        {
            roots[rootIndex] = CreateNode(
                level: 0,
                maxDepth: depth,
                branchingFactor,
                rootIndex,
                ref sequence);
        }

        return (roots, sequence);
    }

    private static OptimizedHierarchyCellSampleNode CreateNode(
        int level,
        int maxDepth,
        int branchingFactor,
        int rootIndex,
        ref int sequence)
    {
        int id = ++sequence;
        IReadOnlyList<OptimizedHierarchyCellSampleNode> children;
        if (level >= maxDepth)
        {
            children = Array.Empty<OptimizedHierarchyCellSampleNode>();
        }
        else
        {
            var childArray = new OptimizedHierarchyCellSampleNode[branchingFactor];
            for (int childIndex = 0; childIndex < childArray.Length; childIndex++)
            {
                childArray[childIndex] = CreateNode(
                    level + 1,
                    maxDepth,
                    branchingFactor,
                    rootIndex,
                    ref sequence);
            }

            children = childArray;
        }

        string kind = s_kinds[Math.Min(level, s_kinds.Length - 1)];
        string owner = s_owners[(id + rootIndex) % s_owners.Length];
        string region = s_regions[(id / 7) % s_regions.Length];
        string state = s_states[(id / 13) % s_states.Length];
        return new OptimizedHierarchyCellSampleNode(
            $"{kind} {id:D7}",
            kind,
            owner,
            region,
            state,
            $"Root {rootIndex + 1:D3}, level {level}, shard {id % 4_096:D4}",
            children,
            isExpanded: level == 0);
    }

    private static long CalculateNodeCount(int rootCount, int branchingFactor, int depth)
    {
        long nodesPerRoot = 1;
        long levelCount = 1;
        for (int level = 1; level <= depth; level++)
        {
            levelCount *= branchingFactor;
            nodesPerRoot += levelCount;
        }

        return nodesPerRoot * rootCount;
    }

    private void ExpandAll()
    {
        var stopwatch = Stopwatch.StartNew();
        Model.ExpandAll();
        stopwatch.Stop();
        this.RaisePropertyChanged(nameof(VisibleNodeSummary));
        Summary = $"Expand all dispatched in {stopwatch.Elapsed.TotalMilliseconds:n1} ms; layout and rendering complete on subsequent frames.";
        RefreshManagedMemory();
    }

    private void CollapseAll()
    {
        var stopwatch = Stopwatch.StartNew();
        Model.CollapseAll();
        stopwatch.Stop();
        this.RaisePropertyChanged(nameof(VisibleNodeSummary));
        Summary = $"Collapse all dispatched in {stopwatch.Elapsed.TotalMilliseconds:n1} ms; layout and rendering complete on subsequent frames.";
    }

    private void JumpToFirst() =>
        SelectedItem = Model.Flattened.Count == 0 ? null : Model.Flattened[0].Item;

    private void JumpToMiddle() =>
        SelectedItem = Model.Flattened.Count == 0 ? null : Model.Flattened[Model.Flattened.Count / 2].Item;

    private void JumpToLast() =>
        SelectedItem = Model.Flattened.Count == 0 ? null : Model.Flattened[^1].Item;

    private void RefreshManagedMemory() => ManagedMemorySummary = CreateManagedMemorySummary();

    private static string CreateManagedMemorySummary() =>
        $"Process managed heap snapshot: {GC.GetTotalMemory(false) / (1024d * 1024d):n1} MiB";

    private enum HierarchyColumnPath
    {
        Standard,
        OptimizedPresenter,
        DirectHierarchy,
        BuiltInDrawn,
        CustomDrawn,
    }
}

public sealed class OptimizedHierarchyCellPathsViewModelFactory : IDataContextFactory
{
    public object Create() => new OptimizedHierarchyCellPathsViewModel();
}
