# Flat retained-row smooth-scroll retarget results — 2026-08-13

## Scope

The rowless `Virtualized` surface already records zero realized rows and cells, so
row recycling and generation cannot be optimized in that steady-state lane. This
follow-up targets the retained `Flat` lane used by direct and ordinary retained
cells. Before the change, even a one-row fixed-height scroll detached the leaving
row, placed it in the recycle pool, acquired it again, rebound it, prepared it, and
inserted it at the opposite edge.

The candidate adds a guarded overlapping-window path:

- fixed row and column geometry is calculated arithmetically;
- rows whose slots remain visible keep their item and position in the logical
  window;
- only entering rows are rebound;
- a same-slot fractional move keeps the existing overscan row and performs no row
  bind, generation, or recycle work; and
- the established recycle/generate path remains the fallback for drawn cells,
  discontinuous jumps, current or focused leaving rows, derived grids/rows,
  custom factories, lifecycle handlers, row details/headers, grouping, active
  search or conditional formatting, auto-sized columns, and non-reusable content.

The baseline is commit `9cf6221f` (`Precompute virtual surface cell clips`). The
candidate is that commit plus the flat retained-row changes described here. Both
variants were built in Release and run in separate native processes on macOS 26.6
Arm64, .NET 10.0.5, Avalonia 12.1/Skia, an 800 × 500 window at 2× scale, 4,094
expanded hierarchy rows, five fixed-width columns, and 32 scroll operations per
iteration. Tiered compilation was disabled and `DOTNET_ROLL_FORWARD=Major` ran the
net8.0 executable on .NET 10. Process order alternated between baseline/candidate
and candidate/baseline. Raw JSON is gitignored under
`artifacts/performance/flat-row-retarget-2026-08-13/`.

## Ownership before the change

One diagnostic process per retained cell mode used three warmups and eight measured
iterations. Values are means per scroll operation.

| Mode / pattern | `ScrollSlotsByHeight` | Row generation | Row recycle | Displayed-row update |
|---|---:|---:|---:|---:|
| Flat direct, line | 0.17319 ms | 0.09177 ms | 0.02390 ms | 0.11869 ms |
| Flat direct, fractional | 0.09372 ms | intermittent | intermittent | 0.07979 ms |
| Flat drawn, line | 0.12572 ms | 0.05450 ms | 0.01613 ms | 0.08415 ms |

The fractional retained window alternated between 20 and 21 rows because the two
offsets straddled the viewport's half-row boundary. Keeping the already-created
21st row as overscan removes that oscillating lifecycle work.

## Instrumented paired results

Diagnostic runs used three warmups, eight measured iterations, and both Avalonia
and ProDataGrid meters. The active-work score is mutation + layout + UI render
recording + compositor update + compositor render. It excludes frame wait.

### One-row line scroll

Five alternating process pairs exercised `flat-direct-cell`. The candidate recorded
one retargeted entering row per eligible operation and no generate/recycle timer in
that operation.

| Pair | `ScrollSlotsByHeight` change | Active-work change | Allocation change |
|---:|---:|---:|---:|
| 1 | +147.2% | +256.5% | −4.43% |
| 2 | −49.0% | −20.7% | −4.39% |
| 3 | −38.2% | −0.3% | −4.38% |
| 4 | −37.7% | −5.6% | −4.43% |
| 5 | −38.9% | −4.9% | −4.46% |
| **Pairwise median** | **−38.2%** | **−4.9%** | **−4.43%** |

Pair 1 is a process-level timing outlier: its baseline target timer was 0.08319 ms
while the other baselines were 0.19507–0.39810 ms, and its active score moved in
the same direction. It is retained in the report. Four subsequent pairs reproduce
the target and active-work improvement, while all five independently reproduce the
allocation reduction.

The line path does not reach a 50% target reduction because the entering row still
must receive its new item and update five direct cell values. It removes detach,
pool, acquire, preparation, and reinsertion work without treating the necessary
bind as idle.

### Same-slot fractional scroll

Three final-source diagnostic pairs exercised the alternating 0.375/0.625 offsets.

| Pair | `ScrollSlotsByHeight` change | Mutation + layout change | Active-work change | Allocation change |
|---:|---:|---:|---:|---:|
| 1 | −75.1% | −14.8% | −8.3% | −3.82% |
| 2 | −73.4% | −6.7% | +1.6% | −3.83% |
| 3 | −77.0% | −19.7% | −13.2% | −3.88% |
| **Pairwise median** | **−75.1%** | **−14.8%** | **−8.3%** | **−3.83%** |

This meets the requested greater-than-50% reduction for the targeted
`ScrollSlotsByHeight` component. The candidate has no retarget-validation sample
for same-slot operations because no row identity, item, or logical ordering changes.

## Meter-free guardrails

The clean lane disabled all Avalonia and ProDataGrid meters. Line used three process
pairs; final-source fractional used five. Values below are pairwise medians.

| Pattern | Mutation + layout | Allocation | Wall mean | Frame wait | P95 wall |
|---|---:|---:|---:|---:|---:|
| Line | **−5.0%** | **−3.00%** | −0.08% | +0.42% | −0.90% |
| Fractional | −5.5% | **−2.84%** | −0.15% | +0.04% | +0.93% |

All three line pairs reduced mutation + layout by 0.4–15.7%. Fractional synchronous
timing remained noisy (three wins and two losses), while allocation improved in all
five pairs and wall means stayed between −0.56% and +0.09%. Four fractional P95
pairs were between +0.22% and +1.51%; one candidate process had a 10.46 ms P95
outlier while still improving its wall mean by 0.56%. Two added pairs did not
reproduce that spike.

Frame wait is reported as a guardrail, not as active DataGrid work. It includes the
animation-clock completion convention and cannot be added to mutation/layout or
render component timers to infer CPU ownership.

## Guarded fallback validation

Headless tests prove that:

- a one-row move preserves all overlapping row identities and rotates only the
  entering row;
- every retargeted row and flat cell receives the correct item;
- fractional movement keeps the overscan row window attached with no retarget;
- drawn cells continue through recycling; and
- a `LoadingRow` handler disables the fast path.

The discontinuous native smoke likewise records 20 recycled rows and no flat-row
retarget, while the drawn line smoke records one recycled row and no retarget. This
prevents the optimization from repeating the measured drawn-cell and discontinuous
regressions observed during development.

## Rejected surface-line batching experiment

A preceding experiment replaced hierarchy expander and combo-box chevron line
commands with one pooled custom scene operation. A Skia parity test showed identical
clipped pixels and diagnostics proved one scene operation replaced 40–46 line
segments, but paired timings were inconsistent and managed allocation increased by
about 0.5%. The experiment was removed. This is why the retained-row lifecycle
owner—not frame wait or a visually plausible command-count reduction—became the
accepted target.

## Reproduction

Build once, then run one process per variant and pattern:

```sh
dotnet build tests/ProDataGrid.Hierarchy.NativeBenchmarks/Native.Pro/Native.Pro.csproj \
  -c Release --no-restore

GRID_BENCH_PRO_MODE=flat-direct-cell \
DOTNET_TieredCompilation=0 \
DOTNET_ROLL_FORWARD=Major \
dotnet exec tests/ProDataGrid.Hierarchy.NativeBenchmarks/Native.Pro/bin/Release/net8.0/Native.Pro.dll \
  --scroll-only --scroll-pattern line --scroll-jumps 32 \
  --warmup 3 --iterations 8 \
  --avalonia-diagnostics --prodatagrid-diagnostics \
  --output /tmp/flat-direct-line.json

GRID_BENCH_PRO_MODE=flat-direct-cell \
DOTNET_TieredCompilation=0 \
DOTNET_ROLL_FORWARD=Major \
dotnet exec tests/ProDataGrid.Hierarchy.NativeBenchmarks/Native.Pro/bin/Release/net8.0/Native.Pro.dll \
  --scroll-only --scroll-pattern fractional --scroll-jumps 32 \
  --warmup 3 --iterations 15 \
  --output /tmp/flat-direct-fractional-clean.json
```
