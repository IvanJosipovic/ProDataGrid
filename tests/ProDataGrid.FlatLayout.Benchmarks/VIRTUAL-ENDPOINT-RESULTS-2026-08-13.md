# Current retained-to-virtual endpoint results — 2026-08-13

## Decision

The current virtual architecture meets the requested 50% total-work reduction
against the legacy retained architecture for every declared native scroll pattern.
The matched median Active-work reductions are:

| Pattern | Legacy retained | Current virtual | Median paired change |
|---|---:|---:|---:|
| Discontinuous | 4.95238 ms | **0.91976 ms** | **−81.1%** |
| Line | 2.39029 ms | **1.13309 ms** | **−51.3%** |
| Fractional | 1.88405 ms | **0.77514 ms** | **−58.9%** |

These are endpoint comparisons, not compounded percentages from separate commits.
Both modes were built from commit `64152499` and executed from the same native
assembly in independent alternating processes.

The stopping decision is therefore to finalize the architecture rather than add a
row-scene cache. That cache would introduce another invalidation and render-thread
lifetime boundary after the requested product target has already been reached.

## Compared architectures

`GRID_BENCH_PRO_MODE=standard` selects the legacy nested retained topology.
`GRID_BENCH_PRO_MODE=virtual` selects the eligible rowless virtual surface.

Every process validated the same 4,094-row hierarchy and geometry:

| Structure | Legacy retained | Current virtual |
|---|---:|---:|
| Retained rows | 20 | **0** |
| Retained cells | 120 | **0** |
| Visuals | 2,131 | **62** |
| Controls | 2,131 | **62** |
| Extent | 760 × 98,256 | 760 × 98,256 |
| Viewport | 800 × 468 | 800 × 468 |

The result therefore does not come from rendering fewer logical rows or using a
different viewport.

## Environment and protocol

- macOS 26.6 (25G72), Arm64;
- Apple M3 Pro, 11 cores, 18 GB;
- commit `64152499637f29fa8e04a41ce40d88b068559b86`;
- .NET SDK 10.0.201; the net8.0 benchmark ran on .NET 10 through
  `DOTNET_ROLL_FORWARD=Major`;
- Avalonia 12.1.0, Skia, 800 × 500 window, render scale 2;
- `DOTNET_TieredCompilation=0`;
- 4,094 expanded hierarchy rows and five fixed-width data columns;
- 32 deterministic scroll operations per iteration;
- three warmups and ten measured iterations per diagnostic process;
- three warmups and 15 measured iterations per clean process;
- alternating process order (`standard virtual`, `virtual standard`,
  `standard virtual`);
- seven diagnostic pairs for line scrolling because the initial three exposed one
  compositor-noisy reversal; and
- three diagnostic and three clean pairs for discontinuous and fractional
  scrolling, plus three clean line pairs.

Raw JSON is gitignored under
`artifacts/performance/current-endpoint-2026-08-13/`.

## What total work means

Active work is:

```text
scroll mutation + explicit layout + UI render recording
                + compositor update + compositor render
```

Frame wait is excluded because it is the harness's two-animation-callback barrier
and includes refresh-clock pacing. It is not CPU work owned by the DataGrid. The
surface timer is nested within UI render recording and is not added again.

Active is an attribution score rather than literal single-thread CPU time: UI and
render work can overlap, and compositor meters report the maximum associated pass.
It is nevertheless the repository's declared total-work comparison and directly
answers the requested target.

## Active component results

Values are means of process means. Changes are medians of paired percentage
changes.

### Discontinuous

| Component | Legacy retained | Current virtual | Median paired change |
|---|---:|---:|---:|
| Mutation | 0.08546 ms | 0.08139 ms | −3.5% |
| Explicit layout | 3.46109 ms | **0.07074 ms** | **−97.9%** |
| UI render recording | 0.32678 ms | 0.32489 ms | −1.8% |
| Compositor update | 0.31124 ms | **0.01712 ms** | **−94.4%** |
| Compositor render | 0.76783 ms | **0.42563 ms** | **−44.9%** |
| **Active work** | **4.95238 ms** | **0.91976 ms** | **−81.1%** |
| `ScrollSlotsByHeight` attribution | 1.88833 ms | **0.02060 ms** | **−98.9%** |

All three Active pairs improved: −82.7%, −80.5%, and −81.1%.

### Line

| Component | Legacy retained | Current virtual | Median paired change |
|---|---:|---:|---:|
| Mutation | 0.11179 ms | 0.12522 ms | +7.7% |
| Explicit layout | 0.82017 ms | **0.09942 ms** | **−88.6%** |
| UI render recording | 0.09166 ms | 0.32970 ms | +257.7% |
| Compositor update | 0.05487 ms | **0.02019 ms** | **−62.4%** |
| Compositor render | 1.31181 ms | **0.55857 ms** | **−57.3%** |
| **Active work** | **2.39029 ms** | **1.13309 ms** | **−51.3%** |
| `ScrollSlotsByHeight` attribution | 0.39583 ms | **0.02744 ms** | **−92.8%** |

The seven paired Active changes were −51.3%, +5.8%, −78.4%, −61.6%, −38.8%,
−51.2%, and −65.4%. Six of seven improved; the median is −51.3%, and the change
between the two seven-process mean aggregates is −52.6%. The one reversal moved
with all compositor-related values in that process and is retained rather than
excluded.

The virtual surface deliberately moves some work into UI render recording. The
net win comes from removing the retained visual matrix, its layout, and its larger
compositor scene.

### Fractional

| Component | Legacy retained | Current virtual | Median paired change |
|---|---:|---:|---:|
| Mutation | 0.12006 ms | **0.09781 ms** | **−19.9%** |
| Explicit layout | 0.32326 ms | **0.08092 ms** | **−74.8%** |
| UI render recording | 0.06076 ms | 0.14463 ms | +133.0% |
| Compositor update | 0.04294 ms | **0.01517 ms** | **−55.3%** |
| Compositor render | 1.33703 ms | **0.43661 ms** | **−67.2%** |
| **Active work** | **1.88405 ms** | **0.77514 ms** | **−58.9%** |
| `ScrollSlotsByHeight` attribution | 0.14505 ms | **0.02205 ms** | **−85.1%** |

All three Active pairs improved: −59.6%, −57.9%, and −58.9%.

## Allocation

| Pattern | Diagnostic median | Clean median | Interpretation |
|---|---:|---:|---|
| Discontinuous | **−87.9%** | **−87.8%** | Removes retained row/cell retarget traffic |
| Line | **−28.3%** | **−24.6%** | Reuses projected items and formatted values |
| Fractional | +3.1% | +5.6% | About 1.5 KB/op more than already-stable retained rows |

The fractional allocation regression is real and documented. It is small in
absolute terms and accompanies a 58.9% Active reduction, but it remains a residual
optimization opportunity rather than being hidden by the total-work result.

## Meter-free guardrails

| Pattern | Mutation + layout | Allocation | Full wall | Frame wait | p95 wall |
|---|---:|---:|---:|---:|---:|
| Discontinuous | **−94.7%** | **−87.8%** | −3.3% | +55.5% | −0.4% |
| Line | **−76.5%** | **−24.6%** | −0.03% | +6.7% | −0.9% |
| Fractional | **−49.5%** | +5.6% | −2.7% | −0.9% | −9.4% |

Full wall remains near the same display-paced band. In discontinuous scrolling,
removing synchronous layout makes more of the fixed animation-clock interval
visible as `frame wait`; the complete wall mean still improves. This is why frame
wait is retained as a guardrail but never treated as grid work.

## Stopping decision and residual work

The architecture has met the 50% Active-work target without a row-scene cache.
Adding one now would require new ownership for selection/current-cell overlays,
frozen clips, theme changes, text-render data, row entry/exit, value notifications,
and render-thread disposal. That complexity is not justified by the current target.

Remaining opportunities are intentionally narrower:

- reduce fractional virtual allocation without regressing overlap reuse;
- keep measuring supported column configurations as their surface renderers evolve;
- use platform GPU/compositor captures only if a product workload misses its frame
  budget despite the current CPU/scene reduction; and
- preserve retained fallback rather than approximating unsupported semantics.

## Representative command

```sh
GRID_BENCH_PRO_MODE=virtual \
DOTNET_ROLL_FORWARD=Major \
DOTNET_TieredCompilation=0 \
dotnet tests/ProDataGrid.Hierarchy.NativeBenchmarks/Native.Pro/bin/Release/net8.0/Native.Pro.dll \
  --scroll-only --scroll-pattern line --scroll-jumps 32 \
  --warmup 3 --iterations 10 \
  --avalonia-diagnostics --prodatagrid-diagnostics \
  --output artifacts/performance/current-endpoint-2026-08-13/virtual.json
```

The retained command changes only `GRID_BENCH_PRO_MODE=standard`. The clean lane
removes both diagnostic switches and uses 15 measured iterations.
