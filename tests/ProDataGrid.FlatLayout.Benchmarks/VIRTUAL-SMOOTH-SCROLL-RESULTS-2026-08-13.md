# Virtual smooth-scroll and precomputed-clip results — 2026-08-13

## Scope

This follow-up separates three fixed-height virtual-surface workloads instead of
treating every offset change as a discontinuous jump:

- `discontinuous` replaces the complete visible item projection;
- `line` advances one row and normally reuses 19 of 20 visible item references; and
- `fractional` changes only the within-row offset and reuses all visible items.

The candidate also moves horizontal cells-viewport and frozen-region intersection
from the per-visible-cell render, hit-test, and bounds paths into the per-column
layout record. `FlatColumnLayout` now stores the final visible left edge and width.
The surface consumes those values without repeatedly querying row-header and frozen
column widths for every cell.

The baseline is commit `08074740d4ae5aae803726183eefe537a723e6e4`. The candidate
is that commit plus the precomputed-clip changes. Both variants were built in
Release and run in separate native processes on macOS 26.6 Arm64, .NET 10.0.5,
Avalonia 12.1/Skia, an 800 × 500 window at 2× scale, 4,094 expanded hierarchy rows,
five fixed-width columns, and 32 scroll operations per iteration. Tiered compilation
was disabled and the net8.0 application used `DOTNET_ROLL_FORWARD=Major` because the
installed runtime was .NET 10. Process order alternated `B C`, `C B`, `B C`.

Every process validated zero realized rows and zero realized cells. Raw JSON is
gitignored under
`artifacts/performance/virtual-smooth-scroll-2026-08-13/`.

## Fine-grained diagnostic ownership

Three process pairs per pattern used three warmups and eight measured iterations
with Avalonia and ProDataGrid diagnostics enabled. Values are process-mean time per
scroll operation. Change is the mean of the three paired percentage changes.

| Pattern | Surface recording | Change | UI render recording | Change | Active-work sum | Change |
|---|---:|---:|---:|---:|---:|---:|
| Discontinuous | 0.39119 → **0.37799 ms** | **−3.3%** | 0.43771 → **0.42716 ms** | **−2.4%** | 1.32596 → 1.34240 ms | +1.4% |
| Line | 0.29646 → **0.26202 ms** | **−11.5%** | 0.33536 → **0.30035 ms** | **−10.3%** | 1.11642 → **1.08462 ms** | −2.8% |
| Fractional | 0.18205 → **0.15213 ms** | **−16.4%** | 0.22719 → **0.19643 ms** | **−13.5%** | 0.97091 → **0.94669 ms** | −2.6% |

The directly targeted surface timer improved in every pair:

- discontinuous: −3.2%, −1.1%, and −5.6%;
- line: −10.6%, −8.6%, and −15.3%; and
- fractional: −17.3%, −16.6%, and −15.5%.

The complete active-work score is intentionally more conservative. Line pair
changes were −1.6%, +1.5%, and −8.3%; fractional changes were −2.5%, −6.2%, and
+1.0%. The change therefore proves reduced surface-recording ownership, not a
universal 50% reduction in all active components.

`ScrollSlotsByHeight` itself was only 0.0285–0.0334 ms in the baseline diagnostics,
about 2–3% of the complete active-work score. Even deleting it completely could not
produce a 50% total improvement while surface and compositor work remain. The
eligible virtual lane already records zero row realization and zero cell realization:
retained row recycling/generation is not present in this workload.

## Avalonia phase attribution without ProDataGrid meters

A second three-pair lane used three warmups and ten measured iterations with only
Avalonia phase diagnostics enabled.

| Pattern | UI render change | Active-work change | Active pair changes |
|---|---:|---:|---|
| Discontinuous | **−10.3%** | −7.3% | −9.6%, +2.2%, −14.4% |
| Line | **−22.4%** | **−15.6%** | −16.1%, −8.3%, −22.5% |
| Fractional | **−10.3%** | +2.1% | +1.1%, +8.7%, −3.4% |

UI recording improved in all line and fractional pairs and two of three
discontinuous pairs. Fractional compositor-render time increased in this lane, so
its UI reduction is not reported as an end-to-end active-work win. Full frame wait
is excluded from the active score because it includes refresh-clock pacing.

## Meter-free guardrails

Three process pairs per pattern used three warmups and 15 measured iterations with
no Avalonia or ProDataGrid meters.

| Pattern | Mutation + layout | Allocation | Full wall | Frame wait | P95 wall |
|---|---:|---:|---:|---:|---:|
| Discontinuous | +4.7% | +0.01% | −0.27% | −0.40% | +0.95% |
| Line | +7.1% | −0.08% | −0.23% | −0.44% | −1.67% |
| Fractional | −16.7% | +0.05% | −0.10% | +0.39% | −1.24% |

Mutation and layout moved in opposite directions across patterns despite identical
candidate code and are not used to claim a broad synchronous improvement. The
guardrails show effectively unchanged allocation and no wall-time, frame-band, or
tail regression. Wall and frame wait remain dominated by the approximately 8 ms
display-clock band.

## Rejected same-range projection experiment

A follow-up attempted to replace the existing staging-buffer publication with
in-place `DataGridVirtualRowInfo` rewrites when a fractional scroll kept the exact
same slot range. Existing behavior tests passed, including zero new item resolution,
complete item reuse, and updated fractional geometry. Performance rejected the
change: across three 12-iteration diagnostic pairs, `ScrollSlotsByHeight` regressed
by +3.8%, +14.8%, and +28.9% (mean +15.9%), while displayed-row update regressed by
+5.2%, +16.9%, and +26.0%. The experiment was removed.

The remaining projection buffer is small, allocation-free in steady state, and
faster than rewriting the list in place at this viewport size. Future work should
target the dominant surface/compositor owners or use trace evidence before changing
the row-projection publication strategy again.
