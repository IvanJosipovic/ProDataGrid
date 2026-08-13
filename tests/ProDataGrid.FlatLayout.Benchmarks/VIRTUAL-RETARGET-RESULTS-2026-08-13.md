# Virtual row retarget-buffer results — 2026-08-13

This focused A/B evaluates a bookkeeping optimization in the fixed-height virtual
surface scroll path. It does not claim a whole-frame speedup. The change replaces
parallel item/index arrays and repeated circular-list lookups with one reusable
typed retarget-entry buffer. It also advances target slots arithmetically when the
slot range has no group headers, group footers, or collapsed slots. Validation
still completes for the entire window before any row is mutated.

## Environment and protocol

- Baseline: `77833213` (`Add virtual ComboBox text rendering`).
- Candidate: this report's commit containing the typed retarget-entry buffer.
- macOS 26.6, Arm64, .NET 10.0.5, Avalonia 12.1, Skia.
- Native window: 800 × 500 at 2×; 4,094 expanded hierarchy rows; fixed 24-pixel
  rows; five columns; 32 deterministic scroll jumps per iteration.
- Both variants were built in `Release` from separate worktrees.
- Three independent process pairs used alternating order: `B C`, `C B`, `B C`.
- Diagnostic processes used three warmups and ten measured iterations (320 jumps
  per process) with both ProDataGrid and Avalonia diagnostics enabled.
- Clean processes used three warmups and 25 measured iterations (800 jumps per
  process) without diagnostic meters.
- `DOTNET_ROLL_FORWARD=Major` was required because the benchmark targets .NET 8
  and the machine had the .NET 10 runtime installed.

Raw JSON remains under the gitignored
`artifacts/performance/virtual-retarget-entry-buffer-2026-08-13` directory.

## Diagnostic component result

| Pair | Retarget validation | `ScrollSlotsByHeight` | Active work | Diagnostic wall |
|---|---:|---:|---:|---:|
| B1 → C1 | −18.9% | −7.0% | −5.4% | −1.8% |
| B2 → C2 | −5.1% | +3.5% | +8.9% | −1.8% |
| B3 → C3 | −30.6% | −21.1% | −19.7% | −1.0% |
| Paired median | **−18.9%** | **−7.0%** | **−5.4%** | **−1.8%** |

Retarget validation improved in every pair. Its process means moved from
`0.00881`, `0.00765`, and `0.00890` ms to `0.00714`, `0.00726`, and `0.00617` ms.
The complete `ScrollSlotsByHeight` phase moved from `0.13632`, `0.12231`, and
`0.13703` ms to `0.12676`, `0.12662`, and `0.10809` ms. The second pair regressed,
so the outer-phase reduction is directional rather than a stable 50% result.

Active work is mutation + explicit layout + UI render recording + compositor
update + compositor render. It excludes the deliberately awaited animation-clock
interval. Render variance dominates this small layout change, which is why active
work also regressed in the second pair.

## Clean guardrails

| Pair | Mean wall | Managed allocation per jump |
|---|---:|---:|
| B1 → C1 | −5.8% | −2.0% |
| B2 → C2 | +4.2% | +0.9% |
| B3 → C3 | +0.1% | +0.4% |
| Paired median | **+0.1%** | **+0.4%** |

The first baseline process and second candidate process entered slower frame bands.
The paired median and per-sample medians show no meaningful end-to-end or allocation
change. This is expected: frame wait remains about 8 ms while the optimized
validation phase is measured in microseconds.

## Structural and correctness validation

The diagnostic workload retained the intended virtual architecture on every jump:

- 20 realized rows and zero realized display cells;
- 102 realized visuals;
- 20 rows retargeted per discontinuous jump;
- 20 row measures reused and 20 row arrangements reused; and
- identical 4,094-row extent/viewport validation.

The focused `DataGridFlatVisualLayoutTests` suite passed all 33 tests, including
in-place retarget identity, selection/validation state restoration, fractional
offset arrangement, lifecycle fallback, editing, and pointer hit testing.

## Rejected experiments

Two larger-looking results were rejected during this work:

- hoisting virtual render geometry produced neutral clean timing and inconsistent
  paired active-work results, so it was reverted;
- bulk-copying target items initially appeared to reduce validation, but the copy
  was outside the validation timer. After correcting the instrumentation boundary,
  validation regressed by a paired median of 1.7%, so that experiment was removed.

These rejected results are retained here because they explain why the final change
is deliberately small and why only active component work—not frame wait—is used to
select the next optimization target.
