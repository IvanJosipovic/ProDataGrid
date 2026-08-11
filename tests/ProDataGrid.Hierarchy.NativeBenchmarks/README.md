# Native hierarchy source comparison

This harness compares ProDataGrid with Wieslaw's open-source TreeDataGrid using
source project references only. It has no paid-grid assembly or package reference.
The workflow pins the TreeDataGrid source revision so every process uses the same
implementation.

The two controls use the same generated models, five data columns, an 800 x 500
native desktop window, fixed 24-pixel rows, layout, and a two-animation-frame
completion wait. Every measured operation begins from a fully rendered state. A
full collection is followed by an unmeasured two-frame barrier before each sample,
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
- Managed allocation is `GC.GetTotalAllocatedBytes` traffic during the timed
  operation. It is not retained heap, native allocation, RSS, or GPU memory.
- Collapse results also split the same end-to-end sample into synchronous model/UI
  mutation, `UpdateLayout`, and rendered-frame wait durations. These diagnostic
  phase means sum to the reported collapse mean; the end-to-end mean remains the
  primary reported comparison. The collapse optimization gate uses mutation plus
  layout and managed allocation, while the full rendered-frame total remains
  visible for detecting separate platform rendering or scheduling work.

## Frame alignment and the two-callback barrier

`RequestAnimationFrame` asks Avalonia to invoke a callback at an upcoming animation
and render scheduling opportunity. It is not a fence proving that the compositor,
GPU, or display has presented the preceding pixels. A callback registered inside
another callback cannot run in the same callback turn, so the nested callback used
by this harness crosses two scheduling opportunities:

1. the first callback lets invalidation, layout, animation, and render scheduling
   triggered by the operation reach the next frame opportunity;
2. the nested callback waits for the following opportunity, giving work queued by
   the first frame another dispatcher/render turn before the sample completes.

This is a conservative UI-completion convention, not a claim that the operation
intrinsically requires two frames. At a 60 Hz refresh rate each missed frame cutoff
adds about 16.7 ms. A small difference in synchronous work, GC duration, dispatcher
traffic, or the point within the vsync interval at which a sample starts can
therefore change the end-to-end result by a whole frame even when model and layout
work improve.

The harness controls that phase in two places. Before every measured sample it
performs the full GC and then waits on an unmeasured two-callback barrier, preventing
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

The JSON also records the delay from layout completion to callback 1, the interval
from callback 1 to callback 2, the animation-clock interval, raw per-iteration phase
samples, and the number of `LayoutUpdated` notifications. The CI diagnostic process
enables Avalonia's built-in meters and records the maximum UI render-recording,
compositor-update, and compositor-render pass observed for each sample. That
instrumented process is separate from the clean performance gate because meter
collection intentionally adds overhead.

The full total is still the observable latency under this completion convention.
For the collapse-path performance gate, mutation + layout isolates the code path
the change is intended to optimize; allocation independently measures managed
traffic. A frame-wait regression remains visible and must be investigated, but a
whole-frame scheduling band is not attributed to hierarchy traversal without phase
evidence.

## Why `LayoutUpdated` mattered

`LayoutUpdated` is raised after an Avalonia layout pass and may occur repeatedly as
layout is invalidated. ProDataGrid's hierarchy-change path used
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

dotnet \
  tests/ProDataGrid.Hierarchy.NativeBenchmarks/Native.Tree/bin/Release/net8.0/Native.Tree.dll \
  --warmup 2 --iterations 10 --output /tmp/tree.json
```

On a machine without .NET 8, `DOTNET_ROLL_FORWARD=Major` can run the net8.0
applications on a newer installed runtime, but the report must record that change.
Use the same runtime and machine for both implementations.
