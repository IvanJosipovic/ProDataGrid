# Virtual cell surface benchmark results — 2026-08-12

> Follow-up scroll work replaced repeatedly formatted text with cached
> `TextLayout` instances. The scroll row below is the earlier baseline; the
> [focused scroll report](SCROLL-RESULTS-2026-08-12.md) supersedes it with the
> final four-process result and CPU/render-pass attribution.

This report compares the new ProDataGrid virtual cell surface with the existing
flat retained architectures and Wieslaw's TreeDataGrid source implementation.
The virtual path rendered all compatible display cells in one control and the
harness verified zero realized `DataGridCell` controls.

## Environment and method

- macOS 26.6, Apple M3 Pro Arm64, .NET 10.0.5, Avalonia 12.1, Skia.
- Native window: 800 × 500, render scale 2, fixed 24-pixel rows, five matched
  columns, two warmups and five measured iterations.
- Scroll results aggregate 160 render-completed jumps.
- TreeDataGrid and ProDataGrid use source project references and the same data,
  host, render-completion convention, and runtime. The net8 applications used
  `DOTNET_ROLL_FORWARD=Major` because only the .NET 10 runtime was installed.
- Allocation is managed allocation traffic per operation, not retained heap,
  native memory, RSS, or GPU memory.
- Native end-to-end latency crosses the harness's two-callback Avalonia render
  barrier. Collapse mutation and layout are also reported separately because
  frame scheduling can change the total by a display interval.

## Native source comparison

| Implementation | First render | First alloc. | Expand | Expand alloc. | Collapse | Collapse alloc. | Scroll | Scroll alloc. |
|---|---:|---:|---:|---:|---:|---:|---:|---:|
| ProDataGrid virtual surface | **17.63 ms** | **1.69 MB** | **22.36 ms** | **3.54 MB** | **15.19 ms** | 0.73 MB | **8.33 ms** | 0.48 MB |
| ProDataGrid direct-cell retained | 26.13 ms | 3.51 MB | 27.78 ms | 4.85 MB | 17.67 ms | **0.54 MB** | 12.38 ms | **0.46 MB** |
| ProDataGrid drawn ordinary cells | 23.62 ms | 3.01 MB | **24.35 ms** | 4.41 MB | 16.24 ms | 0.59 MB | 12.43 ms | 0.57 MB |
| Wieslaw TreeDataGrid | 29.18 ms | 5.97 MB | 26.38 ms | 6.25 MB | 19.91 ms | 1.07 MB | 8.44 ms | 0.79 MB |

The virtual surface reduced first-render latency by 25% versus drawn ordinary
cells, 33% versus direct retained cells, and 40% versus TreeDataGrid. First
render allocated 44%, 52%, and 72% less respectively. Scroll latency was 33%
faster than both ProDataGrid cell-control paths and effectively matched
TreeDataGrid while allocating 40% less than TreeDataGrid.

The virtual collapse mutation plus explicit layout was 6.57 ms. The comparable
figures were 7.97 ms for drawn ordinary cells, 10.82 ms for direct retained
cells, and 14.68 ms for TreeDataGrid. The complete virtual collapse sample was
also the fastest in this run, but the phase result is the safer attribution for
the layout architecture.

## Structural comparison

| Implementation | Realized visuals | Realized controls | Realized rows | Realized cells |
|---|---:|---:|---:|---:|
| ProDataGrid virtual surface | **102** | **102** | 20 | **0** |
| ProDataGrid direct-cell retained | 301 | 301 | 20 | 100 |
| ProDataGrid drawn ordinary cells | 221 | 221 | 20 | 100 |
| Wieslaw TreeDataGrid | 595 | 595 | 20 | 120 |

The surface removes 119 visuals versus ProDataGrid's existing drawn-cell path,
199 versus direct retained cells, and 493 versus TreeDataGrid. Rows remain
retained for row semantics; one active editor cell is materialized only during
editing.

## BenchmarkDotNet hierarchy collapse

The matched built-in-drawn workload uses the existing 149,792-node hierarchy,
a 1200 × 760 headless host, and a 520-pixel DataGrid viewport.

| Operation | Nested | Flat retained | Virtual surface |
|---|---:|---:|---:|
| Pending collapse layout | 4.441 ms / 1.75 MB | 2.825 ms / 1.02 MB | **0.585 ms / 0.05 MB** |
| Collapse plus layout/render | 9.676 ms / 1.86 MB | 8.510 ms / 1.13 MB | **6.651 ms / 0.14 MB** |

This final rerun used one launch, three warmups, and five measurements. Each
iteration is below BenchmarkDotNet's recommended 100 ms, so these numbers are
directional. The pending-layout result puts flat 36.4% below nested and virtual
86.8% below nested; managed allocation falls by 41.7% and 97.0%, respectively.
For collapse plus dispatcher/layout completion, flat is 12.1% faster and virtual
31.3% faster than nested, with allocation reductions of 39.3% and 92.4%.

## Raw artifacts

The local `artifacts/performance/scroll-2026-08-12` directory contains the final
native JSON, diagnostic comparisons, and BenchmarkDotNet CSV, Markdown, HTML,
and log output. It is gitignored by design. The JSON files contain raw samples,
medians, P95, standard deviations, allocation, scroll/collapse phases, viewport
metrics, and structural validation.
