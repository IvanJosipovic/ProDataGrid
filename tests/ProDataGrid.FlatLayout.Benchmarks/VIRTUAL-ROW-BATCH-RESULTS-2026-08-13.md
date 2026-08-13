# Virtual row lifecycle batch results — 2026-08-13

This focused A/B evaluates lifecycle-metric overhead in discontinuous fixed-height
virtual scrolling. A successful 20-row retarget previously emitted three lifecycle
counters per row: retargeted, prepared, and realized-with-source. The candidate
records the same additive totals once per completed transaction. Normal generation
and fallback paths retain their existing single-row instrumentation.

This is a target-path optimization. Frame wait is refresh pacing rather than active
component work, so acceptance uses diagnostic component timings plus clean mutation
and layout guardrails.

## Environment and protocol

- Baseline: `2880ea0b` (`Optimize virtual row retargeting`).
- Candidate: this report's commit containing lifecycle-counter batching.
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
`artifacts/performance/virtual-next-2026-08-13` directory.

## Diagnostic component result

| Pair | Retarget bind | Displayed-row update | `ScrollSlotsByHeight` | Active work |
|---|---:|---:|---:|---:|
| B1 → C1 | −7.9% | −7.5% | −5.9% | −2.7% |
| B2 → C2 | −9.4% | −8.3% | −7.9% | −4.4% |
| B3 → C3 | −18.5% | −17.6% | −17.4% | −9.4% |
| Paired median | **−9.4%** | **−8.3%** | **−7.9%** | **−4.4%** |

Every pair improved every target component. Mean retarget-bind time moved from
`0.04815`, `0.04826`, and `0.05084` ms to `0.04436`, `0.04374`, and `0.04145` ms.
The complete `ScrollSlotsByHeight` phase moved from `0.08286`, `0.08349`, and
`0.08805` ms to `0.07800`, `0.07690`, and `0.07274` ms.

Active work is mutation + explicit layout + UI render recording + compositor
update + compositor render. It excludes the deliberately awaited animation-clock
interval. Diagnostic wall time was neutral to slightly higher because frame wait
and instrumentation noise dominate a sub-millisecond target-path change.

## Clean synchronous-work guardrails

| Pair | Mutation | Layout | Mutation + layout | Mean wall | Allocation per jump |
|---|---:|---:|---:|---:|---:|
| B1 → C1 | −0.9% | −4.1% | −3.0% | +0.2% | −0.3% |
| B2 → C2 | −2.2% | −3.0% | −2.7% | −0.2% | +0.1% |
| B3 → C3 | −2.9% | −0.5% | −1.4% | +0.3% | −0.9% |
| Paired median | **−2.2%** | **−3.0%** | **−2.7%** | **+0.2%** | **−0.3%** |

Mutation plus layout improved in all three clean pairs. Mean wall and allocation
remained effectively flat, which is expected while frame wait remains about 8 ms.
The change therefore reduces synchronous work without claiming a visible whole-frame
improvement.

## Counter and architecture invariants

Every diagnostic process retained the intended virtual architecture:

- 20 realized rows and zero realized display cells;
- 102 realized visuals;
- exactly 20 retargeted, prepared, and realized rows per discontinuous jump; and
- 20 reused row measures and 20 reused row arrangements.

The counters are emitted only after the transactional bind loop succeeds. A focused
headless test records the values before a discontinuous jump and proves that the
retargeted, prepared, and realized deltas remain equal. Zero and negative batch
counts are ignored, and ordinary single-row APIs still delegate to a count of one.

## Rejected experiments

Two broader shortcuts were measured and removed:

- a uniform fixed-height discontinuous `ScrollSlotsByHeight` shortcut produced a
  neutral paired median for the target phase and a median 8.4% active-work
  regression; and
- suppressing row-index header callbacks when row numbers are hidden changed sign
  when process order was reversed, identifying noise rather than a stable gain.

The retained change is narrower, preserves the complete layout and lifecycle
contracts, and improves all paired target-path and clean synchronous-work samples.
