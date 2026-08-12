# Native hierarchy source comparison

This harness compares ProDataGrid with Wieslaw's open-source TreeDataGrid using
source project references only. It has no paid-grid assembly or package reference.
The workflow pins the TreeDataGrid source revision so every process uses the same
implementation.

The two controls use the same generated models, five data columns, an 800 x 500
native desktop window, fixed 24-pixel rows, layout, and a two-animation-frame
completion wait. Every measured operation begins from a fully rendered state. A
full collection is followed by an unmeasured three-pulse alignment barrier before each sample,
so implementation-specific GC duration cannot move the timed work to a different
Windows vsync phase. The timed two-frame completion barrier is armed at the sample
boundary before the synchronous mutation; its callbacks cannot run until mutation
and layout yield the UI thread, and both controls therefore enter the same frame
schedule.

- `ExpandAllAndRender` expands the 4,094-node binary-tree workload used by the
  existing native expansion comparison.
- `CollapseAllAndRender` starts with the sample's 149,792-node workload fully
  expanded, retains materialized children for equivalent post-collapse semantics,
  collapses to 32 roots, updates layout, and waits for rendered completion.
- `ScrollAndRender` moves through 32 deterministic offsets in the expanded
  hierarchy and records mutation, explicit layout, frame wait, and managed
  allocation for every jump. `--scroll-only` skips the other scenarios so the
  process can be used for focused scroll profiling and paired comparisons.
- Managed allocation is `GC.GetTotalAllocatedBytes` traffic during the timed
  operation. It is not retained heap, native allocation, RSS, or GPU memory.
- Collapse and scroll results also split the same end-to-end sample into synchronous model/UI
  mutation, `UpdateLayout`, and rendered-frame wait durations. These diagnostic
  phase means sum to the reported collapse mean; the end-to-end mean remains the
  primary reported comparison. The collapse optimization gate uses mutation plus
  layout and managed allocation, while the full rendered-frame total remains
  visible for detecting separate platform rendering or scheduling work.

## Frame alignment and the timed two-callback barrier

`RequestAnimationFrame` asks Avalonia to invoke a callback on an upcoming animation
clock pulse. It is not the composition batch's `Rendered` task or a GPU/display
presentation fence. A callback registered inside another callback cannot run in
the same pulse because Avalonia swaps its current and next callback queues before
invoking them. The nested barrier therefore crosses two animation-clock pulses:

1. after synchronous mutation and `UpdateLayout()` yield the UI thread, pulse A
   invokes callback 1 before Avalonia records and commits that UI render;
2. callback 1 queues callback 2 for a later pulse. Avalonia records dirty visuals,
   serializes and submits the composition batch, and suppresses another animation
   pulse until the render thread has deserialized the batch and marked it
   `Processed`; a later media pass then invokes callback 2 at pulse B.

`Processed` means that the batch was accepted/deserialized and will soon be
rendered. It is earlier than `Rendered`, so the second callback still does not
prove GPU presentation. The callback-1-to-callback-2 interval can include UI render
recording, composition serialization, render-thread scheduling and deserialization,
the processed notification, and scheduling the next UI media pass.

This is a conservative UI-completion convention, not a claim that the operation
intrinsically requires two frames. At a 60 Hz refresh rate each missed frame cutoff
adds about 16.7 ms. A small difference in synchronous work, GC duration, dispatcher
traffic, or the point within the vsync interval at which a sample starts can
therefore change the end-to-end result by a whole frame even when model and layout
work improve.

The harness controls that phase in two places. Before every measured sample it
performs the full GC and then waits on an unmeasured three-pulse barrier, preventing
implementation-specific collection time from deciding which side of the next
frame cutoff contains the timed operation. It then registers the timed barrier
before starting the synchronous mutation. Because mutation and `UpdateLayout()` run
on the UI thread, neither callback can execute until that work yields. Both source
implementations therefore enter the same callback schedule, and the result is split
into:

- **mutation**: hierarchy/model notifications and synchronous grid updates;
- **layout**: the explicit `UpdateLayout()` call;
- **frame wait**: dispatcher/render scheduling until the second callback;
- **total**: mutation + layout + frame wait.

The JSON records these callback phases for both collapse and every individual
scroll jump: the delay from layout completion to callback 1, the interval from
callback 1 to callback 2, the animation-clock interval, raw per-operation phase
samples, and the number of collapse `LayoutUpdated` notifications. The CI summary
reports frame pickup and the callback interval as separate scroll columns. If the
callback interval tracks the animation-clock interval, that portion is refresh
pacing rather than DataGrid execution and cannot be reduced by changing row or cell
layout. The CI diagnostic process
enables Avalonia's built-in meters and records the maximum UI render-recording,
compositor-update, and compositor-render pass observed for each sample. That
instrumented process is separate from the clean performance gate because meter
collection intentionally adds overhead.

The normal pre-sample alignment barrier has three callbacks. The third pulse is
important because callback 2 runs before Avalonia records the rest of its own media
pass. With only two alignment callbacks, ProDataGrid could begin the timed sample
while a trailing alignment-pass composition batch was still pending; Avalonia then
withheld timed callback 1 until that unrelated batch was processed. A source-only
Windows diagnostic changed only the unmeasured alignment count from two to three:
ProDataGrid's layout-to-callback-1 delay fell from 5.93 ms to 0.048 ms and its frame
wait fell from 24.03 ms to 13.84 ms. TreeDataGrid remained in the same frame band
(10.92 ms versus 12.08 ms). The measured operation still uses exactly two callbacks.

The diagnostic-only `--alignment-callbacks` option can override the pre-sample
count without changing that timed two-callback completion barrier. CI retains the
two-versus-three diagnostic and also captures sampled .NET traces for source-symbol
attribution of the UI render, composition serialization, and batch-deserialization
paths. These traces contain only the two source implementations in this harness.

The full total is still the observable latency under this completion convention.
For the collapse-path performance gate, mutation + layout isolates the code path
the change is intended to optimize; allocation independently measures managed
traffic. A frame-wait regression remains visible and must be investigated, but a
whole-frame scheduling band is not attributed to hierarchy traversal without phase
evidence.

## Why `LayoutUpdated` mattered

`LayoutUpdated` is raised by Avalonia's layout manager after a root layout pass; a
control's subscription is forwarded from that root event. It is not a render or
presentation notification and can be raised even if that control's bounds did not
change. A handler that changes measure-, arrange-, or render-affecting state can
queue later work, and another layout pass raises another notification. ProDataGrid's
hierarchy-change path used
`RequestHierarchicalIndentationRefresh()` to subscribe a one-shot handler, with a
background-dispatcher fallback. That is useful when realized rows are not yet in
their final range: the refresh must wait until layout has established the range.

The optimized bulk-splice path is different. It synchronously realizes/rebinds the
final displayed range and calls `RefreshHierarchicalIndentation()` before
`EnsureDisplayedRowsInRange()` and `InvalidateMeasure()`. Registering the deferred
`LayoutUpdated` refresh afterward repeated work that had already been completed.
When the explicit `UpdateLayout()` raised `LayoutUpdated`, the handler refreshed
indentation after the pass and could invalidate visual state for another layout or
render interval. The optimized path now skips only that redundant deferred request;
non-bulk paths retain it. A headless regression test verifies that bulk expand and
collapse each perform exactly one indentation refresh, preserving the final row
state while removing the duplicate post-layout work.

The CI comparison runs four independent processes for all five ProDataGrid source
modes and the pinned TreeDataGrid source mode, reversing the complete mode order in
alternating processes. Every process performs two warmups and ten measurements. The
report aggregates the four process means and includes a Student-t 95% confidence
interval. Raw JSON and `aggregate.json` are uploaded as the
`hierarchy-native-source-windows` artifact.

Build both source applications locally:

```sh
dotnet build tests/ProDataGrid.Hierarchy.NativeBenchmarks/Native.Pro/Native.Pro.csproj -c Release
dotnet build tests/ProDataGrid.Hierarchy.NativeBenchmarks/Native.Tree/Native.Tree.csproj -c Release \
  -p:TreeDataGridSourceRoot=/absolute/path/to/Avalonia.Controls.TreeDataGrid
```

Run one process per implementation:

```sh
GRID_BENCH_PRO_MODE=direct-cell dotnet \
  tests/ProDataGrid.Hierarchy.NativeBenchmarks/Native.Pro/bin/Release/net8.0/Native.Pro.dll \
  --warmup 2 --iterations 10 --output /tmp/pro-direct-cell.json

GRID_BENCH_PRO_MODE=virtual dotnet \
  tests/ProDataGrid.Hierarchy.NativeBenchmarks/Native.Pro/bin/Release/net8.0/Native.Pro.dll \
  --scroll-only --scroll-jumps 32 --warmup 2 --iterations 10 \
  --output /tmp/pro-virtual.json

GRID_BENCH_PRO_MODE=virtual-checkbox dotnet \
  tests/ProDataGrid.Hierarchy.NativeBenchmarks/Native.Pro/bin/Release/net8.0/Native.Pro.dll \
  --scroll-only --scroll-jumps 32 --warmup 2 --iterations 10 \
  --output /tmp/pro-virtual-checkbox.json

GRID_BENCH_PRO_MODE=flat-direct-cell dotnet \
  tests/ProDataGrid.Hierarchy.NativeBenchmarks/Native.Pro/bin/Release/net8.0/Native.Pro.dll \
  --scroll-only --scroll-jumps 32 --warmup 2 --iterations 10 \
  --output /tmp/pro-flat-direct-cell.json

GRID_BENCH_PRO_MODE=flat-drawn dotnet \
  tests/ProDataGrid.Hierarchy.NativeBenchmarks/Native.Pro/bin/Release/net8.0/Native.Pro.dll \
  --scroll-only --scroll-jumps 32 --warmup 2 --iterations 10 \
  --output /tmp/pro-flat-drawn.json

dotnet \
  tests/ProDataGrid.Hierarchy.NativeBenchmarks/Native.Tree/bin/Release/net8.0/Native.Tree.dll \
  --scroll-only --scroll-jumps 32 --warmup 2 --iterations 10 \
  --output /tmp/tree.json
```

On a machine without .NET 8, `DOTNET_ROLL_FORWARD=Major` can run the net8.0
applications on a newer installed runtime, but the report must record that change.
Use the same runtime and machine for both implementations.

The `virtual` mode uses `DataGridVisualLayoutMode.Virtualized` with the flat row
theme and typed column accessors. Harness validation requires zero realized
`DataGridCell` controls for every measured display state; a retained fallback is
reported as a validation failure rather than being mislabeled as a virtual run.
`flat-direct-cell` and `flat-drawn` use the centralized flat row/cell layout with
the corresponding retained or drawn cell content. Add `--avalonia-diagnostics`
only to a separate diagnostic run; it records Avalonia UI-render and compositor
meter durations but intentionally adds measurement overhead.

`virtual-checkbox` replaces the payload text column with a typed
`DataGridCheckBoxColumn`. It validates that the checkbox remains on the single
surface and that no retained display cells are realized. The diagnostic-only
`GRID_BENCH_ALLOW_VIRTUAL_FALLBACK=1` override exists for matched historical
baseline experiments; never set it when validating the candidate backend.

The complete experiment design and interpretation rules are in
[Layout performance benchmark methodology](../../docfx/articles/layout-performance-benchmarking.md).

See [the 2026-08-12 focused scroll report](../ProDataGrid.FlatLayout.Benchmarks/SCROLL-RESULTS-2026-08-12.md)
for the paired renderer optimization and flat-versus-nested source results.
