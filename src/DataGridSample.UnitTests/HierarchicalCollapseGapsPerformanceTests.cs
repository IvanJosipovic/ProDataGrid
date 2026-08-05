using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.IO;
using System.Linq;
using System.Text.Json;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Xunit;

namespace DataGridSample.Tests;

public sealed class HierarchicalCollapseGapsPerformanceTests
{
    private const int ScrollSteps = 64;
    private const string DiagnosticsMeterName = "ProDataGrid.Diagnostic.Meter";
    private const string RowsDisplayUpdateTimeName = "prodatagrid.rows.display.update.time";
    private const string RowsPresenterViewportChangedTimeName = "prodatagrid.rows.presenter.viewport.changed.time";
    private const string RowsScrollSlotsByHeightTimeName = "prodatagrid.rows.scroll.slots.by.height.time";
    private const string RowsScrollEstimateOffsetTimeName = "prodatagrid.rows.scroll.estimate.offset.time";
    private const string RowsMeasureTimeName = "prodatagrid.rows.measure.time";
    private const string RowsArrangeTimeName = "prodatagrid.rows.arrange.time";
    private const string DataGridMeasureTimeName = "prodatagrid.datagrid.measure.time";
    private const string DataGridArrangeTimeName = "prodatagrid.datagrid.arrange.time";
    private const string CellsMeasureTimeName = "prodatagrid.cells.measure.time";
    private const string CellsArrangeTimeName = "prodatagrid.cells.arrange.time";
    private const string RowMeasureTimeName = "prodatagrid.row.measure.time";
    private const string RowArrangeTimeName = "prodatagrid.row.arrange.time";
    private const string RowsDisplayElementInsertTimeName = "prodatagrid.rows.display.element.insert.time";
    private const string RowsDisplayElementAttachTimeName = "prodatagrid.rows.display.element.attach.time";
    private const string RowsDisplayElementMeasureTimeName = "prodatagrid.rows.display.element.measure.time";
    private const string RowsDisplayElementHeightRecordTimeName = "prodatagrid.rows.display.element.height.record.time";
    private const string RowsDisplayElementLoadTimeName = "prodatagrid.rows.display.element.load.time";
    private const string RowsScrollViewportDeltaName = "prodatagrid.rows.scroll.viewport.delta";
    private const string RowsScrollExtentDeltaName = "prodatagrid.rows.scroll.extent.delta";
    private const string RowsLogicalOffsetSynchronizedDeltaName = "prodatagrid.rows.logical.offset.synchronized.delta";
    private const string RowGenerateTimeName = "prodatagrid.rows.generate.time";
    private const string RowsRealizedCountName = "prodatagrid.rows.realized.count";
    private const string RowsRecycledCountName = "prodatagrid.rows.recycled.count";
    private const string RowsPreparedCountName = "prodatagrid.rows.prepared.count";
    private const string RowsMeasuredCountName = "prodatagrid.rows.measured.count";
    private const string RowsMeasureSkippedCountName = "prodatagrid.rows.measure.skipped.count";
    private const string RowsArrangedCountName = "prodatagrid.rows.arranged.count";
    private const string RowsArrangeSkippedCountName = "prodatagrid.rows.arrange.skipped.count";
    private const string RowsArrangeMeasureInvalidatedCountName = "prodatagrid.rows.arrange.measure.invalidated.count";
    private const string RowsScrollInfoChangedCountName = "prodatagrid.rows.scroll.info.changed.count";
    private const string RowsScrollExtentChangedCountName = "prodatagrid.rows.scroll.extent.changed.count";
    private const string RowsScrollViewportChangedCountName = "prodatagrid.rows.scroll.viewport.changed.count";
    private const string RowsScrollOffsetCoercedCountName = "prodatagrid.rows.scroll.offset.coerced.count";
    private const string RowsScrollInvalidatedCountName = "prodatagrid.rows.scroll.invalidated.count";
    private const string RowsLogicalOffsetSynchronizedCountName = "prodatagrid.rows.logical.offset.synchronized.count";
    private const string RowsScrollExactSlotHeightLookupCountName = "prodatagrid.rows.scroll.exact.slot.height.lookup.count";
    private const string RowsScrollExactSlotHeightInsertionCountName = "prodatagrid.rows.scroll.exact.slot.height.insertion.count";

    private readonly ITestOutputHelper _output;

    public HierarchicalCollapseGapsPerformanceTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [AvaloniaFact]
    public void RapidScrollbarScroll_ReportsLayoutRenderAndMemoryTiming()
    {
        var artifactDirectory = GetArtifactDirectory();
        Directory.CreateDirectory(artifactDirectory);
        using var diagnostics = new DataGridDiagnosticsListener();

        var page = new global::DataGridSample.Pages.HierarchicalCollapseGapsPage();
        var window = new Window
        {
            Width = 1280,
            Height = 900,
            Content = page
        };
        window.ApplySampleTheme();

        try
        {
            window.Show();
            PumpLayout(window);

            var grid = page.GetVisualDescendants().OfType<DataGrid>().Single();
            var scrollViewer = grid.GetVisualDescendants().OfType<ScrollViewer>().Single();
            var maximumOffset = Math.Max(0, scrollViewer.Extent.Height - scrollViewer.Viewport.Height);

            var warmupOffsets = new[]
            {
                maximumOffset * 0.25,
                maximumOffset * 0.75,
                maximumOffset * 0.50,
                0
            };
            foreach (var offset in warmupOffsets)
            {
                MeasureScroll(window, grid, scrollViewer, offset, captureFrame: false);
            }

            diagnostics.Reset();
            var initialManagedBytes = GC.GetTotalMemory(false);
            var initialResidentBytes = Process.GetCurrentProcess().WorkingSet64;
            var samples = new List<ScrollSample>(ScrollSteps * 2);

            for (var direction = 0; direction < 2; direction++)
            {
                for (var step = 0; step < ScrollSteps; step++)
                {
                    var progress = (double)step / (ScrollSteps - 1);
                    if (direction == 1)
                    {
                        progress = 1 - progress;
                    }

                    var offset = maximumOffset * progress;
                    samples.Add(MeasureScroll(window, grid, scrollViewer, offset, captureFrame: true, diagnostics));
                }
            }

            var diagnosticReport = diagnostics.CreateReport();
            var screenshotPath = SaveFinalFrame(window);
            var report = new PerformanceReport(
                DateTimeOffset.UtcNow,
                maximumOffset,
                initialManagedBytes,
                initialResidentBytes,
                GC.GetTotalMemory(false),
                Process.GetCurrentProcess().WorkingSet64,
                CalculateStats(samples.Select(sample => sample.LayoutMilliseconds)),
                CalculateStats(samples.Select(sample => sample.RenderMilliseconds)),
                CalculateStats(samples.Select(sample => sample.ManagedAllocatedBytes)),
                CalculateStats(samples.Select(sample => sample.ResidentBytes)),
                diagnosticReport,
                samples,
                screenshotPath);

            var reportPath = Path.Combine(artifactDirectory, "hierarchical-collapse-gaps-scroll.json");
            File.WriteAllText(reportPath, JsonSerializer.Serialize(report, new JsonSerializerOptions
            {
                WriteIndented = true
            }));

            _output.WriteLine(FormattableString.Invariant(
                $"Hierarchical Collapse Gaps scroll benchmark: steps={samples.Count}, extent={maximumOffset:F1}; layout median={report.Layout.Median:F3} ms p95={report.Layout.P95:F3} ms; render median={report.Render.Median:F3} ms p95={report.Render.P95:F3} ms; row display median={report.Diagnostics.RowsDisplayUpdate.Median:F3} ms p95={report.Diagnostics.RowsDisplayUpdate.P95:F3} ms; row measure median={report.Diagnostics.RowsMeasure.Median:F3} ms p95={report.Diagnostics.RowsMeasure.P95:F3} ms; row arrange median={report.Diagnostics.RowsArrange.Median:F3} ms p95={report.Diagnostics.RowsArrange.P95:F3} ms; row generation median={report.Diagnostics.RowGeneration.Median:F3} ms p95={report.Diagnostics.RowGeneration.P95:F3} ms; measured={report.Diagnostics.RowsMeasured} measure skipped={report.Diagnostics.RowsMeasureSkipped} arranged={report.Diagnostics.RowsArranged} arrange skipped={report.Diagnostics.RowsArrangeSkipped} arrange-triggered measure invalidations={report.Diagnostics.RowsArrangeMeasureInvalidated}; rows realized={report.Diagnostics.RowsRealized} recycled={report.Diagnostics.RowsRecycled} prepared={report.Diagnostics.RowsPrepared}; allocated median={report.Allocated.Median:F0} bytes; resident memory peak={report.ResidentMemory.Maximum:F0} bytes; report={reportPath}; screenshot={screenshotPath ?? "none"}"));

            Assert.True(samples.Count == ScrollSteps * 2);
            Assert.True(maximumOffset > 0);
            Assert.All(samples, sample =>
            {
                Assert.True(sample.LayoutMilliseconds >= 0);
                Assert.True(sample.RenderMilliseconds >= 0);
                Assert.True(sample.RealizedRows > 0);
            });
        }
        finally
        {
            window.Close();
        }
    }

    private static ScrollSample MeasureScroll(
        Window window,
        DataGrid grid,
        ScrollViewer scrollViewer,
        double offset,
        bool captureFrame,
        DataGridDiagnosticsListener? diagnostics = null)
    {
        var diagnosticsBefore = diagnostics?.CreateSnapshot();
        var allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
        var residentBefore = Process.GetCurrentProcess().WorkingSet64;
        var layoutTimer = Stopwatch.StartNew();

        var offsetTimer = Stopwatch.StartNew();
        scrollViewer.Offset = new Vector(0, offset);
        offsetTimer.Stop();

        var preLayoutJobsTimer = Stopwatch.StartNew();
        Dispatcher.UIThread.RunJobs();
        preLayoutJobsTimer.Stop();

        var updateLayoutTimer = Stopwatch.StartNew();
        window.UpdateLayout();
        updateLayoutTimer.Stop();

        var postLayoutJobsTimer = Stopwatch.StartNew();
        Dispatcher.UIThread.RunJobs();
        postLayoutJobsTimer.Stop();

        layoutTimer.Stop();

        DiagnosticDelta? diagnosticDelta = null;
        if (diagnosticsBefore is not null)
        {
            diagnosticDelta = diagnostics!.CreateSnapshot().Subtract(diagnosticsBefore);
        }

        var splitPath = Environment.GetEnvironmentVariable("DATAGRID_LAYOUT_SPLIT_PATH");
        if (!string.IsNullOrWhiteSpace(splitPath))
        {
            File.AppendAllText(
                splitPath,
                FormattableString.Invariant(
                    $"{offset:F3}\toffset={offsetTimer.Elapsed.TotalMilliseconds:F6}\tpreJobs={preLayoutJobsTimer.Elapsed.TotalMilliseconds:F6}\tupdateLayout={updateLayoutTimer.Elapsed.TotalMilliseconds:F6}\tpostJobs={postLayoutJobsTimer.Elapsed.TotalMilliseconds:F6}{Environment.NewLine}"));
        }

        var renderTimer = Stopwatch.StartNew();
        if (captureFrame)
        {
            using var frame = window.CaptureRenderedFrame();
        }
        renderTimer.Stop();

        var rows = grid.GetVisualDescendants()
            .OfType<DataGridRow>()
            .Where(row => row.IsVisible)
            .ToArray();
        var firstRow = rows.Length == 0 ? -1 : rows.Min(row => row.Index);
        var lastRow = rows.Length == 0 ? -1 : rows.Max(row => row.Index);

        return new ScrollSample(
            offset,
            layoutTimer.Elapsed.TotalMilliseconds,
            renderTimer.Elapsed.TotalMilliseconds,
            GC.GetAllocatedBytesForCurrentThread() - allocatedBefore,
            Math.Max(residentBefore, Process.GetCurrentProcess().WorkingSet64),
            rows.Length,
            firstRow,
            lastRow,
            diagnosticDelta);
    }

    private static string? SaveFinalFrame(Window window)
    {
        var screenshotDirectory = Environment.GetEnvironmentVariable("AVALONIA_SCREENSHOT_DIR");
        if (string.IsNullOrWhiteSpace(screenshotDirectory))
        {
            return null;
        }

        Directory.CreateDirectory(screenshotDirectory);
        var path = Path.Combine(screenshotDirectory, "hierarchical-collapse-gaps-post-scroll.png");
        using var frame = window.CaptureRenderedFrame();
        if (frame != null)
        {
            using var stream = File.Create(path);
            frame.Save(stream, new Avalonia.Media.Imaging.PngBitmapEncoderOptions());
        }
        return File.Exists(path) ? Path.GetFullPath(path) : null;
    }

    private static string GetArtifactDirectory()
    {
        var directory = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "Avalonia.Controls.DataGrid.slnx")))
        {
            directory = directory.Parent;
        }

        var rootDirectory = directory?.FullName ?? Directory.GetCurrentDirectory();
        var configuredDirectory = Environment.GetEnvironmentVariable("DATAGRID_PERF_ARTIFACT_DIR");
        if (!string.IsNullOrWhiteSpace(configuredDirectory))
        {
            return Path.GetFullPath(configuredDirectory, rootDirectory);
        }

        return Path.Combine(
            rootDirectory,
            "artifacts",
            "performance",
            "hierarchical-collapse-gaps");
    }

    private static void PumpLayout(Control control)
    {
        Dispatcher.UIThread.RunJobs();
        control.UpdateLayout();
        Dispatcher.UIThread.RunJobs();
    }

    private static Stats CalculateStats(IEnumerable<double> values)
    {
        var sorted = values.OrderBy(value => value).ToArray();
        var p95Index = Math.Max(0, (int)Math.Ceiling(sorted.Length * 0.95) - 1);
        return new Stats(
            sorted.Length == 0 ? 0 : sorted[sorted.Length / 2],
            sorted.Length == 0 ? 0 : sorted[p95Index],
            sorted.Length == 0 ? 0 : sorted[0],
            sorted.Length == 0 ? 0 : sorted[^1]);
    }

    private static Stats CalculateStats(IEnumerable<long> values)
    {
        return CalculateStats(values.Select(value => (double)value));
    }

    private sealed record PerformanceReport(
        DateTimeOffset TimestampUtc,
        double MaximumOffset,
        long InitialManagedBytes,
        long InitialResidentBytes,
        long FinalManagedBytes,
        long FinalResidentBytes,
        Stats Layout,
        Stats Render,
        Stats Allocated,
        Stats ResidentMemory,
        DiagnosticReport Diagnostics,
        IReadOnlyList<ScrollSample> Samples,
        string? ScreenshotPath);

    private sealed record DiagnosticReport(
        Stats RowsDisplayUpdate,
        Stats RowsPresenterViewportChanged,
        Stats RowsScrollSlotsByHeight,
        Stats RowsScrollEstimateOffset,
        Stats RowsMeasure,
        Stats RowsArrange,
        Stats DataGridMeasure,
        Stats DataGridArrange,
        Stats CellsMeasure,
        Stats CellsArrange,
        Stats RowMeasure,
        Stats RowArrange,
        Stats RowsDisplayElementInsert,
        Stats RowsDisplayElementAttach,
        Stats RowsDisplayElementMeasure,
        Stats RowsDisplayElementHeightRecord,
        Stats RowsDisplayElementLoad,
        Stats RowsScrollViewportDelta,
        Stats RowsScrollExtentDelta,
        Stats RowsLogicalOffsetSynchronizedDelta,
        Stats RowGeneration,
        long RowsRealized,
        long RowsRecycled,
        long RowsPrepared,
        long RowsMeasured,
        long RowsMeasureSkipped,
        long RowsArranged,
        long RowsArrangeSkipped,
        long RowsArrangeMeasureInvalidated,
        long RowsScrollInfoChanged,
        long RowsScrollExtentChanged,
        long RowsScrollViewportChanged,
        long RowsScrollOffsetCoerced,
        long RowsScrollInvalidated,
        long RowsLogicalOffsetSynchronized,
        long RowsScrollExactSlotHeightLookups,
        long RowsScrollExactSlotHeightInsertions,
        int RowsDisplayUpdatePasses,
        int RowsMeasurePasses,
        int RowsArrangePasses);

    private sealed record ScrollSample(
        double Offset,
        double LayoutMilliseconds,
        double RenderMilliseconds,
        long ManagedAllocatedBytes,
        long ResidentBytes,
        int RealizedRows,
        int FirstRow,
        int LastRow,
        DiagnosticDelta? Diagnostics);

    private sealed record DiagnosticDelta(
        int RowsDisplayUpdatePasses,
        int RowsPresenterViewportChangedPasses,
        int RowsScrollSlotsByHeightPasses,
        int RowsMeasurePasses,
        int RowsArrangePasses,
        int RowGenerationPasses,
        long RowsRealized,
        long RowsRecycled,
        long RowsPrepared,
        long RowsMeasured,
        long RowsMeasureSkipped,
        long RowsArranged,
        long RowsArrangeSkipped,
        long RowsScrollInfoChanged,
        long RowsScrollExtentChanged,
        long RowsScrollInvalidated,
        long RowsScrollExactSlotHeightLookups,
        long RowsScrollExactSlotHeightInsertions);

    private sealed record DiagnosticSnapshot(
        int RowsDisplayUpdatePasses,
        int RowsPresenterViewportChangedPasses,
        int RowsScrollSlotsByHeightPasses,
        int RowsMeasurePasses,
        int RowsArrangePasses,
        int RowGenerationPasses,
        long RowsRealized,
        long RowsRecycled,
        long RowsPrepared,
        long RowsMeasured,
        long RowsMeasureSkipped,
        long RowsArranged,
        long RowsArrangeSkipped,
        long RowsScrollInfoChanged,
        long RowsScrollExtentChanged,
        long RowsScrollInvalidated,
        long RowsScrollExactSlotHeightLookups,
        long RowsScrollExactSlotHeightInsertions)
    {
        public DiagnosticDelta Subtract(DiagnosticSnapshot previous)
        {
            return new DiagnosticDelta(
                RowsDisplayUpdatePasses - previous.RowsDisplayUpdatePasses,
                RowsPresenterViewportChangedPasses - previous.RowsPresenterViewportChangedPasses,
                RowsScrollSlotsByHeightPasses - previous.RowsScrollSlotsByHeightPasses,
                RowsMeasurePasses - previous.RowsMeasurePasses,
                RowsArrangePasses - previous.RowsArrangePasses,
                RowGenerationPasses - previous.RowGenerationPasses,
                RowsRealized - previous.RowsRealized,
                RowsRecycled - previous.RowsRecycled,
                RowsPrepared - previous.RowsPrepared,
                RowsMeasured - previous.RowsMeasured,
                RowsMeasureSkipped - previous.RowsMeasureSkipped,
                RowsArranged - previous.RowsArranged,
                RowsArrangeSkipped - previous.RowsArrangeSkipped,
                RowsScrollInfoChanged - previous.RowsScrollInfoChanged,
                RowsScrollExtentChanged - previous.RowsScrollExtentChanged,
                RowsScrollInvalidated - previous.RowsScrollInvalidated,
                RowsScrollExactSlotHeightLookups - previous.RowsScrollExactSlotHeightLookups,
                RowsScrollExactSlotHeightInsertions - previous.RowsScrollExactSlotHeightInsertions);
        }
    }

    private sealed record Stats(double Median, double P95, double Minimum, double Maximum);

    private sealed class DataGridDiagnosticsListener : IDisposable
    {
        private readonly MeterListener _listener;
        private readonly Dictionary<string, List<double>> _doubleMeasurements = new(StringComparer.Ordinal);
        private readonly Dictionary<string, long> _longMeasurements = new(StringComparer.Ordinal);

        public DataGridDiagnosticsListener()
        {
            _listener = new MeterListener
            {
                InstrumentPublished = (instrument, listener) =>
                {
                    if (instrument.Meter.Name == DiagnosticsMeterName)
                    {
                        listener.EnableMeasurementEvents(instrument);
                    }
                }
            };

            _listener.SetMeasurementEventCallback<double>((instrument, measurement, _, _) =>
            {
                if (!_doubleMeasurements.TryGetValue(instrument.Name, out var values))
                {
                    values = new List<double>();
                    _doubleMeasurements[instrument.Name] = values;
                }

                values.Add(measurement);
            });

            _listener.SetMeasurementEventCallback<long>((instrument, measurement, _, _) =>
            {
                _longMeasurements[instrument.Name] =
                    _longMeasurements.GetValueOrDefault(instrument.Name) + measurement;
            });

            _listener.Start();
        }

        public void Reset()
        {
            _doubleMeasurements.Clear();
            _longMeasurements.Clear();
        }

        public DiagnosticSnapshot CreateSnapshot()
        {
            return new DiagnosticSnapshot(
                GetDoubleMeasurementCount(RowsDisplayUpdateTimeName),
                GetDoubleMeasurementCount(RowsPresenterViewportChangedTimeName),
                GetDoubleMeasurementCount(RowsScrollSlotsByHeightTimeName),
                GetDoubleMeasurementCount(RowsMeasureTimeName),
                GetDoubleMeasurementCount(RowsArrangeTimeName),
                GetDoubleMeasurementCount(RowGenerateTimeName),
                GetLongMeasurement(RowsRealizedCountName),
                GetLongMeasurement(RowsRecycledCountName),
                GetLongMeasurement(RowsPreparedCountName),
                GetLongMeasurement(RowsMeasuredCountName),
                GetLongMeasurement(RowsMeasureSkippedCountName),
                GetLongMeasurement(RowsArrangedCountName),
                GetLongMeasurement(RowsArrangeSkippedCountName),
                GetLongMeasurement(RowsScrollInfoChangedCountName),
                GetLongMeasurement(RowsScrollExtentChangedCountName),
                GetLongMeasurement(RowsScrollInvalidatedCountName),
                GetLongMeasurement(RowsScrollExactSlotHeightLookupCountName),
                GetLongMeasurement(RowsScrollExactSlotHeightInsertionCountName));
        }

        public DiagnosticReport CreateReport()
        {
            return new DiagnosticReport(
                CalculateStats(GetDoubleMeasurements(RowsDisplayUpdateTimeName)),
                CalculateStats(GetDoubleMeasurements(RowsPresenterViewportChangedTimeName)),
                CalculateStats(GetDoubleMeasurements(RowsScrollSlotsByHeightTimeName)),
                CalculateStats(GetDoubleMeasurements(RowsScrollEstimateOffsetTimeName)),
                CalculateStats(GetDoubleMeasurements(RowsMeasureTimeName)),
                CalculateStats(GetDoubleMeasurements(RowsArrangeTimeName)),
                CalculateStats(GetDoubleMeasurements(DataGridMeasureTimeName)),
                CalculateStats(GetDoubleMeasurements(DataGridArrangeTimeName)),
                CalculateStats(GetDoubleMeasurements(CellsMeasureTimeName)),
                CalculateStats(GetDoubleMeasurements(CellsArrangeTimeName)),
                CalculateStats(GetDoubleMeasurements(RowMeasureTimeName)),
                CalculateStats(GetDoubleMeasurements(RowArrangeTimeName)),
                CalculateStats(GetDoubleMeasurements(RowsDisplayElementInsertTimeName)),
                CalculateStats(GetDoubleMeasurements(RowsDisplayElementAttachTimeName)),
                CalculateStats(GetDoubleMeasurements(RowsDisplayElementMeasureTimeName)),
                CalculateStats(GetDoubleMeasurements(RowsDisplayElementHeightRecordTimeName)),
                CalculateStats(GetDoubleMeasurements(RowsDisplayElementLoadTimeName)),
                CalculateStats(GetDoubleMeasurements(RowsScrollViewportDeltaName)),
                CalculateStats(GetDoubleMeasurements(RowsScrollExtentDeltaName)),
                CalculateStats(GetDoubleMeasurements(RowsLogicalOffsetSynchronizedDeltaName)),
                CalculateStats(GetDoubleMeasurements(RowGenerateTimeName)),
                GetLongMeasurement(RowsRealizedCountName),
                GetLongMeasurement(RowsRecycledCountName),
                GetLongMeasurement(RowsPreparedCountName),
                GetLongMeasurement(RowsMeasuredCountName),
                GetLongMeasurement(RowsMeasureSkippedCountName),
                GetLongMeasurement(RowsArrangedCountName),
                GetLongMeasurement(RowsArrangeSkippedCountName),
                GetLongMeasurement(RowsArrangeMeasureInvalidatedCountName),
                GetLongMeasurement(RowsScrollInfoChangedCountName),
                GetLongMeasurement(RowsScrollExtentChangedCountName),
                GetLongMeasurement(RowsScrollViewportChangedCountName),
                GetLongMeasurement(RowsScrollOffsetCoercedCountName),
                GetLongMeasurement(RowsScrollInvalidatedCountName),
                GetLongMeasurement(RowsLogicalOffsetSynchronizedCountName),
                GetLongMeasurement(RowsScrollExactSlotHeightLookupCountName),
                GetLongMeasurement(RowsScrollExactSlotHeightInsertionCountName),
                GetDoubleMeasurementCount(RowsDisplayUpdateTimeName),
                GetDoubleMeasurementCount(RowsMeasureTimeName),
                GetDoubleMeasurementCount(RowsArrangeTimeName));
        }

        public void Dispose()
        {
            _listener.Dispose();
        }

        private IEnumerable<double> GetDoubleMeasurements(string name)
        {
            return _doubleMeasurements.TryGetValue(name, out var values)
                ? values
                : Array.Empty<double>();
        }

        private long GetLongMeasurement(string name)
        {
            return _longMeasurements.GetValueOrDefault(name);
        }

        private int GetDoubleMeasurementCount(string name)
        {
            return _doubleMeasurements.TryGetValue(name, out var values)
                ? values.Count
                : 0;
        }
    }
}