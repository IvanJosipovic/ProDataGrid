# Flat retained-row discontinuous-scroll retarget results — 2026-08-13

## Scope

This follow-up extends the retained `Flat` row-window optimization documented in
`FLAT-ROW-RETARGET-RESULTS-2026-08-13.md`. The earlier implementation retained
overlapping windows but deliberately returned to row recycling when a scroll moved
by at least the realized row count. A discontinuous jump therefore recycled and
generated the entire 20-row / 100-cell viewport even when every container was an
eligible default fixed-height row with reusable cell content.

The rowless `Virtualized` cell surface is not affected: it already has zero retained
rows and cells in its steady-state scroll lane. This change targets the retained
flat compatibility lane used for direct cells, editing-compatible content, and
other cases that require real Avalonia controls.

## Architecture change

`TryScrollDefaultFlatRows` now offers both overlapping and disjoint fixed-height
windows to the same transactional in-place retarget operation. The operation:

1. validates the complete old row window and builds every target row/item entry;
2. rotates the logical row order with bounded modular arithmetic;
3. rebinds only rows whose slot changes;
4. preserves the normal recycle/generate pipeline as an all-or-nothing fallback;
5. clears pointer-over state on a leaving row before checking recyclability, which
   matches the established ordinary recycle lifecycle; and
6. invalidates child indexes and arrange once for the completed batch.

The pointer-state ordering is material. Before it was corrected, a stationary
pointer made 92 of 256 measured operations reject the batch after eligibility was
tested. The ordinary recycle path already clears that transient state first because
an offscreen row cannot remain pointer-over. Applying the same rule makes all 256
candidate operations retarget 20 rows and recycle/generate zero rows.

The existing guards still exclude derived grids or rows, custom realization,
current/focused rows, lifecycle handlers, row details/headers, grouping, search or
conditional formatting, auto-sized columns, drawn cells, and non-reusable content.

## Method

The baseline is commit `d3c77fc7` (`Optimize flat retained row scrolling`). The
candidate is that commit plus the disjoint-window and pointer-state changes. Both
were built in Release and executed as separate native processes on macOS 26.6
Arm64, .NET 10.0.5, Avalonia 12.1/Skia, an 800 × 500 window at 2× scale, 4,094
expanded hierarchy rows, five fixed-width direct-cell columns, and 32 discontinuous
scroll operations per iteration. Tiered compilation was disabled. Process order
alternated in every pair.

Diagnostic runs used three warmups, eight measured iterations, Avalonia rendering
meters, and ProDataGrid component meters. Meter-free runs used three warmups and
twelve measured iterations. Raw JSON is gitignored under
`artifacts/performance/flat-discontinuous-retarget-2026-08-13/`.

Active work is mutation + layout + UI render recording + compositor update +
compositor render. It deliberately excludes the benchmark's frame-completion wait.

## Lifecycle ownership

Values are the median process mean across the five diagnostic pairs, per scroll
operation.

| Component / count | Baseline | Candidate |
|---|---:|---:|
| `ScrollSlotsByHeight` | 0.71490 ms | 0.56513 ms |
| Row generation | 0.50164 ms | 0 |
| Row recycling | 0.12718 ms | 0 |
| Retarget bind | 0 | 0.46165 ms |
| Rows retargeted | 0 | 20 |
| Rows recycled | 20 | 0 |

The lifecycle work is not simply deleted: all 20 rows still need their new items
and all 100 direct cells still need their new values. The optimization removes pool,
detach, acquire, prepare, and insertion work while preserving the necessary bind.

## Five-pair diagnostic results

| Pair | `ScrollSlotsByHeight` | Active work | Mutation + layout | Allocation |
|---:|---:|---:|---:|---:|
| 1 | −20.40% | −1.51% | −3.77% | −4.06% |
| 2 | −22.72% | −0.35% | −3.02% | −4.05% |
| 3 | −12.96% | +7.06% | +4.52% | −4.06% |
| 4 | −30.85% | −6.73% | −11.43% | −4.06% |
| 5 | −21.43% | −1.12% | −3.16% | −4.06% |
| **Pairwise median** | **−21.43%** | **−1.12%** | **−3.16%** | **−4.06%** |

All five pairs reduce the target component and allocation. Four of five reduce
active work and mutation + layout. The target reduction is below 50% because the
candidate's 0.46165 ms retarget bind is required active work; removing all row
recycle/generate operations cannot remove the data and cell-value update itself.

## Meter-free guardrail

| Pair | Mutation + layout | Allocation | Wall mean | Frame wait | P95 wall |
|---:|---:|---:|---:|---:|---:|
| 1 | −0.93% | −3.82% | +90.93% | +117.48% | +83.16% |
| 2 | +1.15% | −3.86% | +88.67% | +111.01% | +78.93% |
| 3 | +10.54% | −3.82% | +91.86% | +113.37% | +108.70% |
| 4 | +0.08% | −3.83% | +91.18% | +115.00% | +81.73% |
| 5 | −5.58% | −5.08% | +21.22% | +28.46% | +80.48% |
| **Pairwise median** | **+0.08%** | **−3.83%** | +90.93% | +113.37% | +81.73% |

Allocation improves in every meter-free pair and synchronous mutation + layout is
neutral at the median. The wall increase is entirely in the benchmark's asynchronous
frame-completion interval: the two-frame barrier lands on a later animation tick
after the lifecycle scheduling changes. It is retained as a guardrail and must not
be added to component timers or described as DataGrid CPU work. The separately
instrumented active-work total has a −1.12% median and does not reproduce a render
work regression.

## Correctness and fallbacks

Headless coverage performs a 200-row discontinuous move and verifies that:

- the complete row-container set is preserved;
- the retarget count increases by exactly the realized row count;
- every row, cell, slot, index, and item agrees after the jump; and
- no lifecycle event contract is widened.

The existing line, fractional, drawn-cell, loading-handler, editing, virtual-surface,
and transition tests remain in the focused flat-layout suite. A native `flat-drawn`
discontinuous smoke records zero retargets, 20 recycled rows, and 20 realized rows,
confirming that unsupported drawn content continues through the established
fallback.

## Rejected experiments

Two stronger-looking cell-level experiments were measured and removed:

- bypassing repeated direct-accessor configuration changed the target timer by only
  −0.25%, showing that Avalonia `DataContext` propagation—not accessor lookup—owns
  the retained-cell bind; and
- switching flat cells from explicit local data contexts to logical inheritance
  increased the target timer from about 0.111 ms to 0.191 ms in a line-scroll smoke.

Those results keep the public cell data-context semantics and the faster explicit
propagation path intact. The next cell-bind optimization must reduce property-system
work without weakening `DataContext`, change notification, editing, or fallback
semantics.

## Reproduction

```sh
GRID_BENCH_PRO_MODE=flat-direct-cell \
DOTNET_TieredCompilation=0 \
DOTNET_ROLL_FORWARD=Major \
dotnet exec tests/ProDataGrid.Hierarchy.NativeBenchmarks/Native.Pro/bin/Release/net8.0/Native.Pro.dll \
  --scroll-only --scroll-pattern discontinuous --scroll-jumps 32 \
  --warmup 3 --iterations 8 \
  --avalonia-diagnostics --prodatagrid-diagnostics \
  --output /tmp/flat-direct-discontinuous.json
```
