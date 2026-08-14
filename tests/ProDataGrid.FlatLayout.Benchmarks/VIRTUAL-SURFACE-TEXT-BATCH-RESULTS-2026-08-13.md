# Virtual surface text-command batching — 2026-08-13

## Scope

The warm rowless surface shaped no new text, but recorded 100 separate
`TextLayout.Draw` scene commands per jump. This experiment extracts immutable
glyph-run references from cache entries and submits supported text through one
bounded `ICustomDrawOperation`. Unsupported brushes or text runs retain the normal
`TextLayout.Draw` path.

The baseline is commit `247d8d13`. Both variants were built in Release and run in
separate native processes on macOS Arm64, .NET 10 with tiered compilation disabled,
Avalonia 12.1/Skia, an 800 × 500 window at 2× scale, 4,094 expanded rows, five
fixed-width columns, and 32 deterministic jumps. Three process pairs alternated
`B C`, `C B`, `B C`; each used three warmups and ten measured iterations. Raw JSON
is gitignored under `artifacts/performance/virtual-text-batch-2026-08-13/`.

## Clean lane

| Per jump | Baseline | Batched text | Change |
|---|---:|---:|---:|
| Mutation | 0.08834 ms | **0.08663 ms** | **−1.9%** |
| Explicit layout | 0.08103 ms | **0.07721 ms** | **−4.7%** |
| Mutation + layout | 0.16936 ms | **0.16384 ms** | **−3.3%** |
| Managed allocation | 78,272 B | **67,392 B** | **−13.9%** |
| Full wall | 8.24348 ms | 8.24735 ms | +0.05% |
| Frame wait | 8.07411 ms | 8.08351 ms | +0.12% |

Wall and frame wait remain in the same refresh-paced band. They are not active
DataGrid work and are not used to judge this render-path change.

## Combined diagnostic lane

| Active component per jump | Baseline | Batched text | Change |
|---|---:|---:|---:|
| Mutation | 0.11012 ms | **0.08473 ms** | **−23.1%** |
| Explicit layout | 0.09045 ms | **0.07031 ms** | **−22.3%** |
| UI render recording | 0.43946 ms | **0.33443 ms** | **−23.9%** |
| Compositor update | 0.02005 ms | **0.01780 ms** | **−11.2%** |
| Compositor render | 0.43284 ms | 0.43783 ms | +1.2% |
| **Render-stage aggregate** | 0.89235 ms | **0.79006 ms** | **−11.5%** |
| **All active components** | 1.09293 ms | **0.94511 ms** | **−13.5%** |
| Virtual surface render | 0.40065 ms | **0.30403 ms** | **−24.1%** |
| Managed allocation | 80,965 B | **70,076 B** | **−13.4%** |

The candidate recorded one text scene operation per surface pass and approximately
117.5 immutable glyph runs. UI recording and the directly owned surface stage
improved in every process pair. Compositor render varied in both directions and
was materially unchanged; the optimization removes UI-side scene-command overhead
rather than transferring a comparable cost to the render thread.

## Ownership and correctness

The cache owns one reference to extracted render data, and each submitted scene
operation owns another until Avalonia disposes it. Cache eviction or surface detach
therefore cannot invalidate work already queued to the render thread. Command
storage is rented and returned, disposal is idempotent, and constructor/handoff
failure paths release every acquired reference.

Per-cell clips are replayed inside the operation. Selection backgrounds remain
before text, while current-cell borders and grid-line overlays remain after text.
An Avalonia/Skia pixel-parity test compares centered, ellipsized and clipped output
from direct `TextLayout.Draw` with the batched path and requires zero mismatched
pixels. Smoke coverage also exercises text, date, time, masked, autocomplete,
slider-text, and ComboBox-text surface modes.

## Interpretation

This is a render-recording and allocation win, not a whole-frame claim. The next
render investigation should separately profile glyph drawing/raster work and the
remaining approximately 0.304 ms of UI surface recording before changing either
owner.
