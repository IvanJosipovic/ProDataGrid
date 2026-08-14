# ProDataGrid flat-layout benchmarks

This BenchmarkDotNet suite compares the nested compatibility architecture, the
opt-in flat sibling row/cell surface, and the virtual single-cell surface. All
three use the same 149,792-node
hierarchy, 1200 × 760 headless host, 520-pixel DataGrid viewport, and six cell
paths from the optimized hierarchy sample.

The authoritative cross-suite phase definitions, structural gates, A/B run
ordering, frame-pacing interpretation, and memory rules are documented in
[Layout performance benchmark methodology](../../docfx/articles/layout-performance-benchmarking.md).

`HierarchyCollapseLayoutBenchmarks` measures the pending Avalonia layout pass
after `CollapseAll`. `HierarchyCollapseEndToEndBenchmarks` measures
`CollapseAll` plus dispatcher/layout completion. Iteration setup expands and
fully lays out the hierarchy outside the measured operation. The memory
diagnoser reports managed allocation traffic and GC collections per operation;
it does not represent retained heap, native allocations, RSS, or GPU memory.

Build once, then run the full clean comparison:

```sh
dotnet build tests/ProDataGrid.FlatLayout.Benchmarks/ProDataGrid.FlatLayout.Benchmarks.csproj \
  -c Release --no-restore

DOTNET_TieredCompilation=0 dotnet run \
  --project tests/ProDataGrid.FlatLayout.Benchmarks/ProDataGrid.FlatLayout.Benchmarks.csproj \
  -c Release --no-build -- \
  --filter '*HierarchyCollapse*Benchmarks*' \
  --launchCount 3 \
  --warmupCount 3 \
  --iterationCount 10 \
  --invocationCount 1 \
  --unrollFactor 1 \
  --allStats
```

Disabling tiered compilation prevents a short, single-collapse iteration from
switching generated-code tiers midway through a launch. To isolate one cell
path while investigating a regression, set
`PRODATAGRID_BENCHMARK_CELL_PATH=OptimizedTheme`. The unfiltered run covers all
six paths.

For a separate allocation or CPU trace, the benchmark executable also provides
an unmeasured repetition loop:

```sh
DOTNET_TieredCompilation=0 dotnet run \
  --project tests/ProDataGrid.FlatLayout.Benchmarks/ProDataGrid.FlatLayout.Benchmarks.csproj \
  -c Release --no-build -- --profile flat 100 BuiltInDrawn
```

Use `nested` for the compatibility path or `virtual` for the zero-display-cell
surface. Add `inspect` after the cell path to print realized visual and
layout-validity statistics. Use
`--profile-end-to-end flat 100 BuiltInDrawn` to mark the complete collapse and
layout operation. Profiler entry points must not be reported as BenchmarkDotNet
timing evidence.

Keep `BenchmarkDotNet.Artifacts` with the exact commit/dirty state and machine
metadata. Run intrusive profilers separately from this clean timing pass.

The checked-in report for the current implementation is
[RESULTS-2026-08-12.md](RESULTS-2026-08-12.md). The virtual-surface follow-up is
[VIRTUAL-SURFACE-RESULTS-2026-08-12.md](VIRTUAL-SURFACE-RESULTS-2026-08-12.md).
The latest scroll, cache, recycling, and typed-checkbox comparisons are in
[SCROLL-RESULTS-2026-08-12.md](SCROLL-RESULTS-2026-08-12.md).
The focused fixed-height row-window retarget optimization is documented in
[VIRTUAL-RETARGET-RESULTS-2026-08-13.md](VIRTUAL-RETARGET-RESULTS-2026-08-13.md).
The follow-up lifecycle-counter batching result is documented in
[VIRTUAL-ROW-BATCH-RESULTS-2026-08-13.md](VIRTUAL-ROW-BATCH-RESULTS-2026-08-13.md).
The retarget-apply decomposition and sparse-state follow-up is documented in
[VIRTUAL-ROW-APPLY-RESULTS-2026-08-13.md](VIRTUAL-ROW-APPLY-RESULTS-2026-08-13.md).
The rowless virtual-layout and direct `ScrollSlotsByHeight` result is documented in
[VIRTUAL-LIGHTWEIGHT-ROWS-RESULTS-2026-08-13.md](VIRTUAL-LIGHTWEIGHT-ROWS-RESULTS-2026-08-13.md).
The subsequent text-command batching and lightweight projection results are
documented in
[VIRTUAL-SURFACE-TEXT-BATCH-RESULTS-2026-08-13.md](VIRTUAL-SURFACE-TEXT-BATCH-RESULTS-2026-08-13.md)
and
[VIRTUAL-LAYOUT-PROJECTION-RESULTS-2026-08-13.md](VIRTUAL-LAYOUT-PROJECTION-RESULTS-2026-08-13.md).

The discontinuous, line, and fractional virtual-scroll workload definitions plus
the precomputed cell-clip evidence are documented in
[VIRTUAL-SMOOTH-SCROLL-RESULTS-2026-08-13.md](VIRTUAL-SMOOTH-SCROLL-RESULTS-2026-08-13.md).
The follow-up that prepares renderer, accessor, text-style, culture, and brush-key
state once per visible column is documented in
[VIRTUAL-COLUMN-RENDER-PLAN-RESULTS-2026-08-13.md](VIRTUAL-COLUMN-RENDER-PLAN-RESULTS-2026-08-13.md).
The retained Flat-mode overlapping-row and fractional-overscan optimization is
documented in
[FLAT-ROW-RETARGET-RESULTS-2026-08-13.md](FLAT-ROW-RETARGET-RESULTS-2026-08-13.md).
The disjoint-window follow-up that removes full-viewport recycling and generation
is documented in
[FLAT-DISCONTINUOUS-RETARGET-RESULTS-2026-08-13.md](FLAT-DISCONTINUOUS-RETARGET-RESULTS-2026-08-13.md).
The fresh full-matrix and native TreeDataGrid comparison is documented in
[TDG-VIRTUAL-COMPARISON-RESULTS-2026-08-13.md](TDG-VIRTUAL-COMPARISON-RESULTS-2026-08-13.md).
