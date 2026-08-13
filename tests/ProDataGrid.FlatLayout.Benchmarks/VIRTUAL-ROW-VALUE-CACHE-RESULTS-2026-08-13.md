# Virtual row-value cache results — 2026-08-13

## Scope

The rowless virtual lane already performs fixed-height `ScrollSlotsByHeight` as
direct slot projection. In this experiment the native workload reports no row
generation or recycling, while the scroll operation itself is only about
0.02–0.04 ms per jump. The remaining repeated row work was inside surface
recording: each render resolved and formatted all 100 visible cell values even
when line or fractional scrolling preserved most or all visible item identities.

The baseline is commit `0495c8f5`. The candidate adds a presenter-owned,
row-aligned value cache bounded to the lightweight visible-row window. It also
stores text converter, parameter, format, and culture in the existing per-column
render plan, so this metadata is resolved once per column rather than once per
cell.

A cached row is valid only when all of these remain identical:

- projected item reference;
- render-plan version;
- tracked value-change version; and
- visible value count.

Overlapping slots keep their cache entries. Entering rows reuse entries that left
the window and resolve only their own values. Column/style/formatter changes and
tracked item notifications invalidate cached values. Fallback, detach, and
lightweight-row cleanup release item and value references. The cache does not
retain row or cell controls.

## Environment and protocol

- macOS 26.6 (25G72), Arm64;
- Apple M3 Pro, 11 cores, 18 GB;
- .NET SDK 10.0.201; benchmark runtime .NET 10.0.5 running the net8.0 app with
  `DOTNET_ROLL_FORWARD=Major`;
- Avalonia 12.1.0, Skia, 800 × 500 window, render scale 2;
- `DOTNET_TieredCompilation=0`;
- `GRID_BENCH_PRO_MODE=virtual`;
- 4,094 expanded hierarchy rows, five fixed-width columns, 20 rendered rows and
  100 rendered cells per pass;
- 32 deterministic scroll operations per iteration;
- discontinuous, line, and fractional patterns;
- three warmups and ten measured iterations per diagnostic process;
- three alternating process pairs per pattern (`B C`, `C B`, `B C`); and
- separate meter-free processes with three warmups and 15 measured iterations.

Raw JSON is gitignored under
`artifacts/performance/virtual-row-value-cache-2026-08-13/`. Every accepted
process validated 4,094 logical rows, zero retained rows, zero retained cells, 62
visuals, and 62 controls.

## Work attribution

Active work is mutation + explicit layout + Avalonia UI render recording +
compositor update + compositor render. Frame wait is excluded because it contains
the harness animation-frame barrier and refresh pacing. The virtual-surface timer
is nested inside UI render and is not added to Active a second time.

`ScrollSlotsByHeight` remains an attribution metric, not a second additive phase.
Row generation and recycling meters remain zero in this rowless lane.

## Diagnostic results

Values are means of three process means. Changes are medians of the three paired
percentage changes.

| Pattern | Surface baseline | Surface candidate | Median paired change | Active baseline | Active candidate | Median paired change |
|---|---:|---:|---:|---:|---:|---:|
| Discontinuous | 0.39902 ms | 0.39391 ms | −1.7% | 1.26194 ms | 1.31502 ms | −0.8% |
| Line | 0.22764 ms | **0.17489 ms** | **−16.4%** | 0.97388 ms | **0.80074 ms** | **−16.6%** |
| Fractional | 0.11912 ms | **0.10603 ms** | **−12.2%** | 0.83795 ms | 0.86886 ms | +10.8% |

Direct surface pair changes were:

| Pattern | Pair 1 | Pair 2 | Pair 3 |
|---|---:|---:|---:|
| Discontinuous | −1.7% | −11.9% | +14.0% |
| Line | −43.6% | +4.0% | −16.4% |
| Fractional | +20.4% | −12.2% | −35.8% |

The component timings are process-sensitive at these sub-millisecond sizes, so
the result must not be described as a universal frame reduction. The structural
cache counters provide the less ambiguous mechanism check:

| Pattern | Candidate hits / operation | Candidate misses / operation |
|---|---:|---:|
| Discontinuous | 0 | 100 |
| Line | 95 | 5 |
| Fractional | 100 | 2.5 |

Fractional alternates two offsets and occasionally changes the overscan row,
which explains its 2.5 average misses. Baseline binaries do not expose these new
counters.

Diagnostic allocation moved consistently with overlap:

| Pattern | Baseline | Candidate | Median paired change |
|---|---:|---:|---:|
| Discontinuous | 69,923 B | 69,847 B | −0.02% |
| Line | 41,941 B | **38,628 B** | **−7.9%** |
| Fractional | 32,302 B | **28,969 B** | **−10.4%** |

This is the strongest repeatable result: cached formatted strings are not
reallocated for overlapping rows, while the zero-hit workload stays flat.

## Meter-free guardrail

| Pattern | Mutation + layout | Allocation | Full wall | Frame wait | p95 wall |
|---|---:|---:|---:|---:|---:|
| Discontinuous | +19.2% | −0.00% | −0.25% | −0.62% | +4.24% |
| Line | +32.7% | **−8.0%** | −0.39% | −0.91% | −2.46% |
| Fractional | +0.36% | **−11.4%** | −0.32% | −0.33% | +0.34% |

Mutation + layout does not include the optimized surface-recording work and is
only about 0.13–0.18 ms here; its percentage is unstable. Full wall remains in
the same refresh-paced band. The allocation reduction repeats without diagnostic
meters.

## Interpretation and next owner

This change does not deliver a 50% reduction in total Active work. It removes the
value-resolution portion of overlapping surface passes, not drawing, glyph scene
recording, compositor work, or refresh pacing. The conservative paired result is
about 16% lower surface/Active work for line scrolling and a repeatable 8–11%
allocation reduction for overlapping patterns.

The requested row recycling/generation target is structurally exhausted in this
lane: both counters are zero. `ScrollSlotsByHeight` is already direct arithmetic
plus bounded projection and is a small component. A materially larger next step
must reuse or translate row-level draw commands/scene data for overlapping rows,
rather than further tuning nonexistent row-container lifecycle work.

## Correctness

- The full unit suite passes: 2,816 tests, zero failures.
- Tests cover overlap hits, entering-row misses, item-notification invalidation,
  render-plan/style invalidation, fallback cleanup, and zero retained cells.
- Eight native smoke modes validate the supported virtual column configurations;
  all keep 4,094 logical rows with zero retained rows and cells.

## Representative command

```sh
DOTNET_ROLL_FORWARD=Major \
DOTNET_TieredCompilation=0 \
GRID_BENCH_PRO_MODE=virtual \
dotnet tests/ProDataGrid.Hierarchy.NativeBenchmarks/Native.Pro/bin/Release/net8.0/Native.Pro.dll \
  --scroll-only --scroll-pattern line --scroll-jumps 32 \
  --warmup 3 --iterations 10 \
  --avalonia-diagnostics --prodatagrid-diagnostics \
  --output artifacts/performance/virtual-row-value-cache-2026-08-13/line.json
```

The clean lane removes both diagnostic switches and uses 15 measured iterations.
Baseline and candidate were built in separate worktrees and invoked by absolute
assembly paths.
