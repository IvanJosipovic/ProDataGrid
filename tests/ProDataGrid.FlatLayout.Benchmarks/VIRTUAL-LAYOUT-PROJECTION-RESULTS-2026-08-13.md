# Virtual layout projection fast path — 2026-08-13

## Scope

The rowless fixed-height architecture had already removed `DataGridRow` recycling
and generation, but `ScrollSlotsByHeight` still re-entered the general
`UpdateDisplayedRows` dispatcher and rebuilt every lightweight item record. The
candidate enters the guarded lightweight update directly, uses the eligibility
invariants that slots are contiguous and equal to row indexes, and reuses item
references for the overlap with the previous viewport.

The baseline is commit `6a4f0e43`. Both variants were built in Release and run in
separate native processes on macOS Arm64, .NET 10.0.9 with tiered compilation
disabled, Avalonia 12.1/Skia, an 800 × 500 window at 2× scale, 4,094 expanded
hierarchy rows, five fixed-width columns, and 32 deterministic discontinuous jumps.
Process order alternated `B C`, `C B`, `B C`. Raw JSON is gitignored under
`artifacts/performance/virtual-layout-projection-2026-08-13/`.

## Diagnostic ownership

Three process pairs used three warmups and ten measured iterations with both
Avalonia and ProDataGrid meters enabled.

| Active component per jump | Baseline | Candidate | Change |
|---|---:|---:|---:|
| `ScrollSlotsByHeight` | 0.02044 ms | **0.01863 ms** | **−8.9%** |
| Displayed-row update | 0.00576 ms | **0.00427 ms** | **−25.9%** |
| Mutation | 0.07751 ms | **0.07690 ms** | −0.8% |
| Explicit layout | 0.06720 ms | **0.06431 ms** | **−4.3%** |
| UI render recording | 0.32908 ms | **0.32066 ms** | −2.6% |
| Compositor update | 0.01314 ms | 0.01345 ms | +2.4% |
| Compositor render | 0.43760 ms | **0.43408 ms** | −0.8% |
| **All active components** | 0.92454 ms | **0.90941 ms** | **−1.6%** |

The target timers improved in every pair. `ScrollSlotsByHeight` changed by −8.3%,
−11.4%, and −6.8%; displayed-row update changed by −23.5%, −31.3%, and −22.5%.
The active-component sum also improved in every pair. The tiny compositor-update
increase is about 0.00031 ms and is not a material transfer of work.

## Clean guardrails

Three process pairs used three warmups and 25 measured iterations without meters.

| Per jump | Baseline | Candidate | Change |
|---|---:|---:|---:|
| Mutation | 0.06849 ms | 0.06863 ms | +0.2% |
| Explicit layout | 0.06223 ms | **0.05916 ms** | **−4.9%** |
| Mutation + layout | 0.13072 ms | **0.12779 ms** | **−2.2%** |
| Managed allocation | 72,167 B | **72,095 B** | −0.1% |
| Full wall | 8.23334 ms | 8.23480 ms | +0.02% |
| Frame wait | 8.10262 ms | 8.10701 ms | +0.05% |

Layout improved in all three pairs. Mutation plus layout improved in two pairs and
regressed in one as the mutation component varied, while its cross-process mean
improved 2.2%. Wall and frame wait remain in the same refresh-paced band and are
not used as active-work measures.

## Projection behavior

The discontinuous benchmark is the worst case for overlap and therefore still
resolves all 20 entering items. Smooth scrolling benefits more:

- moving by one row reuses 19 of 20 visible item references and resolves one;
- changing only the fractional row offset reuses all visible item references and
  resolves none; and
- non-scroll refreshes deliberately rebuild the complete projection so collection
  changes cannot leave stale items.

A reusable presenter buffer makes publication transactional without changing the
identity of the public/internal row-record list. Discovering an item-owned
`DataGridRow` clears the provisional projection and completes the same scroll
through the retained compatibility pipeline. Row recycling and generation remain
zero for the eligible benchmark path; the optimization reduces the bounded
projection work that remains after their removal.
