# Scroll architecture benchmark results — 2026-08-12

This report compares the virtual, flat, and nested ProDataGrid scroll paths with
Wieslaw's TreeDataGrid source implementation. It also records the matched A/B
result for the latest virtual text-layout-cache optimization.

## Environment and method

- macOS 26.6, Apple M3 Pro Arm64, .NET 10.0.5, Avalonia 12.1, Skia.
- Native window: 800 × 500, render scale 2, fixed 24-pixel rows, five matched
  hierarchy columns, and the same expanded 4,094-row tree.
- Each all-mode diagnostic process used two warmups and five measured iterations
  of 32 deterministic equal-distance scroll jumps (160 measured jumps per mode).
- The cache A/B used three interleaved process pairs. Each process used two
  warmups and five measured iterations (480 jumps per variant), with order
  reversed in the middle pair.
- `DOTNET_TieredCompilation=0` was used and the net8.0 applications ran on the
  installed .NET 10 runtime with major-version roll-forward.
- Allocation is managed allocation traffic per jump, not retained heap, native
  allocation, RSS, or GPU memory.

The all-mode table is a diagnostic ownership sweep, not a statistical gate: it
contains one independent process per mode. The cache result is the stronger
matched, interleaved comparison used to accept the source change.

## What counts as active work

The benchmark's complete `frame wait` crosses two Avalonia animation callbacks.
Most of that interval is refresh-clock pacing. It is not CPU time spent by the
grid and cannot be reduced by row recycling or layout code.

For optimization attribution this report uses **measured active work**:

```text
mutation + explicit layout + UI render recording
         + compositor update + compositor render
```

The compositor values are the maximum instrumented pass durations associated
with each sample. Their sum is an attribution score rather than process CPU
time, because UI and render-thread work can overlap. Full wall time and frame
wait remain visible as scheduling diagnostics.

## Corrected all-mode ownership sweep

These results are from clean commit `077156c4`, before the cache-capacity change.

| Mode | Active work | Mutation | Layout | UI render | Comp. update | Comp. render | Allocated | Reduction vs Tree |
|---|---:|---:|---:|---:|---:|---:|---:|---:|
| Virtual surface | **2.752 ms** | 0.145 ms | **0.455 ms** | 1.361 ms | **0.029 ms** | 0.764 ms | **241.4 KB** | **67.1%** |
| Flat direct-cell | 3.554 ms | 0.141 ms | 2.350 ms | 0.265 ms | 0.073 ms | 0.725 ms | 461.0 KB | 57.5% |
| Flat drawn | 3.699 ms | 0.151 ms | 1.411 ms | 1.363 ms | 0.055 ms | 0.721 ms | 475.1 KB | 55.7% |
| Nested direct | 3.751 ms | 0.153 ms | 2.485 ms | 0.313 ms | 0.078 ms | 0.722 ms | 442.3 KB | 55.1% |
| Nested direct-cell | 3.900 ms | 0.162 ms | 2.651 ms | 0.277 ms | 0.075 ms | 0.735 ms | 447.7 KB | 53.3% |
| Nested drawn | 4.250 ms | 0.167 ms | 1.679 ms | 1.605 ms | 0.056 ms | 0.743 ms | 561.8 KB | 49.2% |
| Optimized retained | 5.295 ms | 0.168 ms | 3.812 ms | 0.399 ms | 0.113 ms | 0.804 ms | 542.5 KB | 36.7% |
| Standard retained | 6.088 ms | **0.106 ms** | 4.305 ms | 0.424 ms | 0.370 ms | 0.883 ms | 579.7 KB | 27.2% |
| Wieslaw TreeDataGrid | 8.360 ms | 0.166 ms | 6.565 ms | 0.687 ms | 0.130 ms | 0.812 ms | 787.7 KB | — |

Virtual had the least active work and allocation. The flat and direct modes also
removed more than half of TreeDataGrid's measured active work. Standard and
feature-preserving optimized retained modes intentionally keep their nested
presenters, bindings, and template semantics; they are compatibility baselines,
not aliases for the new flat architecture.

### Nested drawn shared-cache follow-up

Ordinary text columns in explicit `Drawn` mode previously kept one formatted-text
cache per recycled cell. They now share the existing bounded cache per column.
The retained editor, automation peer, cell type, and drawing semantics are
unchanged. Flat drawn was used as a control because its rows presenter already
shares layouts.

| Mode | Metric | Per-cell baseline | Shared candidate | Change |
|---|---|---:|---:|---:|
| Nested drawn | Active work | 3.507 ms | **3.198 ms** | **−8.8%** |
| Nested drawn | UI render | 1.232 ms | **0.809 ms** | **−34.4%** |
| Nested drawn | Allocation | 561.7 KB | **432.9 KB** | **−22.9%** |
| Nested drawn | End-to-end | 7.729 ms | **7.486 ms** | −3.1% |
| Flat drawn control | Active work | **3.180 ms** | 3.256 ms | +2.4% |
| Flat drawn control | Allocation | **475.0 KB** | 475.0 KB | +0.01% |

The result aggregates three interleaved process pairs and 480 jumps per variant.
Relative to the 8.360 ms TreeDataGrid ownership reference, the optimized nested
drawn score is 61.7% lower, so all flat/direct/drawn/virtual architecture modes
now clear the 50% active-work target. The standard and feature-preserving nested
binding modes remain explicitly reported compatibility baselines. A separate
10-iteration stress run measured 211.0 MB maximum RSS for the per-cell baseline
and 212.2 MB for the per-column cache (+1.2 MB / 0.6%); peak footprint was
slightly lower for the candidate (308.1 MB versus 309.4 MB).

## Latest virtual cache A/B

The virtual renderer already cached shaped `TextLayout` instances, but its
1,024-entry bound was smaller than the discontinuous-scroll working set. The LRU
therefore evicted layouts that later jumps needed again. The candidate raises
only the virtual-surface cache to 4,096 entries; retained and drawn-cell cache
defaults are unchanged.

| Metric | 1,024-entry baseline | 4,096-entry candidate | Change |
|---|---:|---:|---:|
| UI render recording | 0.952 ms | **0.480 ms** | **−49.5%** |
| Managed allocation | 241.3 KB | **99.5 KB** | **−58.8%** |
| Measured active work | 2.007 ms | **1.511 ms** | **−24.7%** |
| Mutation | **0.106 ms** | 0.113 ms | +6.3% |
| Explicit layout | **0.323 ms** | 0.345 ms | +6.6% |
| Compositor update | **0.023 ms** | 0.030 ms | +31.4% |
| Compositor render | 0.603 ms | **0.543 ms** | −10.0% |
| End-to-end wall time | **7.162 ms** | 7.486 ms | +4.5% |
| Full frame wait | **6.732 ms** | 7.028 ms | +4.4% |

The small mutation/layout changes and compositor-update percentage are below
0.01 ms in absolute terms and vary between process pairs. The repeatable signal
is the approximately halved UI-render duration and 58.8% allocation reduction.
Using the ownership-sweep TreeDataGrid reference, the optimized virtual result's
1.511 ms active-work score is 81.9% lower.

A separate 10-iteration memory run measured 212.8 MB maximum RSS and 315.3 MB
peak footprint for the 1,024-entry baseline, versus 218.4 MB maximum RSS and
316.6 MB peak footprint for 4,096 entries. The accepted bound therefore cost
about 5.6 MB maximum RSS in this stress workload. An 8,192-entry trial did not
improve UI render or allocation and was rejected.

## Virtual row recycling and generation

The preceding `077156c4` change optimizes the other half of the requested path:

- exact built-in virtual rows retain their previous non-null `DataContext` until
  the recycled row is rebound, avoiding the item → null → item binding cascade;
- fixed-height virtual rows preserve a valid recycled measure instead of forcing
  an insertion-time measure invalidation;
- custom/derived rows and retained compatibility fallbacks keep the conservative
  lifecycle path;
- a headless regression test verifies that a large virtual jump reuses the same
  row objects without a null `DataContext` transition.

In its matched campaign that row-recycling change reduced synchronous scroll
work by 9.6%, explicit layout by 11.3%, measured active work by 8.2%, UI render
recording by 12.6%, and allocation by 8.2%. The cache change is additive.

### In-place virtual row retargeting

Component diagnostics then split every discontinuous 20-row jump into row acquire,
bind, prepare, cleanup, detach, pool, and insertion phases. The trace showed that
the fixed-height surface still performed 20 complete recycle/generate cycles even
though the same 20 zero-cell row controls were needed after the jump.

The new guarded path retargets that displayed window in place. It is limited to
exact built-in grids/rows with the default realization factory, no row headers,
row numbers, details template, lifecycle handlers, focused/editor rows, or group
slots. Any failed guard uses the full existing lifecycle pipeline. Focused tests
verify both the handler-free retarget and routed `LoadingRow`/`UnloadingRow`
fallback, including selection, validation, item, and zero-cell state.

The clean comparison used three interleaved process pairs, three warmups and ten
measured iterations of 32 jumps: 960 jumps per variant. Baseline was clean commit
`76bb13df`; the candidate included the retarget path. Avalonia diagnostics were
enabled for active-work attribution, while ProDataGrid's finer component listener
was confined to a separate diagnostic run.

| Metric | Recycle/generate baseline | In-place retarget | Change |
|---|---:|---:|---:|
| Explicit layout | 0.317 ms | **0.193 ms** | **−39.0%** |
| Mutation | 0.096 ms | **0.096 ms** | −0.02% |
| UI render recording | 0.470 ms | **0.469 ms** | −0.2% |
| Compositor update | **0.018 ms** | 0.021 ms | +16.5% |
| Compositor render | **0.453 ms** | 0.464 ms | +2.4% |
| Measured active work | 1.353 ms | **1.242 ms** | **−8.2%** |
| Managed allocation | 95.6 KB | **84.9 KB** | **−11.2%** |
| End-to-end wall time | 7.369 ms | **7.331 ms** | −0.5% |
| Full frame wait | **6.957 ms** | 7.042 ms | +1.2% |

The component run reports 20 `prodatagrid.rows.retargeted.count` events per jump
and no row generation or recycle-phase duration. Wall time is unchanged because
the roughly 7 ms two-callback frame wait remains refresh pacing; the repeatable
signal is the 39% layout reduction and lower active work/allocation. Raw results
are under `artifacts/performance/virtual-row-path-2026-08-12`, with the clean six
processes named `fresh-{baseline,final}-{a,b,c}.json` in `paired-clean/`.

### Retarget state and target-lookup follow-up

After row retargeting removed generation/recycling, a component probe attributed
the residual displayed-row update to three phases: approximately 0.056 ms in the
20-row bind/state batch, 0.015 ms in row validation, and 0.011 ms in eligibility
and target-range calculation. The follow-up therefore keeps lifecycle guards
unchanged and optimizes only the already-eligible path:

- pointer and drag/drop cleanup is conditional on tracked transient state;
- row selection/current/validation pseudo-classes are changed only when the old
  and new slot states differ; and
- validation captures target row indices/items in reusable arrays so the bind
  pass does not repeat hierarchical lookup and slot mapping.

The publication run compared clean `2cae9137` with the candidate in three process
pairs ordered `B A`, `A B`, `B A`. Each process used three warmups and ten measured
iterations of 32 discontinuous jumps, for 960 jumps per variant. Avalonia render
diagnostics were enabled to calculate active work; ProDataGrid component meters
were disabled for this gate and used only in the separate attribution probe.

| Metric | Initial retarget path | Sparse retarget state | Change |
|---|---:|---:|---:|
| Mutation | 0.103 ms | **0.099 ms** | **−3.9%** |
| Explicit layout | 0.200 ms | **0.182 ms** | **−9.0%** |
| UI render recording | 0.442 ms | **0.420 ms** | **−5.1%** |
| Compositor update | 0.020 ms | **0.020 ms** | **−4.3%** |
| Compositor render | 0.475 ms | **0.459 ms** | **−3.4%** |
| Measured active work | 1.240 ms | **1.179 ms** | **−5.0%** |
| Managed allocation | **84.916 KB** | 84.961 KB | +0.05% |
| End-to-end wall time | 6.568 ms | **6.528 ms** | −0.6% |
| Full frame wait | 6.266 ms | **6.247 ms** | −0.3% |

The allocation movement is 45 bytes per jump and is treated as noise. The active
components all move in the expected direction, while wall time remains dominated
by the excluded callback wait. Raw balanced results are
`artifacts/performance/virtual-next-2026-08-12/final-{baseline,candidate}-{a,b,c}.json`.

### Retarget layout reuse follow-up

With row generation and recycling already removed, the next component profile
showed that `ScrollSlotsByHeight` still led the presenter through duplicated row
measure and arrange loops. The follow-up batches logical child-index notification,
resolves selection state once, and reuses validated fixed-height row geometry.
Measure reuse requires rows to remain valid after selector notification and an
unchanged width. Arrange reuse additionally requires the same presenter size and
fractional vertical offset; changed fractional offsets use normal arrangement.

The publication run compared clean `bb2bd4c4` with the candidate in three process
pairs ordered `B A`, `A B`, `B A`. Each process used three warmups and ten measured
iterations of 32 discontinuous jumps, for 960 jumps per variant. Avalonia render
diagnostics were enabled to calculate active work; ProDataGrid component meters
were disabled for the gate and enabled only in a separate attribution run.

| Metric | Sparse retarget baseline | Layout-reuse candidate | Change |
|---|---:|---:|---:|
| Mutation | **0.113 ms** | 0.118 ms | +4.6% |
| Explicit layout | 0.207 ms | **0.158 ms** | **−23.8%** |
| Mutation + layout | 0.320 ms | **0.276 ms** | **−13.8%** |
| UI render recording | **0.521 ms** | 0.546 ms | +4.7% |
| Compositor update | **0.022 ms** | 0.024 ms | +7.2% |
| Compositor render | 0.529 ms | **0.522 ms** | −1.4% |
| Measured active work | 1.393 ms | **1.367 ms** | −1.8% |
| Managed allocation | 85.0 KB | **84.6 KB** | −0.5% |
| End-to-end wall time | 7.325 ms | **7.315 ms** | −0.1% |
| Full frame wait | **7.005 ms** | 7.039 ms | +0.5% |

The clean result isolates the expected gain: explicit layout falls by 23.8% and
synchronous mutation plus layout falls by 13.8%. Total active work falls only 1.8%
because UI/compositor rendering is unchanged by row-container geometry reuse and
dominates this workload. The separate component run reports 20 retargeted rows,
20 reused measures, and 20 reused arrangements on every one of 320 measured integer
jumps, with zero generated/recycled rows, zero realized cells, and 102 visuals.
Focused headless coverage also proves that changing the fractional row offset
disables arrangement reuse and moves row bounds correctly.

Raw gate results are
`artifacts/performance/virtual-next-2026-08-12/layout-reuse-final-{baseline,candidate}-{a,b,c}.json`;
the instrumented proof is `layout-reuse-diagnostic.json` in the same directory.

## Why full frame wait does not fall with active work

The Windows CI artifact for `077156c4` reports only about 0.006–0.009 ms from
layout completion to callback 1 across ProDataGrid modes. For virtual surface,
the callback-1-to-callback-2 interval accounts for virtually the complete frame
wait and matches the animation-clock interval. That is idle refresh pacing.

Consequently, row/cell optimization should be judged primarily on mutation,
layout, UI render, compositor work, and allocation. Wall time remains useful for
detecting a frame-band regression, but a 50% reduction in the deliberately
awaited callback interval would require changing the completion convention or
display/render-loop frequency rather than optimizing `ScrollSlotsByHeight`.

## Typed checkbox surface follow-up

A typed `DataGridCheckBoxColumn` previously made the requested virtual mode fall
back to flat retained cells for the entire grid. The new renderer reads the
column's typed accessor, draws centered two- and three-state indicators on the
surface, and materializes the normal retained `CheckBox` only while editing.
Derived columns and bindings without a compatible accessor remain on retained
fallback.

The matched A/B used the same five-column native hierarchy workload, replacing
only the payload text column with `HasChildren`. Separate clean-timing and
instrumented-attribution campaigns each used three interleaved process pairs,
two warmups, and five measured iterations of 32 jumps (480 jumps per variant per
campaign). The baseline production source was clean `63e5e50e`; the identical
benchmark-only lane was applied to its isolated worktree and retained fallback was
explicitly allowed by the harness. The candidate required zero retained display
cells.

| Metric | Retained fallback | Checkbox surface | Change |
|---|---:|---:|---:|
| Active-work attribution (diagnostic) | 14.909 ms | **2.420 ms** | **−83.8%** |
| Explicit layout (clean) | 9.541 ms | **0.257 ms** | **−97.3%** |
| UI render recording (diagnostic) | 0.974 ms | **0.694 ms** | **−28.7%** |
| Compositor update (diagnostic) | 0.236 ms | **0.030 ms** | **−87.1%** |
| Compositor render (diagnostic) | **0.675 ms** | 0.921 ms | +36.5% |
| Managed allocation (clean) | 3,382.6 KB | **90.5 KB** | **−97.3%** |
| End-to-end mean (clean) | 16.442 ms | **8.227 ms** | **−50.0%** |
| Mean process median (clean) | 16.484 ms | **8.323 ms** | **−49.5%** |
| Mean process p95 (clean) | 24.663 ms | **9.423 ms** | **−61.8%** |
| Full frame wait (clean) | **6.833 ms** | 7.893 ms | +15.5% |
| Realized display cells | 100 | **0** | **−100%** |
| Realized visuals | 1,841 | **102** | **−94.5%** |

The diagnostic compositor-render increase is 0.246 ms and is outweighed by the
clean run removing 9.283 ms of layout plus 3.29 MB of allocation per jump. The
candidate's 7.893 ms frame wait contains a 7.806 ms callback interval and only
0.007 ms of pickup, so its increase is animation-clock phase rather than grid
execution. Active work, clean end-to-end latency, tails, allocation, and
structure all move in the expected direction. Raw JSON is under
`artifacts/performance/virtual-checkbox-2026-08-12`; clean timing is in `clean/`
and the instrumented campaign is in `{baseline,candidate}`.

## Reproduction and raw data

The harness modes and command lines are documented in the
[native benchmark README](../ProDataGrid.Hierarchy.NativeBenchmarks/README.md).
The cross-suite measurement and acceptance rules are documented in
[the layout benchmark methodology](../../docfx/articles/layout-performance-benchmarking.md).
Local raw JSON and traces are under
`artifacts/performance/active-all-modes-2026-08-12`; the artifact directory is
gitignored. The cache comparison is in
`interleaved-virtual-cache-4096-final/{baseline,candidate}`.
