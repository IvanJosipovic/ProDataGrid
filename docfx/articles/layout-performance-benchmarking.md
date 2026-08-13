# Layout performance benchmark methodology

ProDataGrid's layout work spans model mutation, row realization, Avalonia layout,
UI render recording, the compositor, and display-clock pacing. A single wall-clock
number cannot identify which owner changed. This methodology defines the benchmark
lanes, phase boundaries, structural checks, and acceptance rules used for the flat
and virtual architectures.

## Questions the benchmark matrix answers

The repository uses several complementary experiments:

| Lane | Question | Primary evidence |
|---|---|---|
| Model BenchmarkDotNet | Did hierarchy mutation itself change? | CPU time and managed allocation |
| Headless component/layout | Did presenter layout and realization change? | Pending layout time, allocation, structure |
| Native application macrobenchmark | Did the complete interactive path improve? | Mutation, layout, render attribution, wall time, tails |
| Source comparison | How do equivalent ProDataGrid and TreeDataGrid workloads compare? | Independent native processes and structural validation |
| Matched A/B | Did one candidate change improve its intended owner? | Interleaved baseline/candidate process pairs |
| Memory stress | Did caching/pooling change process footprint? | Managed heap plus RSS/private/peak observations |
| Trace/capture | Which method, runtime, compositor, or scheduler owns a cost? | Managed/native stacks and Avalonia diagnostics |

No lane substitutes for another. A model benchmark cannot prove smooth scrolling,
and an application benchmark alone may not identify the responsible code.

## Reference workload

The native hierarchy scroll workload uses:

- an 800 × 500 native window at 2× render scale;
- fixed 24-pixel rows;
- five matched explicit-width columns;
- an expanded 4,094-row binary hierarchy;
- 32 deterministic equal-distance offsets per iteration; and
- a fully rendered and aligned state before each measured sample.

The larger collapse workload expands 149,792 nodes before setup and collapses to 32
roots. BenchmarkDotNet and sample-specific suites document their own viewport and
input differences in their READMEs and reports.

## Phase model

Every native scroll sample records:

```text
total wall time = mutation + explicit layout + frame wait
```

- **mutation** changes the scroll offset and performs synchronous DataGrid work.
- **explicit layout** is the measured `UpdateLayout()` boundary.
- **frame wait** crosses the harness's two animation callbacks.

A separate diagnostic process enables Avalonia meters and records:

- UI render-recording duration;
- compositor update duration; and
- compositor render duration.

The diagnostic process is not used as a clean timing gate because meter collection
adds overhead.

## Active-work attribution

Optimization reports use this attribution score:

```text
active work = mutation + explicit layout + UI render recording
            + compositor update + compositor render
```

This excludes the deliberately awaited idle animation interval. It is not literal
single-thread CPU time: UI and render-thread work can overlap, and compositor values
are maximum instrumented pass durations associated with a sample. It is useful for
ownership comparisons, not for billing CPU milliseconds.

Full wall time and full frame wait remain reported. They detect a frame-band or
scheduling regression even when they are not attributed to DataGrid execution.

## Why frame wait is not treated as grid work

`RequestAnimationFrame` schedules work on Avalonia's animation clock. The harness
registers callback 1 before synchronous mutation. Mutation and layout occupy the UI
thread, so callback 1 cannot run until they yield. Callback 1 schedules callback 2,
which must run on a later pulse.

The interval can therefore include render recording, composition submission,
render-thread processing, scheduler delay, and refresh pacing. It is not a GPU
presentation fence. The JSON splits it into:

- layout completion to callback 1 (pickup);
- callback 1 to callback 2; and
- measured animation-tick interval.

When callback 1→2 matches the animation interval and pickup is only microseconds,
the dominant portion is clock pacing. Row recycling or layout optimization cannot
halve that idle interval. Reports must say so instead of relabeling it as CPU work.

## Pre-sample alignment

Before each sample the harness:

1. completes a full managed collection outside the timed operation;
2. waits through a three-callback alignment barrier; and
3. arms the timed two-callback barrier before mutation.

Three alignment callbacks are required because the second alignment callback runs
before the remainder of Avalonia's media pass. Starting after only two can leave an
unrelated composition batch pending and move the next sample into another refresh
band.

## Structural validation

Timing is accepted only after semantic and structural checks. The native harness
verifies:

- the expected row count;
- a bounded realized-row count;
- the realized cell, visual, and control counts;
- extent and viewport geometry; and
- zero retained display cells for a surface lane unless a baseline experiment
  explicitly permits retained fallback.

Feature-specific tests additionally verify editing, selection, hit testing, value
invalidation, recycling identity, fallback transitions, and cleanup. A faster run
that omitted required cells or failed to activate the requested backend is invalid.

## Clean comparison protocol

For a source-mode comparison, CI runs each mode in an independent process. It uses
two warmups and ten measured iterations, reverses complete mode order in alternating
processes, aggregates four process means, and reports a Student-t 95% confidence
interval. Raw JSON and `aggregate.json` are uploaded as an artifact.

For a focused candidate A/B:

1. create clean baseline and candidate worktrees or published binaries;
2. record both commit IDs and dirty state;
3. build both in `Release` before timing;
4. keep runtime, architecture, window, dataset, and environment identical;
5. run at least three process pairs with alternating order (`B A`, `A B`, `B A`);
6. warm both variants before measurement;
7. retain every raw sample; and
8. compare process-level means, tails, allocation, structure, and regressions.

Do not choose the fastest process. Exclude a run only by a rule declared before
examining the result.

## Runtime and deployment controls

The benchmark report records:

- source commit and dirty state;
- build configuration and target framework;
- SDK and runtime versions;
- OS, architecture, hardware, and logical CPU count;
- Avalonia version, renderer, window dimensions, and render scale;
- warmup, iteration, jump, and alignment counts; and
- environment overrides such as `DOTNET_TieredCompilation=0` or runtime roll-forward.

Short native comparisons disable tiered compilation so a process does not change
generated-code tier midway through the small workload. If the shipping tiering/PGO
behavior is the subject, run it as a separate deployment experiment.

## Allocation and memory interpretation

Native JSON allocation is `GC.GetTotalAllocatedBytes` traffic during the sample.
BenchmarkDotNet's memory diagnoser also reports managed allocation traffic. Neither
proves retained heap size or measures native, Skia, compositor, or GPU allocation.

Cache and pooling changes require a separate stress run at equivalent lifecycle
points. Compare at least:

- managed live/committed heap;
- total allocated bytes;
- process RSS/working set and private or peak footprint where available;
- cache count/capacity and eviction behavior; and
- state after detach or window close.

RSS alone is never labeled a managed leak.

## Acceptance rules

A performance change is accepted when:

1. profiler or phase evidence identifies the intended owner;
2. baseline and candidate preserve behavior and structure;
3. multiple paired processes show a practically meaningful improvement;
4. allocation, tails, startup, and memory lifetime have no unexplained regression;
5. the application-level workload confirms the focused result; and
6. raw artifacts, exact commands, limitations, and residual risks are preserved.

Small sub-0.01 ms movements and shared-runner percentage changes are reported as
noise unless repeated evidence proves otherwise. A whole-frame wall-time movement
is not attributed to layout without pickup/callback evidence.

## Commands and artifacts

Build the native ProDataGrid application:

```bash
dotnet build \
  tests/ProDataGrid.Hierarchy.NativeBenchmarks/Native.Pro/Native.Pro.csproj \
  -c Release
```

Run a focused virtual scroll process:

```bash
DOTNET_TieredCompilation=0 \
GRID_BENCH_PRO_MODE=virtual \
dotnet \
  tests/ProDataGrid.Hierarchy.NativeBenchmarks/Native.Pro/bin/Release/net8.0/Native.Pro.dll \
  --scroll-only --scroll-jumps 32 --warmup 2 --iterations 10 \
  --output artifacts/performance/virtual-scroll.json
```

Add `--avalonia-diagnostics` only for the separate active-work attribution run.
Add `--prodatagrid-diagnostics` to a ProDataGrid-only attribution run when the
virtual layout pipeline needs finer ownership: the JSON records per-jump means and
raw samples for scrolling, displayed-row update, generation phases, recycling
phases, retarget eligibility/validation/bind phases, element insertion, and row
realization/recycling/retargeting counts. Retarget probes also expose
`prodatagrid.rows.retarget.apply.time`,
`prodatagrid.rows.retarget.child-index.time`, and
`prodatagrid.rows.retarget.layout-validity.time` inside the enclosing bind phase.
Use these nested phases to distinguish observable row identity/state work from
logical-tree bookkeeping and the layout-reuse guard; do not add nested phase
values to their enclosing bind time. Retarget probes also expose
`prodatagrid.rows.retarget.measure.reused.count` and
`prodatagrid.rows.retarget.arrange.reused.count`; compare them with the realized
row count to prove whether the guarded geometry-reuse path actually ran. Both
diagnostic switches add measurement overhead and remain outside the clean A/B gate.
For the rowless surface, the same switch records
`prodatagrid.virtual.surface.render.time` plus aggregate rendered-row, rendered-cell,
clip, vertical-grid-line, hierarchy-expander, text-layout cache hit/miss, text scene
operation, and immutable glyph-run counts.
Use those counters to prove which drawing path ran. In particular, a workload with
zero vertical-grid-line operations cannot validate a grid-line batching hypothesis,
and a warm-cache workload must not attribute render time to text shaping without
cache-miss evidence.
Text batching must be evaluated with both UI render recording and compositor
render. A UI-stage reduction is accepted only when the render-stage aggregate does
not show an equivalent transfer of work to the compositor. Scene-operation and
glyph-run counters prove that the candidate path was active.
When a transactional path reports additive counters for every row, batch the
counter additions only after the transaction succeeds and prove that the aggregate
values are unchanged. This reduces diagnostic observer cost without moving phase
boundaries or weakening lifecycle accounting.
For fixed-height virtual-scroll work, compare
`prodatagrid.rows.scroll.slots.by.height.time` with mutation plus layout and the
complete active-work score. A faster target lookup is accepted only when the
application-level active components also improve; full frame wait remains a
separate refresh-pacing diagnostic.
Use `virtual-checkbox` to exercise the mixed text/hierarchy/checkbox surface lane.
Use `virtual-autocomplete` to compare retained autocomplete display text with its
typed zero-cell surface path; suggestion/filter behavior remains outside the
read-only scrolling workload and is covered by editing tests.
Use `virtual-slider-text` to compare retained centered slider value text with its
typed zero-cell surface path; the graphical display mode is intentionally outside
the contract and slider interaction is covered by editing tests.
Use `virtual-combobox-text` to compare an editable retained ComboBox `TextBinding`
display with its typed zero-cell text-and-glyph surface path. Selected-item/value
display remains outside the contract; editing and dropdown interaction are covered
by focused tests.
`GRID_BENCH_ALLOW_VIRTUAL_FALLBACK=1` exists only for controlled baseline
experiments and must not be set when validating the candidate surface.

Large generated traces, dumps, and benchmark output stay under the gitignored
`artifacts/performance` tree or CI artifacts. Checked-in reports contain summarized
results and point to the raw artifact location.

See the [native source benchmark README](https://github.com/wieslawsoltes/ProDataGrid/blob/main/tests/ProDataGrid.Hierarchy.NativeBenchmarks/README.md)
and the [focused scroll report](https://github.com/wieslawsoltes/ProDataGrid/blob/main/tests/ProDataGrid.FlatLayout.Benchmarks/SCROLL-RESULTS-2026-08-12.md)
and the [virtual retarget-buffer report](https://github.com/wieslawsoltes/ProDataGrid/blob/main/tests/ProDataGrid.FlatLayout.Benchmarks/VIRTUAL-RETARGET-RESULTS-2026-08-13.md)
and the [virtual row lifecycle batch report](https://github.com/wieslawsoltes/ProDataGrid/blob/main/tests/ProDataGrid.FlatLayout.Benchmarks/VIRTUAL-ROW-BATCH-RESULTS-2026-08-13.md)
and the [virtual row retarget-apply report](https://github.com/wieslawsoltes/ProDataGrid/blob/main/tests/ProDataGrid.FlatLayout.Benchmarks/VIRTUAL-ROW-APPLY-RESULTS-2026-08-13.md)
and the [virtual surface render report](https://github.com/wieslawsoltes/ProDataGrid/blob/main/tests/ProDataGrid.FlatLayout.Benchmarks/VIRTUAL-SURFACE-RENDER-RESULTS-2026-08-13.md)
for current commands and results.
