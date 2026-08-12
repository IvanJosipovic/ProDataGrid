# Scroll architecture benchmark results — 2026-08-12

This report measures the final virtual-cell renderer optimizations and compares
the virtual, flat, and nested ProDataGrid architectures with Wieslaw's
TreeDataGrid source implementation. Results use the native benchmark harness's
`--scroll-only` path.

## Environment and method

- macOS 26.6, Apple M3 Pro Arm64, .NET 10.0.5, Avalonia 12.1, Skia.
- Native window: 800 × 500, render scale 2, fixed 24-pixel rows, and five matched
  hierarchy columns.
- Each architecture result is the average of four independent process means.
  Every process used two warmups and five measured iterations of 32 deterministic
  scroll jumps, for 640 measured jumps per architecture. Complete mode order was
  reversed in alternating process pairs.
- P95 is the average of the four per-process P95 values. Allocation is managed
  allocation traffic per jump, not retained heap, native allocation, RSS, or GPU
  memory.
- Total latency crosses the harness's two-callback Avalonia frame barrier.
  Mutation, explicit layout, and frame wait are reported separately because a
  process can move into an adjacent display-frame band.

## Final architecture comparison

| Architecture | Mean | Mean process P95 | Allocated | Mutation | Layout | Frame wait |
|---|---:|---:|---:|---:|---:|---:|
| Pro virtual cell surface | **8.252 ms** | **8.738 ms** | **94.3 KB** | 0.118 ms | **1.366 ms** | 6.768 ms |
| Pro flat direct-cell retained | 8.524 ms | 9.814 ms | 488.7 KB | 0.102 ms | 4.594 ms | **3.828 ms** |
| Pro flat drawn | 8.547 ms | 9.251 ms | 453.1 KB | 0.104 ms | 2.914 ms | 5.529 ms |
| Pro nested direct-cell retained | 8.870 ms | 11.591 ms | 459.2 KB | 0.109 ms | 5.033 ms | 3.728 ms |
| Pro nested drawn | 8.985 ms | 15.272 ms | 571.3 KB | 0.110 ms | 2.824 ms | 6.052 ms |
| Wieslaw TreeDataGrid | 9.625 ms | 15.987 ms | 784.5 KB | **0.072 ms** | 5.186 ms | 4.367 ms |

Every ProDataGrid mode beat TreeDataGrid's end-to-end scroll mean and P95. The
virtual surface was 14.27% faster on the mean, 45.34% lower on P95, and allocated
87.98% less than TreeDataGrid. It allocated 79.46% less than the nested
direct-cell retained path.

Centralized flat drawing improved on nested drawn cells by 4.87% in total time
and 20.71% in managed allocation. Flat retained improved on nested retained by
3.90% in total time and 8.73% in explicit layout, but allocated 6.44% more. The
retained flat path therefore does not support an allocation-win claim. Against
TreeDataGrid, flat retained was 11.44% faster on the mean and 38.61% lower on
P95; flat drawn was 11.20% faster on the mean and 42.13% lower on P95.

## Virtual renderer optimization

The final renderer caches `TextLayout` objects rather than `FormattedText`.
`FormattedText` caches metrics but enumerates and formats its lines again on each
draw. `TextLayout` retains its shaped text lines, removing that repeated shaping
and glyph construction from every scroll render. Its bounded LRU cache disposes
layouts on eviction and detach.

The renderer also avoids a capturing cache-factory delegate, skips redundant
full-cell clip scopes, and does not build notifier-tracking state when all
compatible columns explicitly opt out of value-change tracking.

## Why end-to-end does not show a 71% reduction

The native total intentionally waits through a two-animation-callback completion
barrier. On this display it has an approximately 8 ms scheduling floor even if
the grid work approaches zero. The total is useful user-visible latency, but it
cannot attribute CPU work below that floor.

A separate four-process diagnostic comparison enables Avalonia's meters. The
instrumented result is attribution evidence rather than the clean performance
gate because meter collection adds overhead. “Measured active work” is the sum
of mutation, explicit layout, UI render recording, compositor update, and
compositor render durations; it excludes the idle frame wait.

| Diagnostic metric | Pro virtual | TreeDataGrid | Reduction |
|---|---:|---:|---:|
| Mutation + explicit layout | 0.780 ms | 4.091 ms | **80.93%** |
| UI render recording | 0.369 ms | 0.483 ms | **23.60%** |
| Compositor update | 0.0358 ms | 0.1192 ms | **69.95%** |
| Compositor render | 0.400 ms | 0.703 ms | **43.14%** |
| Measured active work | **1.557 ms** | 5.397 ms | **71.15%** |
| Managed allocation | **94.9 KB** | 785.4 KB | **87.92%** |
| End-to-end including frame wait | 7.019 ms | 9.549 ms | 26.50% |

The `TextLayout` change itself reduced virtual UI render recording from 2.307
to 0.369 ms (84.0%), measured active work from 3.743 to 1.557 ms (58.4%), and
managed allocation from 456.7 to 94.9 KB (79.2%) in matched four-process
diagnostic campaigns.

## Frame-wait ownership follow-up

A second four-process campaign added callback-phase telemetry to every scroll
jump for all eight ProDataGrid modes. Each row aggregates 640 measured jumps.
The existing `Frame wait` value spans two callback phases; the new columns split
layout-to-callback-1 frame pickup from the callback-1-to-callback-2 interval.

| Mode | Mutation + layout | Frame wait | Frame pickup | Callback interval | Animation tick |
|---|---:|---:|---:|---:|---:|
| Virtual surface | **1.383 ms** | 6.857 ms | **0.014 ms** | 6.771 ms | 6.770 ms |
| Flat drawn | 2.547 ms | 8.852 ms | 2.005 ms | 6.750 ms | 6.749 ms |
| Flat direct-cell | 4.057 ms | 7.517 ms | 1.483 ms | 5.953 ms | 5.953 ms |
| Nested drawn | 2.487 ms | 9.145 ms | 2.044 ms | 7.026 ms | 7.026 ms |
| Nested direct-cell | 4.145 ms | 7.336 ms | 1.397 ms | 5.854 ms | 5.853 ms |
| Nested direct-content | 4.154 ms | 7.360 ms | 1.410 ms | 5.868 ms | 5.868 ms |
| Optimized retained | 4.716 ms | 7.434 ms | 1.187 ms | 6.167 ms | 6.166 ms |
| Standard retained | 5.780 ms | 6.938 ms | 1.056 ms | 5.780 ms | 5.780 ms |

For virtual surface, callback interval and animation tick differ by only
0.00037 ms. That interval is 98.7% of its reported frame wait and is refresh
pacing, not DataGrid execution. Virtual reduces the DataGrid-owned frame-pickup
portion by 98.7% versus the next-lowest retained mode and by 99.3% versus flat
drawn. Reducing the complete 6.857 ms value by 50% while retaining the same
two-animation-callback convention would require a higher display/render-loop
frequency; row/cell layout cannot remove the deliberately awaited callback
interval.

The 71% work reduction is the expected architectural benefit. The smaller 14%
clean end-to-end reduction is the same work observed through a frame-quantized
completion convention, not evidence that virtual rendering still performs the
same amount of work as TreeDataGrid.

## Reproduction and raw data

The harness modes and command lines are documented in the
[native benchmark README](../ProDataGrid.Hierarchy.NativeBenchmarks/README.md).
Local raw JSON, traces, and published baseline/candidate apps are under
`artifacts/performance/scroll-2026-08-12`; the artifact directory is gitignored.
