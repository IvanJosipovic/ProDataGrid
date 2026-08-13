# Lightweight virtual-row results — 2026-08-13

This experiment removes retained `DataGridRow` controls from the eligible
fixed-height virtual-surface steady state. `DataGridRowsPresenter` now owns a
bounded list of lightweight row records, and `ScrollSlotsByHeight` updates the
visible slot range arithmetically without row recycling, retargeting, generation,
visual-tree mutation, or row-control layout.

The baseline is commit `78cca89d` (retained zero-cell row retargeting). The
candidate is this change. Both variants were built in Release and run as separate
native processes on macOS Arm64, .NET 10.0.9, Avalonia 12.1, an 800 × 500 window at
2× scale, 4,094 expanded hierarchy rows, five fixed-width columns, 32 deterministic
scroll jumps, and `DOTNET_TieredCompilation=0`. Raw JSON is retained under
`artifacts/performance/lightweight-virtual-rows-2026-08-13/`.

## Clean timing

Four alternating baseline/candidate processes used three warmups and eight measured
iterations. Values are the mean of the four process means.

| Per jump | Baseline | Lightweight rows | Change |
|---|---:|---:|---:|
| Mutation | 0.10631 ms | **0.09681 ms** | **−8.9%** |
| Explicit layout | 0.15951 ms | **0.08382 ms** | **−47.5%** |
| Mutation + layout | 0.26582 ms | **0.18063 ms** | **−32.0%** |
| Managed allocation | 81,322 B | **76,526 B** | **−5.9%** |
| Full wall | 8.24232 ms | 8.22099 ms | −0.3% |
| Frame wait | 7.97650 ms | 8.04036 ms | +0.8% |

Wall and frame wait remain in the same approximately 8 ms animation-clock band.
They include deliberate idle refresh pacing and are not interpreted as DataGrid
work.

## Diagnostic ownership

Three separate alternating process pairs enabled both Avalonia and ProDataGrid
diagnostics, with three warmups and three measured iterations. Instrumented values
are attribution evidence and are not mixed with the clean timing gate.

| Active component per jump | Baseline | Lightweight rows | Change |
|---|---:|---:|---:|
| Mutation | 0.17428 ms | **0.15287 ms** | **−12.3%** |
| Explicit layout | 0.24453 ms | **0.12150 ms** | **−50.3%** |
| UI render recording | 0.42579 ms | **0.34688 ms** | **−18.5%** |
| Compositor update | 0.02905 ms | **0.01956 ms** | **−32.7%** |
| Compositor render | 0.61999 ms | **0.48594 ms** | **−21.6%** |
| **Active work** | 1.49364 ms | **1.12675 ms** | **−24.6%** |

Active work is mutation + explicit layout + UI render recording + compositor
update + compositor render. It excludes frame wait.

The intended virtual-layout owners moved more sharply:

| ProDataGrid diagnostic per jump | Baseline | Lightweight rows | Change |
|---|---:|---:|---:|
| `ScrollSlotsByHeight` | 0.15348 ms | **0.03955 ms** | **−74.2%** |
| Displayed-row update | 0.10497 ms | **0.01053 ms** | **−90.0%** |
| Row measure | 0.00528 ms | **0.00465 ms** | **−12.0%** |
| Managed allocation | 52,084 B | **46,738 B** | **−10.3%** |

The candidate records about 0.00526 ms of direct lightweight-row arrange work;
the baseline reported arrangement reuse for retained rows rather than an arrange
duration. The enclosing explicit-layout result still falls by 50.3%.

## Structural result and compatibility

| Steady-state virtual structure | Baseline | Lightweight rows |
|---|---:|---:|
| Retained rows | 20 | **0** |
| Retained display cells | 0 | **0** |
| Visuals / controls | 102 / 102 | **62 / 62** |

This is a 39.2% reduction in realized visuals and controls for the native workload.
Extent, viewport, row count, selection, pointer hit testing, hierarchy expansion,
and drawn values remain validated. Retained rows are materialized for editing and
automation, and the optimized path is disabled for row headers/numbers, details,
row lifecycle handlers, grouping/collapsed slots, custom grids or factories, and
item-owned row containers.

The result meets the specific target: `ScrollSlotsByHeight` is reduced by more than
50%, clean synchronous mutation+layout work falls by 32.0%, and instrumented Active
work falls by 24.6%. The deliberately awaited frame cadence remains unchanged.
