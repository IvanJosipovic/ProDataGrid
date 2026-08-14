# TreeDataGrid-to-virtual comparison results — 2026-08-13

## Result

The current rowless ProDataGrid virtual architecture materially reduces work
relative to the CI-pinned open-source TreeDataGrid implementation. The fresh
matched Active-work comparison is:

| Scroll pattern | TreeDataGrid Active | ProDataGrid virtual Active | Median paired change |
|---|---:|---:|---:|
| Discontinuous | 4.82794 ms | **1.06059 ms** | **−77.9%** |
| Line | 2.49896 ms | **1.36178 ms** | **−46.4%** |
| Fractional | 1.80954 ms | **0.84238 ms** | **−57.3%** |

The line result is below the 50% threshold when TreeDataGrid is the baseline. It
does not contradict the separate retained-to-virtual endpoint result: that matched
comparison uses ProDataGrid's legacy retained architecture as its baseline and
reports −51.3% for line scrolling. The two questions and baselines are different.

TreeDataGrid retains less line-render-recording work than ProDataGrid's legacy
architecture, while the new surface still removes most layout and compositor
scene cost. The resulting line Active reduction against TreeDataGrid is 46.4%.

## Compared sources

- ProDataGrid commit: `13e6f026dc7c01468c0cc78e26037605e5f1a169`;
- TreeDataGrid commit: `7628bd18e29dad315fd36ead57a1d5339bfaf944`,
  the revision pinned by the native-source CI workflow;
- both controls were built from source and run on the same machine and runtime;
- both used the same generated hierarchy, five data columns, 800 × 500 native
  window, 24-pixel row height, and two-animation-callback completion convention.

## Full native lifecycle suite

Four alternating independent process pairs ran the complete harness: first
expanded render, 4,094-node expand, 149,792-to-32 collapse, and 32 discontinuous
scroll operations. Values are means of the four process means; changes are medians
of the four paired percentage changes.

| Scenario | TreeDataGrid wall | Virtual wall | Median paired change | TreeDataGrid allocation | Virtual allocation | Median paired change |
|---|---:|---:|---:|---:|---:|---:|
| First expanded render | 13.37678 ms | **9.12931 ms** | **−29.4%** | 5,937,432 B | **982,495 B** | **−83.5%** |
| Expand all + render | 13.40250 ms | **5.77921 ms** | **−58.5%** | 6,224,798 B | **2,575,403 B** | **−58.6%** |
| Collapse all + render | 11.06832 ms | **7.59678 ms** | **−29.5%** | 1,064,344 B | **252,697 B** | **−76.3%** |
| Discontinuous scroll + barrier | 8.77411 ms | **8.37551 ms** | −1.4% | 786,486 B/op | **73,457 B/op** | **−90.7%** |

For collapse, mutation plus explicit layout fell from 4.37743 ms to 3.01033 ms
(−29.3% median paired). Virtual layout alone was 95.4% lower, while virtual
collapse mutation was 38.8% higher because the two hierarchy models expose
different notification/update work. The complete collapse wall, allocation, and
tail nevertheless all improved.

For the full-suite discontinuous scroll sample, mutation plus layout fell from
2.63863 ms to 0.13573 ms (−94.8%). The two-callback frame wait increased by 43.5%
because the virtual path reached the refresh-paced barrier earlier; complete wall
changed by only −1.4% median paired.

## Full BenchmarkDotNet collapse matrix

The repository's complete hierarchy-collapse matrix was also rerun rather than
sampling only the fastest cell path. It covers nested, flat, and virtualized
layouts across all six supported benchmark cell configurations. Each of the 36
cases used three launches, three warmups, and ten measured iterations.

### Pending layout after collapse

| Cell path | Nested | Flat | Virtualized | Virtual vs nested | Virtual allocation vs nested |
|---|---:|---:|---:|---:|---:|
| Standard | 3.6336 ms | 3.5677 ms | **0.4378 ms** | **−88.0%** | **−98.3%** |
| OptimizedTheme | 3.0273 ms | 2.9329 ms | **0.4321 ms** | **−85.7%** | **−98.2%** |
| OptimizedPresenter | 2.8374 ms | 2.7611 ms | **0.4111 ms** | **−85.5%** | **−98.2%** |
| DirectHierarchy | 2.9303 ms | 2.8819 ms | **0.4744 ms** | **−83.8%** | **−98.2%** |
| BuiltInDrawn | 2.8751 ms | 2.8426 ms | **0.4038 ms** | **−86.0%** | **−98.1%** |
| CustomDrawn retained fallback | **1.2219 ms** | 1.2348 ms | 4.3247 ms | +253.9% | +453.3% |

For the five paths that can use the rowless surface, pending collapse layout is
83.8–88.0% lower than nested and managed allocation falls from roughly
1.0–1.1 MB to 19.4 KB per operation. `CustomDrawn` is deliberately included in
the full matrix, but it is not a rowless-surface result: its custom cell theme
requires the retained fallback, which is why it creates cells and regresses.

### Collapse mutation plus layout

| Cell path | Nested | Flat | Virtualized | Virtual vs nested | Virtual allocation vs nested |
|---|---:|---:|---:|---:|---:|
| Standard | 10.807 ms | 9.267 ms | **5.613 ms** | **−48.1%** | **−93.7%** |
| OptimizedTheme | 10.234 ms | 8.815 ms | **5.595 ms** | **−45.3%** | **−93.6%** |
| OptimizedPresenter | **8.358 ms** | 8.055 ms | 8.597 ms | +2.9% | **−92.7%** |
| DirectHierarchy | 8.542 ms | 8.141 ms | **5.636 ms** | **−34.0%** | **−92.7%** |
| BuiltInDrawn | 8.175 ms | 8.195 ms | **5.767 ms** | **−29.5%** | **−92.4%** |
| CustomDrawn retained fallback | **7.115 ms** | 7.065 ms | 10.721 ms | +50.7% | +376.8% |

The OptimizedPresenter virtual mean has high launch variance (8.597 ± 2.450 ms,
with a 5.675–15.755 ms range), so its +2.9% mean difference is not treated as a
stable regression. Its pending-layout result remains −85.5% and its allocation is
−92.7%. The other four rowless-compatible end-to-end paths improve by
29.5–48.1%.

BenchmarkDotNet warned that these UI operations are shorter than its recommended
100 ms iteration duration, and macOS denied its high-priority request. These
results are therefore directional supporting evidence; the alternating native
process campaigns above and below remain the primary TDG-to-virtual comparison.
The generated CSV, Markdown, and HTML reports are gitignored under
`artifacts/performance/tdg-vs-virtual-2026-08-13/benchmarkdotnet/results/`.

## Active-work component attribution

Active work is the repository's non-overlapping attribution sum:

```text
scroll mutation + explicit layout + UI render recording
                + compositor update + compositor render
```

Frame wait is excluded because it is the harness's asynchronous animation-clock
barrier, not CPU work owned by either grid. The following campaign enabled the
same Avalonia diagnostics for both controls. ProDataGrid-only diagnostics were not
enabled because asymmetric instrumentation would invalidate the comparison.

Values are means of three process means; changes are medians of three paired
percentage changes.

### Discontinuous scrolling

| Component | TreeDataGrid | ProDataGrid virtual | Median paired change |
|---|---:|---:|---:|
| Mutation | 0.11424 ms | 0.11037 ms | +1.5% |
| Explicit layout | 3.53126 ms | **0.08727 ms** | **−97.4%** |
| UI render recording | 0.43762 ms | **0.39862 ms** | **−10.7%** |
| Compositor update | 0.08511 ms | **0.01721 ms** | **−78.4%** |
| Compositor render | 0.65970 ms | **0.44711 ms** | **−32.8%** |
| **Active work** | **4.82794 ms** | **1.06059 ms** | **−77.9%** |

All three Active pairs improved: −77.9%, −75.4%, and −80.6%.

### Line scrolling

| Component | TreeDataGrid | ProDataGrid virtual | Median paired change |
|---|---:|---:|---:|
| Mutation | **0.13380 ms** | 0.18146 ms | +35.3% |
| Explicit layout | 1.25603 ms | **0.12542 ms** | **−90.3%** |
| UI render recording | **0.24574 ms** | 0.41290 ms | +74.5% |
| Compositor update | 0.02152 ms | 0.02184 ms | −5.3% |
| Compositor render | 0.84188 ms | **0.62016 ms** | **−27.7%** |
| **Active work** | **2.49896 ms** | **1.36178 ms** | **−46.4%** |

All three Active pairs improved: −37.9%, −46.4%, and −50.6%. The virtual surface
records a complete display surface on the UI side, whereas TreeDataGrid reuses its
retained cells. The virtual result still wins overall by removing most layout and
reducing the compositor scene.

### Fractional scrolling

| Component | TreeDataGrid | ProDataGrid virtual | Median paired change |
|---|---:|---:|---:|
| Mutation | 0.14053 ms | **0.11785 ms** | **−25.0%** |
| Explicit layout | 0.29573 ms | **0.09234 ms** | **−70.6%** |
| UI render recording | **0.04846 ms** | 0.17893 ms | +247.6% |
| Compositor update | 0.02287 ms | **0.01463 ms** | **−45.1%** |
| Compositor render | 1.30196 ms | **0.43862 ms** | **−69.3%** |
| **Active work** | **1.80954 ms** | **0.84238 ms** | **−57.3%** |

All three Active pairs improved: −38.6%, −57.3%, and −63.3%. As with line
scrolling, virtual surface recording is higher but is more than offset by layout
and compositor reductions.

## Independent meter-free guardrails

The clean campaign disabled both diagnostic systems and used three warmups plus 15
measured iterations per process.

| Pattern | Mutation + layout | Managed allocation | Full wall | Frame wait | p95 wall |
|---|---:|---:|---:|---:|---:|
| Discontinuous | **−92.5%** | **−91.1%** | −1.8% | +39.4% | +0.05% |
| Line | **−82.6%** | **−73.8%** | −1.2% | +11.9% | −1.5% |
| Fractional | **−18.1%** | +15.5% | −1.1% | −0.6% | −2.0% |

The clean allocation means were:

| Pattern | TreeDataGrid | ProDataGrid virtual | Difference |
|---|---:|---:|---:|
| Discontinuous | 784,099 B/op | **70,144 B/op** | **−91.1%** |
| Line | 144,646 B/op | **37,995 B/op** | **−73.8%** |
| Fractional | **22,970 B/op** | 26,484 B/op | +15.5% |

Fractional scrolling remains the one allocation regression: the virtual path uses
about 3.5 KB/op more than TreeDataGrid. It accompanies a 57.3% Active reduction
but remains a concrete residual optimization opportunity.

Full wall remains in the same refresh-paced band for all patterns. The higher
discontinuous and line frame waits are expected when synchronous work completes
earlier and more of the fixed callback interval becomes visible as idle waiting.
They are reported as scheduling guardrails, not counted as grid work.

## Structural validation and limitation

The steady expanded scroll state validated:

| Structure | TreeDataGrid | ProDataGrid virtual |
|---|---:|---:|
| Logical rows | 4,094 | 4,094 |
| Retained rows | 20 | **0** |
| Retained cells | 120 | **0** |
| Visuals | 607 | **62** |
| Controls | 607 | **62** |
| Extent height | 98,256 | 98,256 |

The 89.8% visual/control reduction is the architectural difference being tested.
Both applications use the same outer 800 × 500 window and fixed row height, but
their native themes reserve slightly different internal geometry: TreeDataGrid's
scroll viewport is 800 × 474 with an 800-pixel extent width, while ProDataGrid's
is 800 × 468 with a 760-pixel extent width. This existing harness difference is
recorded as a limitation; the logical row count, extent height, data, columns,
scroll operations, host, runtime, and completion convention remain matched.

## Environment and protocol

- macOS 26.6 (25G72), Arm64;
- Apple M3 Pro, 11 CPU cores, 14 GPU cores, 18 GB;
- .NET SDK 10.0.201 and .NET 10.0.5 runtime;
- net8.0 applications executed with `DOTNET_ROLL_FORWARD=Major`;
- Avalonia 12.1.0, Skia, render scale 2;
- `DOTNET_TieredCompilation=0`;
- 32 deterministic scroll operations per iteration;
- full suite: four alternating process pairs, two warmups, ten measurements;
- Active diagnostics: three alternating process pairs per scroll pattern, three
  warmups, ten measurements;
- clean guardrails: three alternating process pairs per scroll pattern, three
  warmups, 15 measurements; and
- 44 fresh native processes in total, with no failed or excluded processes;
- full BenchmarkDotNet matrix: 36 cases, three launches per case, three warmups,
  and ten measurements per launch.

Raw JSON is gitignored under
`artifacts/performance/tdg-vs-virtual-2026-08-13/`.

## Representative commands

```sh
GRID_BENCH_PRO_MODE=virtual \
DOTNET_ROLL_FORWARD=Major \
DOTNET_TieredCompilation=0 \
dotnet tests/ProDataGrid.Hierarchy.NativeBenchmarks/Native.Pro/bin/Release/net8.0/Native.Pro.dll \
  --scroll-only --scroll-pattern line --scroll-jumps 32 \
  --warmup 3 --iterations 10 --avalonia-diagnostics \
  --output artifacts/performance/tdg-vs-virtual-2026-08-13/diagnostic/line-virtual.json

DOTNET_ROLL_FORWARD=Major \
DOTNET_TieredCompilation=0 \
dotnet tests/ProDataGrid.Hierarchy.NativeBenchmarks/Native.Tree/bin/Release/net8.0/Native.Tree.dll \
  --scroll-only --scroll-pattern line --scroll-jumps 32 \
  --warmup 3 --iterations 10 --avalonia-diagnostics \
  --output artifacts/performance/tdg-vs-virtual-2026-08-13/diagnostic/line-tree.json
```

The clean commands omit `--avalonia-diagnostics` and use 15 measured iterations.

The full managed collapse matrix used:

```sh
DOTNET_TieredCompilation=0 \
dotnet run \
  --project tests/ProDataGrid.FlatLayout.Benchmarks/ProDataGrid.FlatLayout.Benchmarks.csproj \
  -c Release --no-build -- \
  --filter '*HierarchyCollapse*Benchmarks*' \
  --launchCount 3 --warmupCount 3 --iterationCount 10 \
  --invocationCount 1 --unrollFactor 1 --allStats \
  --artifacts artifacts/performance/tdg-vs-virtual-2026-08-13/benchmarkdotnet
```
