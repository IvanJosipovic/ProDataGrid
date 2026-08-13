# Virtual surface render attribution and selection fast path

Date: 2026-08-13

## Scope

This experiment starts after the rowless virtual-surface work removed retained
rows and cells from the fixed-height scrolling path. The remaining active owners
were UI render recording and compositor rendering rather than row generation or
`ScrollSlotsByHeight`.

The change adds direct surface-render diagnostics and avoids querying the selected
cell dictionary for every drawn cell when the grid has no selected cells. It also
resolves the current column once per visible row instead of comparing the current
slot and column for every cell. Selected and current cells retain the existing
drawing path.

## Diagnostic contract

`--prodatagrid-diagnostics` now records these virtual-surface instruments:

- `prodatagrid.virtual.surface.render.time`;
- rendered row and cell counts;
- partial-cell clip count;
- vertical-grid-line and hierarchy-expander draw-operation counts; and
- text-layout cache hit and miss counts.

Counters are aggregated locally and published once per render pass. The standard
`virtual` workload records 20 rows, 100 cells, and a warm-cache steady state. Its
grid lines are disabled, so vertical-grid-line batching is not a valid explanation
for this workload's render cost.

## Environment and protocol

- macOS 26.6, Arm64, 11 logical processors;
- .NET 10.0.5 running the `net8.0` application with
  `DOTNET_ROLL_FORWARD=Major`;
- Avalonia 12.1.0, Skia, 800 × 500 window, render scale 2;
- `DOTNET_TieredCompilation=0`;
- `GRID_BENCH_PRO_MODE=virtual`;
- 32 discontinuous scroll jumps per iteration;
- three warmup iterations;
- ten measured iterations per process; and
- three alternating baseline/candidate process pairs.

The diagnostic lane used the same process order with
`--prodatagrid-diagnostics`. Raw JSON is retained under the gitignored
`artifacts/performance/virtual-gridline-batching` directory.

## Clean results

The clean lane does not enable Avalonia or ProDataGrid meters. Mutation plus
explicit layout is shown separately from the frame-paced completion interval.

| Metric per jump | Baseline | Candidate | Change |
|---|---:|---:|---:|
| Mutation + explicit layout | 0.2429 ms | 0.2510 ms | +3.3% |
| Managed allocation | 78,295.7 B | 78,301.2 B | +0.01% |
| Full wall time | 8.2252 ms | 8.2236 ms | -0.02% |
| Frame wait | 7.9823 ms | 7.9726 ms | -0.12% |

The clean synchronous movement is small and in process noise. Full wall time stays
in the same animation-clock band, as expected; it is not used to judge the render
fast path.

## Targeted diagnostic result

| Metric per jump | Baseline | Candidate | Change |
|---|---:|---:|---:|
| Virtual surface render | 0.5504 ms | 0.5194 ms | **-5.6%** |
| Managed allocation | 79,299.9 B | 79,274.3 B | -0.03% |

This is the directly owned stage. The result does not claim a 5.6% wall-time gain:
the clean total is dominated by the intentionally awaited frame interval.

Structural validation remained unchanged: 4,094 logical rows, zero retained rows,
zero retained cells, 62 visuals, and 62 controls.

## Rejected experiments

Several plausible micro-optimizations were measured and discarded:

- Suppressing LRU promotion while the text-layout cache was below capacity did not
  produce a repeatable render improvement.
- Combining hierarchy chevrons into one `StreamGeometry` reduced expander draw
  operations from about 20 per jump to one, but geometry construction and
  tessellation increased UI and compositor render time.
- Reusing mutable `Pen` instances reduced allocation but regressed the balanced
  render-stage aggregate.
- Avoiding construction of the disabled vertical-grid pen alone also reduced
  allocation but did not improve active render work.

These results reinforce the acceptance rule: fewer commands or allocations are not
accepted when the directly measured active stage regresses.

## Remaining owner

The warm workload still draws 100 cached text layouts per jump. The next material
render experiment should target per-cell text command recording/serialization,
potentially through a carefully bounded immediate/custom draw operation. It must
preserve clipping, selection, current-cell chrome, text fallback, disposal, and
render-thread ownership, and it requires paired UI/compositor evidence before it is
adopted.
