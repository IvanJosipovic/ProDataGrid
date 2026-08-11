using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.Json;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Markup.Xaml.Styling;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Themes.Fluent;
using Avalonia.Threading;
using Avalonia.VisualTree;
using GridHierarchyBenchmarks.Shared;
#if PRO
using Avalonia.Controls.DataGridHierarchical;
#else
using TreeDataGridControl = Avalonia.Controls.TreeDataGrid;
#endif

namespace GridHierarchyBenchmarks.Native;

internal static class Program
{
    [STAThread]
    public static int Main(string[] args)
    {
        NativeBenchmarkOptions.Initialize(args);
        return BuildAvaloniaApp().StartWithClassicDesktopLifetime(
            Array.Empty<string>(),
            ShutdownMode.OnExplicitShutdown);
    }

    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<NativeBenchmarkApplication>()
            .UsePlatformDetect();
}

internal sealed class NativeBenchmarkApplication : Application
{
    private NativeGridHandle? _inspectionHandle;

    public override void Initialize()
    {
        RequestedThemeVariant = ThemeVariant.Light;
        Styles.Add(new FluentTheme());
#if PRO
        Styles.Add(new StyleInclude(new Uri("avares://Avalonia.Controls.DataGrid/Themes/"))
        {
            Source = new Uri("avares://Avalonia.Controls.DataGrid/Themes/Fluent.v2.xaml"),
        });
#else
        Styles.Add(new StyleInclude(new Uri("avares://Avalonia.Controls.TreeDataGrid/Themes/"))
        {
            Source = new Uri("avares://Avalonia.Controls.TreeDataGrid/Themes/Fluent.axaml"),
        });
#endif
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.ShutdownMode = ShutdownMode.OnExplicitShutdown;
            var host = NativeGridAdapter.CreateHostWindow();
            desktop.MainWindow = host;

            if (NativeBenchmarkOptions.Inspect)
            {
                _inspectionHandle = NativeGridAdapter.Create(expanded: true);
                host.Content = _inspectionHandle.Grid;
                host.Closed += (_, _) =>
                {
                    _inspectionHandle?.Dispose();
                    _inspectionHandle = null;
                    desktop.Shutdown();
                };
                host.Opened += (_, _) => Dispatcher.UIThread.Post(
                    () => RunInspectionAsync(host),
                    DispatcherPriority.Background);
            }
            else
            {
                host.Opened += (_, _) => Dispatcher.UIThread.Post(
                    () => RunBenchmarkAsync(host, desktop),
                    DispatcherPriority.Background);
            }
        }

        base.OnFrameworkInitializationCompleted();
    }

    private async void RunInspectionAsync(Window host)
    {
        try
        {
            await NativeBenchmarkRunner.WaitForRenderedFrameAsync(host);
            host.UpdateLayout();
            var validation = NativeGridAdapter.Validate(_inspectionHandle!);
            Console.WriteLine(
                $"NATIVE_INSPECT_READY implementation={NativeBenchmarkOptions.ImplementationName} " +
                $"pid={Environment.ProcessId} rows={validation.RealizedRows} cells={validation.RealizedCells} " +
                $"extent={validation.ExtentWidth:F0}x{validation.ExtentHeight:F0} scaling={host.RenderScaling:F2}");
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex);
            host.Close();
        }
    }

    private static async void RunBenchmarkAsync(
        Window host,
        IClassicDesktopStyleApplicationLifetime desktop)
    {
        try
        {
            await NativeBenchmarkRunner.WaitForRenderedFrameAsync(host);
            var result = await new NativeBenchmarkRunner(host).RunAsync();
            await NativeBenchmarkResultWriter.WriteAsync(result, NativeBenchmarkOptions.OutputPath);
            Console.WriteLine(JsonSerializer.Serialize(result, NativeBenchmarkResultWriter.JsonOptions));
            host.Close();
            desktop.Shutdown(0);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex);
            host.Close();
            desktop.Shutdown(1);
        }
    }
}

internal static class NativeBenchmarkOptions
{
    public static bool Inspect { get; private set; }

    public static bool DrawnCells { get; private set; }

    public static int WarmupIterations { get; private set; } = 2;

    public static int Iterations { get; private set; } = 5;

    public static int ScrollJumps { get; private set; } = 32;

    public static bool FirstRenderOnly { get; private set; }

    public static string OutputPath { get; private set; } = Path.GetFullPath("native-result.json");

#if PRO && PRODATAGRID_PR335
    public static string ProMode { get; } =
        (Environment.GetEnvironmentVariable("GRID_BENCH_PRO_MODE") ?? "standard")
        .Trim()
        .ToLowerInvariant();
#endif

    public static string ImplementationName =>
#if PRO
#if PRODATAGRID_PR335
        ProMode switch
        {
            "standard" => "ProDataGrid (legacy default retained)",
            "optimized" => "ProDataGrid (optimized retained)",
            "direct" => "ProDataGrid (direct-content retained)",
            "direct-cell" => "ProDataGrid (direct-cell retained)",
            "drawn" => "ProDataGrid (drawn ordinary cells)",
            _ => $"ProDataGrid ({ProMode})",
        };
#else
        "ProDataGrid";
#endif
#elif ACCELERATE
        DrawnCells ? "Accelerate TreeDataGrid (drawn cells)" : "Accelerate TreeDataGrid (retained cells)";
#else
        "Wieslaw's TreeDataGrid";
#endif

#if PRO && PRODATAGRID_PR335
    public static string PerformanceMode => ProMode;
#endif

    public static void Initialize(string[] args)
    {
        for (var i = 0; i < args.Length; ++i)
        {
            switch (args[i])
            {
                case "--inspect":
                    Inspect = true;
                    break;
                case "--drawn":
                    DrawnCells = bool.Parse(RequireValue(args, ref i));
                    break;
                case "--output":
                    OutputPath = Path.GetFullPath(RequireValue(args, ref i));
                    break;
                case "--warmup":
                    WarmupIterations = ParsePositive(RequireValue(args, ref i), "warmup iterations", allowZero: true);
                    break;
                case "--iterations":
                    Iterations = ParsePositive(RequireValue(args, ref i), "iterations", allowZero: false);
                    break;
                case "--scroll-jumps":
                    ScrollJumps = ParsePositive(RequireValue(args, ref i), "scroll jumps", allowZero: false);
                    break;
                case "--first-render-only":
                    FirstRenderOnly = true;
                    break;
            }
        }

#if !ACCELERATE
        if (DrawnCells)
            throw new ArgumentException("--drawn true is supported only by Accelerate TreeDataGrid.");
#endif
#if PRO && PRODATAGRID_PR335
        if (ProMode is not ("standard" or "optimized" or "direct" or "direct-cell" or "drawn"))
            throw new ArgumentException($"Unsupported GRID_BENCH_PRO_MODE: {ProMode}.");
#endif
    }

    private static string RequireValue(string[] args, ref int index)
    {
        if (++index >= args.Length)
            throw new ArgumentException($"Missing value for {args[index - 1]}.");
        return args[index];
    }

    private static int ParsePositive(string value, string name, bool allowZero)
    {
        if (!int.TryParse(value, out var result) || result < (allowZero ? 0 : 1))
            throw new ArgumentException($"Invalid {name}: {value}.");
        return result;
    }
}

internal sealed class NativeBenchmarkRunner
{
    private readonly Window _host;

    public NativeBenchmarkRunner(Window host)
    {
        _host = host;
    }

    public async Task<NativeBenchmarkResult> RunAsync()
    {
        var firstRender = await MeasureFirstRenderAsync();
        var expandAndRender = NativeBenchmarkOptions.FirstRenderOnly
            ? null
            : await MeasureExpandAndRenderAsync();
        var collapseAndRender = NativeBenchmarkOptions.FirstRenderOnly
            ? null
            : await MeasureCollapseAndRenderAsync();
        var scrollAndRender = NativeBenchmarkOptions.FirstRenderOnly
            ? null
            : await MeasureScrollAndRenderAsync();

        return new NativeBenchmarkResult
        {
            TimestampUtc = DateTimeOffset.UtcNow,
            Implementation = NativeBenchmarkOptions.ImplementationName,
#if PRO && PRODATAGRID_PR335
            Mode = NativeBenchmarkOptions.PerformanceMode,
#else
            Mode = NativeBenchmarkOptions.DrawnCells ? "drawn" : "retained",
#endif
            AvaloniaVersion = typeof(Application).Assembly.GetName().Version?.ToString() ?? "unknown",
            Runtime = RuntimeInformation.FrameworkDescription,
            OS = RuntimeInformation.OSDescription,
            Architecture = RuntimeInformation.ProcessArchitecture.ToString(),
            Processor = Environment.GetEnvironmentVariable("PROCESSOR_IDENTIFIER") ?? "unknown",
            LogicalProcessorCount = Environment.ProcessorCount,
            ClientWidth = _host.ClientSize.Width,
            ClientHeight = _host.ClientSize.Height,
            RenderScaling = _host.RenderScaling,
            WarmupIterations = NativeBenchmarkOptions.WarmupIterations,
            MeasuredIterations = NativeBenchmarkOptions.Iterations,
            ScrollJumpsPerIteration = NativeBenchmarkOptions.ScrollJumps,
            FirstExpandedRender = firstRender,
            ExpandAllAndRender = expandAndRender,
            CollapseAllAndRender = collapseAndRender,
            ScrollAndRender = scrollAndRender,
        };
    }

    private async Task<NativeWorkloadResult> MeasureFirstRenderAsync()
    {
        for (var i = 0; i < NativeBenchmarkOptions.WarmupIterations; ++i)
            await FirstRenderIterationAsync(measure: false);

        var times = new List<double>();
        var allocations = new List<double>();
        NativeValidation? validation = null;

        for (var i = 0; i < NativeBenchmarkOptions.Iterations; ++i)
        {
            var measurement = await FirstRenderIterationAsync(measure: true);
            times.Add(measurement.ElapsedMilliseconds);
            allocations.Add(measurement.AllocatedBytes);
            validation = measurement.Validation;
        }

        return NativeWorkloadResult.Create("FirstExpandedRender", times, allocations, validation!);
    }

    private async Task<NativeMeasurement> FirstRenderIterationAsync(bool measure)
    {
        using var handle = NativeGridAdapter.Create(expanded: true);
        if (measure)
            CollectForMeasurement();

        var allocatedBefore = measure ? GC.GetTotalAllocatedBytes(precise: false) : 0;
        var stopwatch = Stopwatch.StartNew();
        _host.Content = handle.Grid;
        _host.UpdateLayout();
        await WaitForRenderedFrameAsync(_host);
        stopwatch.Stop();
        var allocated = measure ? GC.GetTotalAllocatedBytes(precise: false) - allocatedBefore : 0;
        var validation = NativeGridAdapter.Validate(handle);
        await DetachAsync();

        return new NativeMeasurement(stopwatch.Elapsed.TotalMilliseconds, allocated, validation);
    }

    private async Task<NativeWorkloadResult> MeasureExpandAndRenderAsync()
    {
        for (var i = 0; i < NativeBenchmarkOptions.WarmupIterations; ++i)
            await ExpandAndRenderIterationAsync(measure: false);

        var times = new List<double>();
        var allocations = new List<double>();
        NativeValidation? validation = null;

        for (var i = 0; i < NativeBenchmarkOptions.Iterations; ++i)
        {
            var measurement = await ExpandAndRenderIterationAsync(measure: true);
            times.Add(measurement.ElapsedMilliseconds);
            allocations.Add(measurement.AllocatedBytes);
            validation = measurement.Validation;
        }

        return NativeWorkloadResult.Create("ExpandAllAndRender", times, allocations, validation!);
    }

    private async Task<NativeMeasurement> ExpandAndRenderIterationAsync(bool measure)
    {
        using var handle = NativeGridAdapter.Create(expanded: false);
        _host.Content = handle.Grid;
        _host.UpdateLayout();
        await WaitForRenderedFrameAsync(_host);

        if (measure)
            CollectForMeasurement();

        var allocatedBefore = measure ? GC.GetTotalAllocatedBytes(precise: false) : 0;
        var stopwatch = Stopwatch.StartNew();
        handle.ExpandAll();
        _host.UpdateLayout();
        await WaitForRenderedFrameAsync(_host);
        stopwatch.Stop();
        var allocated = measure ? GC.GetTotalAllocatedBytes(precise: false) - allocatedBefore : 0;
        var validation = NativeGridAdapter.Validate(handle);
        await DetachAsync();

        return new NativeMeasurement(stopwatch.Elapsed.TotalMilliseconds, allocated, validation);
    }

    private async Task<NativeWorkloadResult> MeasureCollapseAndRenderAsync()
    {
        for (var i = 0; i < NativeBenchmarkOptions.WarmupIterations; ++i)
            await CollapseAndRenderIterationAsync(measure: false);

        var times = new List<double>();
        var allocations = new List<double>();
        var mutationTimes = new List<double>();
        var layoutTimes = new List<double>();
        var frameTimes = new List<double>();
        NativeValidation? validation = null;

        for (var i = 0; i < NativeBenchmarkOptions.Iterations; ++i)
        {
            var measurement = await CollapseAndRenderIterationAsync(measure: true);
            times.Add(measurement.ElapsedMilliseconds);
            allocations.Add(measurement.AllocatedBytes);
            mutationTimes.Add(measurement.MutationMilliseconds);
            layoutTimes.Add(measurement.LayoutMilliseconds);
            frameTimes.Add(measurement.FrameMilliseconds);
            validation = measurement.Validation;
        }

        return NativeWorkloadResult.Create(
            "CollapseAllAndRender",
            times,
            allocations,
            validation!,
            mutationTimes,
            layoutTimes,
            frameTimes);
    }

    private async Task<NativeMeasurement> CollapseAndRenderIterationAsync(bool measure)
    {
        const TreeShape shape = TreeShape.OptimizedSample149792Depth5;
        using var handle = NativeGridAdapter.Create(
            expanded: true,
            shape: shape,
            retainCollapsedChildren: true);
        _host.Content = handle.Grid;
        _host.UpdateLayout();
        await WaitForRenderedFrameAsync(_host);
        NativeGridAdapter.Validate(handle, TreeDataFactory.ExpectedCount(shape));

        if (measure)
            CollectForMeasurement();

        var allocatedBefore = measure ? GC.GetTotalAllocatedBytes(precise: false) : 0;
        var stopwatch = Stopwatch.StartNew();
        handle.CollapseAll();
        var mutationMilliseconds = stopwatch.Elapsed.TotalMilliseconds;
        _host.UpdateLayout();
        var mutationAndLayoutMilliseconds = stopwatch.Elapsed.TotalMilliseconds;
        await WaitForRenderedFrameAsync(_host);
        stopwatch.Stop();
        var allocated = measure ? GC.GetTotalAllocatedBytes(precise: false) - allocatedBefore : 0;
        var validation = NativeGridAdapter.Validate(handle, expectedCount: 32);
        await DetachAsync();

        return new NativeMeasurement(
            stopwatch.Elapsed.TotalMilliseconds,
            allocated,
            validation,
            mutationMilliseconds,
            mutationAndLayoutMilliseconds - mutationMilliseconds,
            stopwatch.Elapsed.TotalMilliseconds - mutationAndLayoutMilliseconds);
    }

    private async Task<NativeWorkloadResult> MeasureScrollAndRenderAsync()
    {
        using var handle = NativeGridAdapter.Create(expanded: true);
        _host.Content = handle.Grid;
        _host.UpdateLayout();
        await WaitForRenderedFrameAsync(_host);
        NativeGridAdapter.Validate(handle);
        var viewer = NativeGridAdapter.GetScrollViewer(handle);

        for (var batch = 0; batch < NativeBenchmarkOptions.WarmupIterations; ++batch)
            await RunScrollBatchAsync(handle, viewer, batch, measure: false);

        var times = new List<double>();
        var allocationPerJump = new List<double>();

        for (var batch = 0; batch < NativeBenchmarkOptions.Iterations; ++batch)
        {
            CollectForMeasurement();
            var allocatedBefore = GC.GetTotalAllocatedBytes(precise: false);
            var batchTimes = await RunScrollBatchAsync(handle, viewer, batch, measure: true);
            var allocated = GC.GetTotalAllocatedBytes(precise: false) - allocatedBefore;
            times.AddRange(batchTimes);
            allocationPerJump.Add(allocated / (double)NativeBenchmarkOptions.ScrollJumps);
        }

        var validation = NativeGridAdapter.Validate(handle);
        await DetachAsync();
        return NativeWorkloadResult.Create("ScrollAndRender", times, allocationPerJump, validation);
    }

    private async Task<IReadOnlyList<double>> RunScrollBatchAsync(
        NativeGridHandle handle,
        ScrollViewer viewer,
        int batch,
        bool measure)
    {
        var times = measure ? new List<double>(NativeBenchmarkOptions.ScrollJumps) : null;
        for (var i = 0; i < NativeBenchmarkOptions.ScrollJumps; ++i)
        {
            var operation = (batch * NativeBenchmarkOptions.ScrollJumps) + i;
            var row = ((operation * 509) % 2_000) + 1;
            var stopwatch = Stopwatch.StartNew();
            NativeGridAdapter.SetScrollRow(viewer, row);
            handle.Grid.UpdateLayout();
            await WaitForRenderedFrameAsync(_host);
            stopwatch.Stop();
            times?.Add(stopwatch.Elapsed.TotalMilliseconds);
        }

        return times is null ? Array.Empty<double>() : times;
    }

    private async Task DetachAsync()
    {
        _host.Content = null;
        _host.UpdateLayout();
        await WaitForRenderedFrameAsync(_host);
    }

    private static void CollectForMeasurement()
    {
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
    }

    public static async Task WaitForRenderedFrameAsync(TopLevel topLevel)
    {
        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        topLevel.RequestAnimationFrame(_ =>
            topLevel.RequestAnimationFrame(__ => completion.TrySetResult()));
        await completion.Task.WaitAsync(TimeSpan.FromSeconds(5));
    }
}

internal static class NativeGridAdapter
{
    public static Window CreateHostWindow()
    {
        var window = new Window
        {
            Name = "BenchmarkWindow",
            Title = $"Native hierarchy benchmark - {NativeBenchmarkOptions.ImplementationName}",
            Width = 800,
            Height = 500,
            CanResize = false,
            Background = Brushes.White,
            WindowStartupLocation = WindowStartupLocation.CenterScreen,
        };
#if !PRO
        window.Styles.Add(new Style(x => x.OfType<TreeDataGridRow>())
        {
            Setters =
            {
                new Setter(TreeDataGridRow.HeightProperty, 24.0),
            },
        });
#endif
        return window;
    }

    public static NativeGridHandle Create(
        bool expanded,
        TreeShape shape = TreeShape.Deep4094Depth11,
        bool retainCollapsedChildren = false)
    {
        var roots = TreeDataFactory.Create(shape);
#if PRO
#if PRODATAGRID_PR335
        var optimized = NativeBenchmarkOptions.ProMode != "standard";
        var directHierarchy = NativeBenchmarkOptions.ProMode is "direct" or "direct-cell" or "drawn";
        var directContent = NativeBenchmarkOptions.ProMode == "direct";
        var directCell = NativeBenchmarkOptions.ProMode == "direct-cell";
        var drawn = NativeBenchmarkOptions.ProMode == "drawn";
        var options = new HierarchicalOptions<Node>
        {
            ChildrenSelector = node => node.Children,
            IsLeafSelector = node => node.Children.Count == 0,
            VirtualizeChildren = !retainCollapsedChildren,
        };
        var model = new HierarchicalModel<Node>(options);
        model.SetRoots(roots);
        if (expanded)
            model.ExpandAll();

        var grid = new DataGrid
        {
            Name = "BenchmarkGrid",
            AutoGenerateColumns = false,
            HierarchicalModel = model,
            HierarchicalRowsEnabled = true,
            HeadersVisibility = DataGridHeadersVisibility.Column,
            GridLinesVisibility = DataGridGridLinesVisibility.None,
            IsReadOnly = true,
            RowHeight = 24,
            UseLogicalScrollable = true,
            CanUserSortColumns = false,
            CanUserResizeColumns = false,
            CanUserReorderColumns = false,
            UseLightweightFiller = optimized,
        };
        var hierarchyColumn = new DataGridHierarchicalColumn
        {
            Header = "Name",
            Width = new DataGridLength(300),
            Binding = new Binding("Item.Name"),
            UseDirectCell = directHierarchy,
            UseDirectTextContent = directHierarchy,
            TrackDirectTextValueChanges = false,
        };
        DataGridColumnMetadata.SetValueAccessor(
            hierarchyColumn,
            new DataGridColumnValueAccessor<HierarchicalNode, string>(node => ((Node)node.Item).Name));
        grid.Columns.Add(hierarchyColumn);
        AddProTextColumn(grid, "Id", 90, "Item.Id", node => ((Node)node.Item).Id, directContent, directCell, drawn);
        AddProTextColumn(grid, "Depth", 90, "Item.Depth", node => ((Node)node.Item).Depth, directContent, directCell, drawn);
        AddProTextColumn(grid, "Children", 100, "Item.ChildCount", node => ((Node)node.Item).ChildCount, directContent, directCell, drawn);
        AddProTextColumn(grid, "Payload", 180, "Item.Payload", node => ((Node)node.Item).Payload, directContent, directCell, drawn);
        if (optimized)
            ApplyProOptimizedThemes(grid, featurePreserving: NativeBenchmarkOptions.ProMode == "optimized");
        return new NativeGridHandle(model, grid);
#else
        var options = new HierarchicalOptions<Node>
        {
            ChildrenSelector = node => node.Children,
            IsLeafSelector = node => node.Children.Count == 0,
            VirtualizeChildren = !retainCollapsedChildren,
        };
        var model = new HierarchicalModel<Node>(options);
        model.SetRoots(roots);
        if (expanded)
            model.ExpandAll();

        var grid = new DataGrid
        {
            Name = "BenchmarkGrid",
            AutoGenerateColumns = false,
            HierarchicalModel = model,
            HierarchicalRowsEnabled = true,
            HeadersVisibility = DataGridHeadersVisibility.Column,
            GridLinesVisibility = DataGridGridLinesVisibility.None,
            IsReadOnly = true,
            RowHeight = 24,
            UseLogicalScrollable = true,
            CanUserSortColumns = false,
            CanUserResizeColumns = false,
            CanUserReorderColumns = false,
        };
        grid.Columns.Add(new DataGridHierarchicalColumn
        {
            Header = "Name",
            Width = new DataGridLength(300),
            Binding = new Binding("Item.Name"),
        });
        grid.Columns.Add(new DataGridTextColumn
        {
            Header = "Id",
            Width = new DataGridLength(90),
            Binding = new Binding("Item.Id"),
        });
        grid.Columns.Add(new DataGridTextColumn
        {
            Header = "Depth",
            Width = new DataGridLength(90),
            Binding = new Binding("Item.Depth"),
        });
        grid.Columns.Add(new DataGridTextColumn
        {
            Header = "Children",
            Width = new DataGridLength(100),
            Binding = new Binding("Item.ChildCount"),
        });
        grid.Columns.Add(new DataGridTextColumn
        {
            Header = "Payload",
            Width = new DataGridLength(180),
            Binding = new Binding("Item.Payload"),
        });
        return new NativeGridHandle(model, grid);
#endif
#else
        var source = new HierarchicalTreeDataGridSource<Node>(roots);
#if ACCELERATE
        source.WithHierarchicalExpanderTextColumn(
            "Name",
            x => x.Name,
            x => x.Children,
            hasChildren: x => x.HasChildren,
            options: options =>
            {
                options.Width = new GridLength(300);
                options.UseDrawnCells = NativeBenchmarkOptions.DrawnCells;
            });
#else
        source.WithHierarchicalExpanderTextColumn(
            "Name",
            x => x.Name,
            x => x.Children,
            options => options.Width = new GridLength(300));
#endif
        source.WithTextColumn("Id", x => x.Id, options =>
        {
            options.Width = new GridLength(90);
#if ACCELERATE
            options.UseDrawnCells = NativeBenchmarkOptions.DrawnCells;
#endif
        });
        source.WithTextColumn("Depth", x => x.Depth, options =>
        {
            options.Width = new GridLength(90);
#if ACCELERATE
            options.UseDrawnCells = NativeBenchmarkOptions.DrawnCells;
#endif
        });
        source.WithTextColumn("Children", x => x.ChildCount, options =>
        {
            options.Width = new GridLength(100);
#if ACCELERATE
            options.UseDrawnCells = NativeBenchmarkOptions.DrawnCells;
#endif
        });
        source.WithTextColumn("Payload", x => x.Payload, options =>
        {
            options.Width = new GridLength(180);
#if ACCELERATE
            options.UseDrawnCells = NativeBenchmarkOptions.DrawnCells;
#endif
        });
        if (expanded)
            source.ExpandAll();

        var grid = new TreeDataGridControl
        {
            Name = "BenchmarkGrid",
            Source = source,
        };
        return new NativeGridHandle(source, grid);
#endif
    }

#if PRO && PRODATAGRID_PR335
    private static void AddProTextColumn<TValue>(
        DataGrid grid,
        string header,
        double width,
        string bindingPath,
        Func<HierarchicalNode, TValue> getter,
        bool directContent,
        bool directCell,
        bool drawn)
    {
        var column = new DataGridTextColumn
        {
            Header = header,
            Width = new DataGridLength(width),
            Binding = new Binding(bindingPath),
            UseDirectTextContent = directContent,
            UseDirectTextCell = directCell,
            TrackDirectTextValueChanges = false,
            DisplayMode = drawn ? DataGridColumnDisplayMode.Drawn : DataGridColumnDisplayMode.Retained,
        };
        DataGridColumnMetadata.SetValueAccessor(
            column,
            new DataGridColumnValueAccessor<HierarchicalNode, TValue>(getter));
        grid.Columns.Add(column);
    }

    private static void ApplyProOptimizedThemes(DataGrid grid, bool featurePreserving)
    {
        grid.RowTheme = FindProTheme(
            grid,
            featurePreserving ? "DataGridOptimizedFeatureUnfrozenRowTheme" : "DataGridOptimizedUnfrozenRowTheme");
        grid.CellTheme = FindProTheme(grid, "DataGridOptimizedCellTheme");
        grid.ColumnHeaderTheme = FindProTheme(
            grid,
            featurePreserving ? "DataGridOptimizedFeatureColumnHeaderTheme" : "DataGridOptimizedColumnHeaderTheme");
    }

    private static ControlTheme FindProTheme(DataGrid grid, string key)
    {
        if ((grid.TryFindResource(key, out object? value) ||
             Application.Current?.TryFindResource(key, out value) == true) &&
            value is ControlTheme theme)
        {
            return theme;
        }

        throw new InvalidOperationException($"Unable to resolve optimized theme resource '{key}'.");
    }
#endif

    public static NativeValidation Validate(NativeGridHandle handle, int expectedCount = 4_094)
    {
        if (handle.Count != expectedCount)
            throw new InvalidOperationException($"Expected {expectedCount:N0} rows, got {handle.Count:N0}.");

#if PRO
        var realizedRows = handle.Grid.GetVisualDescendants().OfType<DataGridRow>().Count();
        var realizedCells = handle.Grid.GetVisualDescendants().OfType<DataGridCell>().Count();
#else
        var realizedRows = handle.Grid.GetVisualDescendants().OfType<TreeDataGridRow>().Count();
        var realizedCells = handle.Grid.GetVisualDescendants().OfType<TreeDataGridCell>().Count();
#endif
        if (realizedRows <= 0 || realizedRows >= 200)
            throw new InvalidOperationException($"Virtualization validation failed: {realizedRows} realized rows.");

        var viewer = GetScrollViewer(handle);
        return new NativeValidation(
            handle.Count,
            realizedRows,
            realizedCells,
            viewer.Extent.Width,
            viewer.Extent.Height,
            viewer.Viewport.Width,
            viewer.Viewport.Height);
    }

    public static ScrollViewer GetScrollViewer(NativeGridHandle handle) =>
        handle.Grid.GetVisualDescendants()
            .OfType<ScrollViewer>()
            .First(x => x.Name == "PART_ScrollViewer");

    public static void SetScrollRow(ScrollViewer viewer, int row)
    {
#if PRO
        viewer.Offset = new Vector(0, row);
#else
        viewer.Offset = new Vector(0, row * 24.0);
#endif
    }
}

#if PRO
internal sealed class NativeGridHandle : IDisposable
{
    public NativeGridHandle(HierarchicalModel<Node> model, DataGrid grid)
    {
        Model = model;
        Grid = grid;
    }

    public HierarchicalModel<Node> Model { get; }

    public DataGrid Grid { get; }

    public int Count => Model.Count;

    public void ExpandAll() => Model.ExpandAll();

    public void CollapseAll() => Model.CollapseAll();

    public void Dispose()
    {
    }
}
#else
internal sealed class NativeGridHandle : IDisposable
{
    public NativeGridHandle(HierarchicalTreeDataGridSource<Node> source, TreeDataGridControl grid)
    {
        Source = source;
        Grid = grid;
    }

    public HierarchicalTreeDataGridSource<Node> Source { get; }

    public TreeDataGridControl Grid { get; }

    public int Count => Source.Rows.Count;

    public void ExpandAll() => Source.ExpandAll();

    public void CollapseAll() => Source.CollapseAll();

    public void Dispose() => Source.Dispose();
}
#endif

internal sealed record NativeMeasurement(
    double ElapsedMilliseconds,
    long AllocatedBytes,
    NativeValidation Validation,
    double MutationMilliseconds = 0,
    double LayoutMilliseconds = 0,
    double FrameMilliseconds = 0);

internal sealed record NativeValidation(
    int RowCount,
    int RealizedRows,
    int RealizedCells,
    double ExtentWidth,
    double ExtentHeight,
    double ViewportWidth,
    double ViewportHeight);

internal sealed class NativeWorkloadResult
{
    public required string Name { get; init; }

    public int Samples { get; init; }

    public double MeanMilliseconds { get; init; }

    public double MedianMilliseconds { get; init; }

    public double P95Milliseconds { get; init; }

    public double StandardDeviationMilliseconds { get; init; }

    public double MeanAllocatedBytes { get; init; }

    public double MeanMutationMilliseconds { get; init; }

    public double MeanLayoutMilliseconds { get; init; }

    public double MeanFrameMilliseconds { get; init; }

    public required NativeValidation Validation { get; init; }

    public static NativeWorkloadResult Create(
        string name,
        IReadOnlyList<double> times,
        IReadOnlyList<double> allocations,
        NativeValidation validation,
        IReadOnlyList<double>? mutationTimes = null,
        IReadOnlyList<double>? layoutTimes = null,
        IReadOnlyList<double>? frameTimes = null)
    {
        var ordered = times.OrderBy(x => x).ToArray();
        var mean = times.Average();
        var variance = times.Sum(x => Math.Pow(x - mean, 2)) / times.Count;
        return new NativeWorkloadResult
        {
            Name = name,
            Samples = times.Count,
            MeanMilliseconds = mean,
            MedianMilliseconds = Percentile(ordered, 0.50),
            P95Milliseconds = Percentile(ordered, 0.95),
            StandardDeviationMilliseconds = Math.Sqrt(variance),
            MeanAllocatedBytes = allocations.Average(),
            MeanMutationMilliseconds = mutationTimes?.Average() ?? 0,
            MeanLayoutMilliseconds = layoutTimes?.Average() ?? 0,
            MeanFrameMilliseconds = frameTimes?.Average() ?? 0,
            Validation = validation,
        };
    }

    private static double Percentile(IReadOnlyList<double> ordered, double percentile)
    {
        var index = Math.Clamp((int)Math.Ceiling(percentile * ordered.Count) - 1, 0, ordered.Count - 1);
        return ordered[index];
    }
}

internal sealed class NativeBenchmarkResult
{
    public DateTimeOffset TimestampUtc { get; init; }

    public required string Implementation { get; init; }

    public required string Mode { get; init; }

    public required string AvaloniaVersion { get; init; }

    public required string Runtime { get; init; }

    public required string OS { get; init; }

    public required string Architecture { get; init; }

    public required string Processor { get; init; }

    public int LogicalProcessorCount { get; init; }

    public double ClientWidth { get; init; }

    public double ClientHeight { get; init; }

    public double RenderScaling { get; init; }

    public int WarmupIterations { get; init; }

    public int MeasuredIterations { get; init; }

    public int ScrollJumpsPerIteration { get; init; }

    public required NativeWorkloadResult FirstExpandedRender { get; init; }

    public NativeWorkloadResult? ExpandAllAndRender { get; init; }

    public NativeWorkloadResult? CollapseAllAndRender { get; init; }

    public NativeWorkloadResult? ScrollAndRender { get; init; }
}

internal static class NativeBenchmarkResultWriter
{
    public static JsonSerializerOptions JsonOptions { get; } = new()
    {
        WriteIndented = true,
    };

    public static async Task WriteAsync(NativeBenchmarkResult result, string path)
    {
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);
        await File.WriteAllTextAsync(path, JsonSerializer.Serialize(result, JsonOptions));
    }
}
